using System.Net.Sockets;

namespace ChatApp.Core;

/// <summary>
/// TcpClient tabanlı sohbet istemcisi. Bağlandıktan sonra arka planda gelen mesajları okur
/// ve <see cref="MesajAlindi"/> olayıyla bildirir. Olaylar arka plan iş parçacığından gelir.
/// </summary>
public sealed class ChatIstemci : IDisposable
{
    private JsonSatirBaglantisi? _baglanti;
    private CancellationTokenSource? _iptal;
    private bool _bilerekKapatildi;

    public event EventHandler<ChatMesaji>? MesajAlindi;

    /// <summary>Bağlantı beklenmedik şekilde koptuğunda sebebiyle tetiklenir.</summary>
    public event EventHandler<string>? BaglantiKoptu;

    public bool Bagli => _baglanti is not null;
    public string KullaniciAdi { get; private set; } = string.Empty;

    /// <summary>
    /// Sunucuya bağlanır ve kullanıcı adıyla katılır. Sunucu reddederse (ör. ad alınmışsa)
    /// <see cref="ChatBaglantiHatasi"/> fırlatılır.
    /// </summary>
    public async Task BaglanAsync(string sunucu, int port, string kullaniciAdi, TimeSpan? zamanAsimi = null)
    {
        if (Bagli)
            throw new InvalidOperationException("Zaten bağlı.");
        if (string.IsNullOrWhiteSpace(sunucu))
            throw new ChatBaglantiHatasi("Sunucu adresi boş olamaz.");
        if (port is < 1 or > 65535)
            throw new ChatBaglantiHatasi("Port 1-65535 arasında olmalı.");

        string? adHatasi = ChatSunucu.KatilmaHatasi(new ChatMesaji { Tur = MesajTuru.Katil, Gonderen = kullaniciAdi ?? "" });
        if (adHatasi is not null)
            throw new ChatBaglantiHatasi(adHatasi);

        using var iptal = new CancellationTokenSource(zamanAsimi ?? TimeSpan.FromSeconds(5));
        var tcp = new TcpClient();
        JsonSatirBaglantisi? baglanti = null;
        try
        {
            await tcp.ConnectAsync(sunucu.Trim(), port, iptal.Token).ConfigureAwait(false);
            baglanti = new JsonSatirBaglantisi(tcp);
            await baglanti.GonderAsync(new ChatMesaji { Tur = MesajTuru.Katil, Gonderen = kullaniciAdi!.Trim() }, iptal.Token)
                          .ConfigureAwait(false);

            // Sunucunun ilk cevabı: hoş geldin (Sistem) ya da Hata
            var cevap = await baglanti.OkuAsync(iptal.Token).ConfigureAwait(false)
                        ?? throw new ChatBaglantiHatasi("Sunucu bağlantıyı kapattı.");
            if (cevap.Tur == MesajTuru.Hata)
                throw new ChatBaglantiHatasi(cevap.Metin);

            KullaniciAdi = kullaniciAdi.Trim();
            _baglanti = baglanti;
            _bilerekKapatildi = false;
            _iptal = new CancellationTokenSource();
            MesajAlindi?.Invoke(this, cevap);
            _ = OkumaDongusuAsync(baglanti, _iptal.Token);
        }
        catch (OperationCanceledException)
        {
            baglanti?.Dispose(); tcp.Dispose();
            throw new ChatBaglantiHatasi("Sunucuya bağlanırken zaman aşımı oluştu.");
        }
        catch (SocketException ex)
        {
            baglanti?.Dispose(); tcp.Dispose();
            throw new ChatBaglantiHatasi(ex.SocketErrorCode == SocketError.ConnectionRefused
                ? $"{sunucu}:{port} adresinde çalışan bir sunucu bulunamadı (bağlantı reddedildi)."
                : $"Bağlantı kurulamadı: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidDataException)
        {
            baglanti?.Dispose(); tcp.Dispose();
            throw new ChatBaglantiHatasi($"Sunucuyla iletişim hatası: {ex.Message}", ex);
        }
        catch (ChatBaglantiHatasi)
        {
            baglanti?.Dispose(); tcp.Dispose();
            throw;
        }
    }

    public Task GonderAsync(string metin) =>
        GonderAsync(new ChatMesaji { Tur = MesajTuru.Sohbet, Gonderen = KullaniciAdi, Metin = metin });

    public Task OzelGonderAsync(string alici, string metin) =>
        GonderAsync(new ChatMesaji { Tur = MesajTuru.Ozel, Gonderen = KullaniciAdi, Alici = alici, Metin = metin });

    private async Task GonderAsync(ChatMesaji mesaj)
    {
        var baglanti = _baglanti ?? throw new ChatBaglantiHatasi("Sunucuya bağlı değilsiniz.");
        if (string.IsNullOrWhiteSpace(mesaj.Metin))
            throw new ChatBaglantiHatasi("Boş mesaj gönderilemez.");
        try
        {
            using var zamanAsimi = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await baglanti.GonderAsync(mesaj, zamanAsimi.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Kapat(beklenmedik: true, "Mesaj gönderilemedi, bağlantı koptu.");
            throw new ChatBaglantiHatasi("Mesaj gönderilemedi: sunucu bağlantısı koptu.", ex);
        }
    }

    private async Task OkumaDongusuAsync(JsonSatirBaglantisi baglanti, CancellationToken iptal)
    {
        string sebep = "Sunucu bağlantıyı kapattı.";
        try
        {
            while (!iptal.IsCancellationRequested)
            {
                var mesaj = await baglanti.OkuAsync(iptal).ConfigureAwait(false);
                if (mesaj is null)
                    break;
                MesajAlindi?.Invoke(this, mesaj);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException
                                       or System.Text.Json.JsonException or InvalidDataException)
        {
            sebep = $"Bağlantı koptu: {ex.Message}";
        }

        Kapat(beklenmedik: true, sebep);
    }

    /// <summary>Kullanıcının "Bağlantıyı Kes" isteği.</summary>
    public void BaglantiyiKes() => Kapat(beklenmedik: false, null);

    private void Kapat(bool beklenmedik, string? sebep)
    {
        var baglanti = Interlocked.Exchange(ref _baglanti, null);
        if (baglanti is null)
            return;

        if (!beklenmedik)
            _bilerekKapatildi = true;
        _iptal?.Cancel();
        baglanti.Dispose();

        if (beklenmedik && !_bilerekKapatildi)
            BaglantiKoptu?.Invoke(this, sebep ?? "Bağlantı koptu.");
    }

    public void Dispose() => BaglantiyiKes();
}

/// <summary>Kullanıcıya gösterilebilecek anlaşılır bağlantı hatası.</summary>
public sealed class ChatBaglantiHatasi : Exception
{
    public ChatBaglantiHatasi(string mesaj, Exception? ic = null) : base(mesaj, ic) { }
}
