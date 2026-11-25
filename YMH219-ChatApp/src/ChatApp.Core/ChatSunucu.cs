using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ChatApp.Core;

/// <summary>Sunucuya bağlı bir istemcinin temel bilgileri.</summary>
public sealed class BagliIstemci
{
    internal BagliIstemci(string kullaniciAdi, JsonSatirBaglantisi baglanti)
    {
        KullaniciAdi = kullaniciAdi;
        Baglanti = baglanti;
        UzakAdres = baglanti.UzakAdres;
        BaglanmaZamani = DateTime.Now;
    }

    public string KullaniciAdi { get; }
    public string UzakAdres { get; }
    public DateTime BaglanmaZamani { get; }
    public int MesajSayisi { get; internal set; }
    internal JsonSatirBaglantisi Baglanti { get; }
}

/// <summary>
/// TcpListener tabanlı sohbet sunucusu. Her istemci için ayrı bir asenkron okuma döngüsü çalışır.
/// Olaylar arka plan iş parçacığından tetiklenir; arayüz bu olaylarda Invoke/BeginInvoke kullanmalıdır.
/// </summary>
public sealed class ChatSunucu : IDisposable
{
    public const int MaksimumKullaniciAdiUzunlugu = 20;

    private readonly ConcurrentDictionary<string, BagliIstemci> _istemciler = new(StringComparer.OrdinalIgnoreCase);
    private TcpListener? _dinleyici;
    private CancellationTokenSource? _iptal;

    public event EventHandler<BagliIstemci>? IstemciBaglandi;
    public event EventHandler<BagliIstemci>? IstemciAyrildi;
    public event EventHandler<ChatMesaji>? MesajIletildi;
    public event EventHandler<string>? Gunluk;

    public bool Calisiyor => _dinleyici is not null;
    public int Port { get; private set; }

    public IReadOnlyCollection<BagliIstemci> Istemciler => _istemciler.Values.ToList();

