using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
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

    /// <summary>İstemcinin bulunduğu sohbet odası.</summary>
    public string Oda { get; internal set; } = ChatMesaji.VarsayilanOda;
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

    /// <summary>Herkese açık mesajların geçmişi; yeni katılanlara son 50 mesaj gönderilir.</summary>
    public MesajGecmisi Gecmis { get; }

    /// <param name="gecmis">Geçmiş deposu. null ise yalnızca bellekte tutulan bir geçmiş kullanılır.</param>
    public ChatSunucu(MesajGecmisi? gecmis = null)
    {
        Gecmis = gecmis ?? new MesajGecmisi();
    }

    /// <summary>
    /// Sunucu parolası. Boş/null ise herkes katılabilir; doluysa Katil mesajındaki parola eşleşmelidir.
    /// Karşılaştırma SHA-256 özetleri üzerinde sabit zamanlı yapılır (zamanlama saldırısına karşı).
    /// </summary>
    public string? Parola
    {
        get => _parola;
        set => _parola = string.IsNullOrEmpty(value) ? null : value;
    }
    private string? _parola;

    /// <summary>
    /// TLS sertifikası. Atanırsa tüm bağlantılar SslStream (TLS 1.2/1.3) ile şifrelenir;
    /// null ise bağlantılar düz metindir.
    /// </summary>
    public X509Certificate2? Sertifika { get; set; }

    public bool TlsAcik => Sertifika is not null;

    /// <summary>Yanlış parola denemesinden sonra cevap vermeden önce beklenecek süre (kaba kuvvete karşı).</summary>
    public TimeSpan YanlisParolaBeklemesi { get; set; } = TimeSpan.FromSeconds(1);

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
        GunlukYaz($"Sunucu {Port} portunda dinlemeye başladı" + (TlsAcik ? $" (TLS açık, parmak izi {TlsYardimcisi.ParmakIzi(Sertifika!)[..23]}...)." : " (şifresiz)."));
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

    /// <summary>TLS açıksa el sıkışmasını yapar ve şifreli akışı döndürür.</summary>
    private async Task<JsonSatirBaglantisi?> BaglantiHazirlaAsync(TcpClient tcp, CancellationToken iptal)
    {
        var sertifika = Sertifika;
        if (sertifika is null)
            return new JsonSatirBaglantisi(tcp);

        var ssl = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false);
        try
        {
            using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
            zamanAsimi.CancelAfter(TimeSpan.FromSeconds(10));
            await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
            {
                ServerCertificate = sertifika,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ClientCertificateRequired = false
            }, zamanAsimi.Token).ConfigureAwait(false);
            return new JsonSatirBaglantisi(tcp, ssl);
        }
        catch (Exception ex) when (ex is AuthenticationException or IOException or OperationCanceledException or SocketException)
        {
            GunlukYaz($"{tcp.Client.RemoteEndPoint} TLS el sıkışması başarısız (istemci TLS kullanmıyor olabilir): {ex.Message}");
            ssl.Dispose();
            tcp.Dispose();
            return null;
        }
    }

    private async Task IstemciyiYonetAsync(TcpClient tcp, CancellationToken iptal)
    {
        var hazir = await BaglantiHazirlaAsync(tcp, iptal).ConfigureAwait(false);
        if (hazir is null)
            return;
        var baglanti = hazir;
        BagliIstemci? istemci = null;
        try
        {
            // 1) İlk mesaj Katil olmalı ve 10 saniye içinde gelmeli
            using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
            zamanAsimi.CancelAfter(TimeSpan.FromSeconds(10));
            var ilk = await baglanti.OkuAsync(zamanAsimi.Token).ConfigureAwait(false);

            string? hata = KatilmaHatasi(ilk) ?? ParolaHatasi(ilk);
            if (hata is not null)
            {
                if (hata == YanlisParolaMetni)
                    await Task.Delay(YanlisParolaBeklemesi, iptal).ConfigureAwait(false);
                GunlukYaz($"{baglanti.UzakAdres} reddedildi: {hata}");
                await baglanti.GonderAsync(ChatMesaji.HataMesaji(hata), iptal).ConfigureAwait(false);
                return;
            }

            istemci = new BagliIstemci(ilk!.Gonderen.Trim(), baglanti)
            {
                Oda = OdaAdiHatasi(ilk.Oda) is null ? ilk.Oda!.Trim() : ChatMesaji.VarsayilanOda
            };
            if (!_istemciler.TryAdd(istemci.KullaniciAdi, istemci))
            {
                istemci = null;
                await baglanti.GonderAsync(ChatMesaji.HataMesaji("Bu kullanıcı adı zaten kullanılıyor."), iptal).ConfigureAwait(false);
                return;
            }

            GunlukYaz($"{istemci.KullaniciAdi} bağlandı ({istemci.UzakAdres}, oda: {istemci.Oda})."); 
            IstemciBaglandi?.Invoke(this, istemci);
            await baglanti.GonderAsync(ChatMesaji.Sistem($"Hoş geldin {istemci.KullaniciAdi}! '{istemci.Oda}' odasına katıldın.") with { Oda = istemci.Oda }, iptal).ConfigureAwait(false);
            foreach (var eski in Gecmis.SonMesajlar(50, istemci.Oda))
                await baglanti.GonderAsync(eski, iptal).ConfigureAwait(false);
            await OdayaGonderAsync(istemci.Oda, Gecmise(ChatMesaji.Sistem($"{istemci.KullaniciAdi} sohbete katıldı.") with { Oda = istemci.Oda }), haric: istemci.KullaniciAdi).ConfigureAwait(false);
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
                    await OdayaGonderAsync(istemci.Oda, Gecmise(ChatMesaji.Sistem($"{istemci.KullaniciAdi} sohbetten ayrıldı.") with { Oda = istemci.Oda })).ConfigureAwait(false);
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

    public const string YanlisParolaMetni = "Sunucu parolası hatalı.";

    private string? ParolaHatasi(ChatMesaji? ilk)
    {
        string? beklenen = _parola;
        if (beklenen is null)
            return null;
        return ParolalarEsit(beklenen, ilk?.Parola ?? "") ? null : YanlisParolaMetni;
    }

    /// <summary>İki parolayı sabit zamanlı karşılaştırır.</summary>
    public static bool ParolalarEsit(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(a)),
                                                SHA256.HashData(Encoding.UTF8.GetBytes(b)));

    /// <summary>Oda adını doğrular (1-20 karakter; harf, rakam, boşluk, - ve _). Sorun yoksa null.</summary>
    public static string? OdaAdiHatasi(string? oda)
    {
        string ad = oda?.Trim() ?? "";
        if (ad.Length is 0 or > 20)
            return "Oda adı 1-20 karakter olmalı.";
        if (!ad.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_'))
            return "Oda adında yalnızca harf, rakam, boşluk, - ve _ kullanılabilir.";
        return null;
    }

    /// <summary>Sunucudaki aktif odalar (varsayılan oda her zaman listededir).</summary>
    public IReadOnlyList<string> Odalar =>
        _istemciler.Values.Select(i => i.Oda).Append(ChatMesaji.VarsayilanOda)
                   .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(o => o).ToList();

    private async Task OdaDegistirAsync(BagliIstemci istemci, string? yeniOda)
    {
        string? hata = OdaAdiHatasi(yeniOda);
        if (hata is not null)
        {
            await GuvenliGonderAsync(istemci, ChatMesaji.HataMesaji(hata)).ConfigureAwait(false);
            return;
        }
        string eski = istemci.Oda, yeni = yeniOda!.Trim();
        if (eski.Equals(yeni, StringComparison.OrdinalIgnoreCase))
            return;

        istemci.Oda = yeni;
        GunlukYaz($"{istemci.KullaniciAdi}: '{eski}' → '{yeni}' odası.");
        await OdayaGonderAsync(eski, Gecmise(ChatMesaji.Sistem($"{istemci.KullaniciAdi} '{yeni}' odasına geçti.") with { Oda = eski })).ConfigureAwait(false);
        await GuvenliGonderAsync(istemci, ChatMesaji.Sistem($"'{yeni}' odasına geçtin.") with { Oda = yeni }).ConfigureAwait(false);
        foreach (var m in Gecmis.SonMesajlar(50, yeni))
            await GuvenliGonderAsync(istemci, m).ConfigureAwait(false);
        await OdayaGonderAsync(yeni, Gecmise(ChatMesaji.Sistem($"{istemci.KullaniciAdi} odaya katıldı.") with { Oda = yeni }), haric: istemci.KullaniciAdi).ConfigureAwait(false);
        await KullaniciListesiniGonderAsync().ConfigureAwait(false);
    }

    private async Task MesajiIsleAsync(BagliIstemci istemci, ChatMesaji gelen)
    {
        if (gelen.Tur == MesajTuru.OdaDegistir)
        {
            await OdaDegistirAsync(istemci, gelen.Oda).ConfigureAwait(false);
            return;
        }
        if (string.IsNullOrWhiteSpace(gelen.Metin))
            return;

        // Gönderen ve zaman sunucu tarafından belirlenir; istemci başkası adına mesaj atamaz.
        istemci.MesajSayisi++;
        var mesaj = gelen with
        {
            Gonderen = istemci.KullaniciAdi, Zaman = DateTime.Now, Kullanicilar = null, Odalar = null,
            Gecmis = null, Parola = null, Oda = istemci.Oda
        };

        switch (gelen.Tur)
        {
            case MesajTuru.Sohbet:
                MesajIletildi?.Invoke(this, mesaj);
                await OdayaGonderAsync(istemci.Oda, Gecmise(mesaj)).ConfigureAwait(false);
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

    /// <summary>Mesajı geçmişe ekleyip aynen döndürür (zincirleme kullanım için).</summary>
    private ChatMesaji Gecmise(ChatMesaji mesaj)
    {
        Gecmis.Ekle(mesaj);
        return mesaj;
    }

    /// <summary>Her istemciye kendi odasındaki kullanıcıları ve aktif oda listesini gönderir.</summary>
    private async Task KullaniciListesiniGonderAsync()
    {
        var odalar = Odalar.ToList();
        var hepsi = _istemciler.Values.ToList();
        var gorevler = hepsi.Select(i => GuvenliGonderAsync(i, new ChatMesaji
        {
            Tur = MesajTuru.KullaniciListesi,
            Gonderen = "Sunucu",
            Oda = i.Oda,
            Odalar = odalar,
            Kullanicilar = hepsi.Where(k => k.Oda.Equals(i.Oda, StringComparison.OrdinalIgnoreCase))
                                .Select(k => k.KullaniciAdi).OrderBy(k => k).ToList()
        }));
        await Task.WhenAll(gorevler).ConfigureAwait(false);
    }

    /// <summary>Yalnızca belirtilen odadaki istemcilere gönderir.</summary>
    private async Task OdayaGonderAsync(string oda, ChatMesaji mesaj, string? haric = null)
    {
        var gorevler = _istemciler.Values
            .Where(i => i.Oda.Equals(oda, StringComparison.OrdinalIgnoreCase))
            .Where(i => haric is null || !i.KullaniciAdi.Equals(haric, StringComparison.OrdinalIgnoreCase))
            .Select(i => GuvenliGonderAsync(i, mesaj));
        await Task.WhenAll(gorevler).ConfigureAwait(false);
    }

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