    /// <summary>Sunucuyu başlatır. Port 0 verilirse işletim sistemi boş bir port seçer (testlerde kullanılır).</summary>
    public void Baslat(int port, IPAddress? adres = null)
    {
        if (Calisiyor)
            throw new InvalidOperationException("Sunucu zaten çalışıyor.");
        if (port is < 0 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port 0-65535 arasında olmalı.");

        var dinleyici = new TcpListener(adres ?? IPAddress.Any, port);
        dinleyici.Start(); // Port kullanımdaysa SocketException fırlatır
        _dinleyici = dinleyici;
        Port = ((IPEndPoint)dinleyici.LocalEndpoint).Port;
        _iptal = new CancellationTokenSource();
        GunlukYaz($"Sunucu {Port} portunda dinlemeye başladı.");
        _ = KabulDongusuAsync(dinleyici, _iptal.Token);
    }

    public void Durdur()
    {
        if (!Calisiyor)
            return;

        _iptal?.Cancel();
        _dinleyici?.Stop();
        _dinleyici = null;

        foreach (var istemci in _istemciler.Values)
            istemci.Baglanti.Dispose();
        _istemciler.Clear();
        GunlukYaz("Sunucu durduruldu.");
    }

    private async Task KabulDongusuAsync(TcpListener dinleyici, CancellationToken iptal)
    {
        while (!iptal.IsCancellationRequested)
        {
            TcpClient tcp;
            try
            {
                tcp = await dinleyici.AcceptTcpClientAsync(iptal).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                break; // Durdur() çağrıldı
            }

            _ = IstemciyiYonetAsync(tcp, iptal);
        }
    }

    private async Task IstemciyiYonetAsync(TcpClient tcp, CancellationToken iptal)
    {
        var baglanti = new JsonSatirBaglantisi(tcp);
        BagliIstemci? istemci = null;
        try
        {
            // 1) İlk mesaj Katil olmalı ve 10 saniye içinde gelmeli
            using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
            zamanAsimi.CancelAfter(TimeSpan.FromSeconds(10));
            var ilk = await baglanti.OkuAsync(zamanAsimi.Token).ConfigureAwait(false);

            string? hata = KatilmaHatasi(ilk);
            if (hata is not null)
            {
                GunlukYaz($"{baglanti.UzakAdres} reddedildi: {hata}");
                await baglanti.GonderAsync(ChatMesaji.HataMesaji(hata), iptal).ConfigureAwait(false);
                return;
            }

            istemci = new BagliIstemci(ilk!.Gonderen.Trim(), baglanti);
            if (!_istemciler.TryAdd(istemci.KullaniciAdi, istemci))
            {
                istemci = null;
                await baglanti.GonderAsync(ChatMesaji.HataMesaji("Bu kullanıcı adı zaten kullanılıyor."), iptal).ConfigureAwait(false);
                return;
            }

            GunlukYaz($"{istemci.KullaniciAdi} bağlandı ({istemci.UzakAdres}).");
            IstemciBaglandi?.Invoke(this, istemci);
            await baglanti.GonderAsync(ChatMesaji.Sistem($"Hoş geldin {istemci.KullaniciAdi}! Sohbete katıldın."), iptal).ConfigureAwait(false);
            await HerkeseGonderAsync(ChatMesaji.Sistem($"{istemci.KullaniciAdi} sohbete katıldı."), haric: istemci.KullaniciAdi).ConfigureAwait(false);
            await KullaniciListesiniGonderAsync().ConfigureAwait(false);

            // 2) Mesaj döngüsü
            while (!iptal.IsCancellationRequested)
            {
                var mesaj = await baglanti.OkuAsync(iptal).ConfigureAwait(false);
                if (mesaj is null)
                    break; // istemci bağlantıyı kapattı

                await MesajiIsleAsync(istemci, mesaj).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            if (istemci is null)
                GunlukYaz($"{baglanti.UzakAdres} zamanında kullanıcı adı göndermedi.");
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException
                                       or System.Text.Json.JsonException or InvalidDataException)
        {
            GunlukYaz($"{istemci?.KullaniciAdi ?? baglanti.UzakAdres} bağlantı hatası: {ex.Message}");
        }
        finally
        {
            baglanti.Dispose();
            if (istemci is not null && _istemciler.TryRemove(istemci.KullaniciAdi, out _))
            {
                GunlukYaz($"{istemci.KullaniciAdi} ayrıldı.");
                IstemciAyrildi?.Invoke(this, istemci);
                if (Calisiyor)
                {
                    await HerkeseGonderAsync(ChatMesaji.Sistem($"{istemci.KullaniciAdi} sohbetten ayrıldı.")).ConfigureAwait(false);
                    await KullaniciListesiniGonderAsync().ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>Katılma mesajını doğrular; sorun yoksa null döner.</summary>
    public static string? KatilmaHatasi(ChatMesaji? ilk)
    {
        if (ilk is null || ilk.Tur != MesajTuru.Katil)
            return "İlk mesaj katılma isteği olmalı.";
        string ad = ilk.Gonderen?.Trim() ?? "";
        if (ad.Length == 0)
            return "Kullanıcı adı boş olamaz.";
        if (ad.Length > MaksimumKullaniciAdiUzunlugu)
            return $"Kullanıcı adı en fazla {MaksimumKullaniciAdiUzunlugu} karakter olabilir.";
        if (ad.Equals("Sunucu", StringComparison.OrdinalIgnoreCase))
            return "Bu kullanıcı adı ayrılmıştır.";
        return null;
    }

    private async Task MesajiIsleAsync(BagliIstemci istemci, ChatMesaji gelen)
    {
        if (string.IsNullOrWhiteSpace(gelen.Metin))
            return;

        // Gönderen ve zaman sunucu tarafından belirlenir; istemci başkası adına mesaj atamaz.
        istemci.MesajSayisi++;
        var mesaj = gelen with { Gonderen = istemci.KullaniciAdi, Zaman = DateTime.Now, Kullanicilar = null };

        switch (gelen.Tur)
        {
            case MesajTuru.Sohbet:
                MesajIletildi?.Invoke(this, mesaj);
                await HerkeseGonderAsync(mesaj).ConfigureAwait(false);
                break;

            case MesajTuru.Ozel when gelen.Alici is not null:
                if (_istemciler.TryGetValue(gelen.Alici, out var alici))
                {
                    MesajIletildi?.Invoke(this, mesaj);
                    await GuvenliGonderAsync(alici, mesaj).ConfigureAwait(false);
                    if (alici != istemci)
                        await GuvenliGonderAsync(istemci, mesaj).ConfigureAwait(false);
                }
                else
                {
                    await GuvenliGonderAsync(istemci, ChatMesaji.HataMesaji($"'{gelen.Alici}' çevrimiçi değil.")).ConfigureAwait(false);
                }
                break;
        }
    }

    private Task KullaniciListesiniGonderAsync() =>
        HerkeseGonderAsync(new ChatMesaji
        {
            Tur = MesajTuru.KullaniciListesi,
            Gonderen = "Sunucu",
            Kullanicilar = _istemciler.Keys.OrderBy(k => k).ToList()
        });

    private async Task HerkeseGonderAsync(ChatMesaji mesaj, string? haric = null)
    {
        var gorevler = _istemciler.Values
            .Where(i => haric is null || !i.KullaniciAdi.Equals(haric, StringComparison.OrdinalIgnoreCase))
            .Select(i => GuvenliGonderAsync(i, mesaj));
        await Task.WhenAll(gorevler).ConfigureAwait(false);
    }

    /// <summary>Bir istemciye gönderir; o istemci kopmuşsa hata diğerlerini etkilemez.</summary>
    private async Task GuvenliGonderAsync(BagliIstemci istemci, ChatMesaji mesaj)
    {
        try
        {
            using var zamanAsimi = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await istemci.Baglanti.GonderAsync(mesaj, zamanAsimi.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            GunlukYaz($"{istemci.KullaniciAdi} kullanıcısına gönderilemedi: {ex.Message}");
            istemci.Baglanti.Dispose(); // okuma döngüsü kapanır ve istemci listeden çıkar
        }
    }

    private void GunlukYaz(string metin) => Gunluk?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {metin}");

    public void Dispose() => Durdur();
}
