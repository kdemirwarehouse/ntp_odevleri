using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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
    private (string Sunucu, int Port, string Ad, string? Parola, bool Tls)? _sonBaglanti;
    private string? _parmakIziHatasi;
    private string _istenenOda = ChatMesaji.VarsayilanOda;
    private CancellationTokenSource? _yenidenBaglanmaIptal;

    public event EventHandler<ChatMesaji>? MesajAlindi;

    /// <summary>Bağlantı beklenmedik şekilde koptuğunda sebebiyle tetiklenir.</summary>
    public event EventHandler<string>? BaglantiKoptu;

    /// <summary>
    /// Bağlantı beklenmedik şekilde koparsa (sunucu yeniden başlatıldı, ağ kesildi ...) istemci
    /// artan bekleme süreleriyle (1, 2, 4, 8 sn ... en fazla 10 sn) kendiliğinden yeniden bağlanmayı dener.
    /// </summary>
    public bool OtomatikYenidenBaglan { get; set; }

    /// <summary>Yeniden bağlanma denemesi sayısı üst sınırı.</summary>
    public int MaksimumYenidenBaglanmaDenemesi { get; set; } = 10;

    /// <summary>İlk bekleme süresi; her denemede iki katına çıkar (testlerde kısaltılabilir).</summary>
    public TimeSpan IlkBekleme { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Yeniden bağlanma denemesi başlamadan önce (deneme no, bekleme süresi) ile tetiklenir.</summary>
    public event EventHandler<(int Deneme, TimeSpan Bekleme)>? YenidenBaglaniyor;

    /// <summary>Otomatik yeniden bağlanma başarılı olduğunda tetiklenir.</summary>
    public event EventHandler? YenidenBaglandi;

    public bool Bagli => _baglanti is not null;

    /// <summary>Bağlantı TLS ile şifreli mi?</summary>
    public bool Sifreli => _baglanti?.Sifreli == true;

    /// <summary>TLS bağlantısında sunucu sertifikasının SHA-256 parmak izi.</summary>
    public string? SunucuParmakIzi { get; private set; }

    /// <summary>Son bağlantıda sunucu ilk kez görüldü ve parmak izi kaydedildi mi (TOFU)?</summary>
    public bool YeniSunucuKaydedildi { get; private set; }

    /// <summary>TOFU parmak izi deposu (testlerde geçici dosya verilebilir).</summary>
    public BilinenSunucular BilinenSunucular { get; set; } = new();

    /// <summary>Şu anda otomatik yeniden bağlanma döngüsü çalışıyor mu?</summary>
    public bool YenidenBaglaniyorMu => _yenidenBaglanmaIptal is not null;
    public string KullaniciAdi { get; private set; } = string.Empty;

    /// <summary>Sunucunun bildirdiği güncel oda.</summary>
    public string Oda { get; private set; } = ChatMesaji.VarsayilanOda;

    /// <summary>
    /// Sunucuya bağlanır ve kullanıcı adıyla katılır. Sunucu reddederse (ör. ad alınmışsa)
    /// <see cref="ChatBaglantiHatasi"/> fırlatılır.
    /// </summary>
    public async Task BaglanAsync(string sunucu, int port, string kullaniciAdi, TimeSpan? zamanAsimi = null,
                                  string? parola = null, string? oda = null, bool tls = false)
    {
        if (!string.IsNullOrWhiteSpace(oda))
            _istenenOda = oda.Trim();
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
            if (tls)
            {
                var ssl = new SslStream(tcp.GetStream(), false, (_, sertifika, _, hatalar) =>
                    SunucuSertifikasiniDogrula($"{sunucu.Trim()}:{port}", sertifika, hatalar));
                SunucuParmakIzi = null; YeniSunucuKaydedildi = false; _parmakIziHatasi = null;
                try
                {
                    await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = sunucu.Trim(),
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    }, iptal.Token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is AuthenticationException or IOException)
                {
                    ssl.Dispose();
                    throw new ChatBaglantiHatasi(_parmakIziHatasi
                        ?? $"TLS el sıkışması başarısız. Sunucu TLS kullanmıyor olabilir ({ex.Message}).", ex);
                }
                baglanti = new JsonSatirBaglantisi(tcp, ssl);
            }
            else
            {
                baglanti = new JsonSatirBaglantisi(tcp);
            }
            await baglanti.GonderAsync(new ChatMesaji { Tur = MesajTuru.Katil, Gonderen = kullaniciAdi!.Trim(), Parola = string.IsNullOrEmpty(parola) ? null : parola, Oda = _istenenOda }, iptal.Token)
                          .ConfigureAwait(false);

            // Sunucunun ilk cevabı: hoş geldin (Sistem) ya da Hata
            var cevap = await baglanti.OkuAsync(iptal.Token).ConfigureAwait(false)
                        ?? throw new ChatBaglantiHatasi(tls ? "Sunucu bağlantıyı kapattı."
                                                            : "Sunucu bağlantıyı kapattı (sunucu TLS istiyor olabilir; 'TLS' kutusunu işaretleyin).");
            if (cevap.Tur == MesajTuru.Hata)
                throw new ChatBaglantiHatasi(cevap.Metin);

            KullaniciAdi = kullaniciAdi.Trim();
            _sonBaglanti = (sunucu.Trim(), port, KullaniciAdi, parola, tls);
            Oda = cevap.Oda ?? ChatMesaji.VarsayilanOda;
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

    /// <summary>
    /// TLS sunucu doğrulaması. Geçerli bir CA zinciri varsa kabul edilir. Kendinden imzalı sertifikada
    /// parmak izi ilk bağlantıda kaydedilir (TOFU); sonraki bağlantılarda değişmişse reddedilir.
    /// </summary>
    private bool SunucuSertifikasiniDogrula(string anahtar, X509Certificate? sertifika, SslPolicyErrors hatalar)
    {
        if (sertifika is null || hatalar.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
            return false;

        SunucuParmakIzi = TlsYardimcisi.ParmakIzi(sertifika);
        if (hatalar == SslPolicyErrors.None)
            return true;

        string? bilinen = BilinenSunucular.Bul(anahtar);
        if (bilinen is null)
        {
            BilinenSunucular.Kaydet(anahtar, SunucuParmakIzi);
            YeniSunucuKaydedildi = true;
            return true;
        }
        if (bilinen == SunucuParmakIzi)
            return true;

        _parmakIziHatasi = $"GÜVENLİK UYARISI: {anahtar} sunucusunun sertifikası değişmiş!\n" +
                           $"Kayıtlı: {bilinen}\nGelen:   {SunucuParmakIzi}\n" +
                           "Araya giren bir sunucu olabilir. Sunucu sertifikası bilerek yenilendiyse kayıtlı parmak izini silin.";
        return false;
    }

    /// <summary>Başka bir odaya geçer; yeniden bağlanınca da bu odaya katılınır.</summary>
    public Task OdaDegistirAsync(string oda)
    {
        string? hata = ChatSunucu.OdaAdiHatasi(oda);
        if (hata is not null)
            throw new ChatBaglantiHatasi(hata);
        _istenenOda = oda.Trim();
        return GonderAsync(new ChatMesaji { Tur = MesajTuru.OdaDegistir, Gonderen = KullaniciAdi, Oda = _istenenOda, Metin = "-" });
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
                if (mesaj.Tur == MesajTuru.KullaniciListesi && mesaj.Oda is not null)
                    Oda = mesaj.Oda;
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

    /// <summary>Kullanıcının "Bağlantıyı Kes" isteği (devam eden yeniden bağlanmayı da durdurur).</summary>
    public void BaglantiyiKes()
    {
        YenidenBaglanmayiDurdur();
        Kapat(beklenmedik: false, null);
    }

    private void YenidenBaglanmayiDurdur()
    {
        var iptal = Interlocked.Exchange(ref _yenidenBaglanmaIptal, null);
        iptal?.Cancel();
    }

    /// <summary>
    /// Artan beklemeli yeniden bağlanma döngüsü. Sunucu yeni açılmışsa eski oturumun adı bir süre
    /// "kullanımda" görünebilir; bu da başarısız deneme sayılır ve tekrar denenir.
    /// </summary>
    private async Task YenidenBaglanAsync(string ilkSebep)
    {
        var bilgi = _sonBaglanti!.Value;
        var iptal = new CancellationTokenSource();
        if (Interlocked.CompareExchange(ref _yenidenBaglanmaIptal, iptal, null) is not null)
            return; // zaten çalışıyor

        TimeSpan bekleme = IlkBekleme;
        string sonHata = ilkSebep;
        try
        {
            for (int deneme = 1; deneme <= MaksimumYenidenBaglanmaDenemesi; deneme++)
            {
                YenidenBaglaniyor?.Invoke(this, (deneme, bekleme));
                await Task.Delay(bekleme, iptal.Token).ConfigureAwait(false);
                try
                {
                    await BaglanAsync(bilgi.Sunucu, bilgi.Port, bilgi.Ad, parola: bilgi.Parola, tls: bilgi.Tls).ConfigureAwait(false);
                    if (iptal.IsCancellationRequested) { Kapat(beklenmedik: false, null); return; }
                    YenidenBaglandi?.Invoke(this, EventArgs.Empty);
                    return;
                }
                catch (ChatBaglantiHatasi ex)
                {
                    sonHata = ex.Message;
                }
                bekleme = TimeSpan.FromTicks(Math.Min(bekleme.Ticks * 2, TimeSpan.FromSeconds(10).Ticks));
            }
            BaglantiKoptu?.Invoke(this, $"{ilkSebep} Yeniden bağlanılamadı ({MaksimumYenidenBaglanmaDenemesi} deneme). Son hata: {sonHata}");
        }
        catch (OperationCanceledException)
        {
            // Kullanıcı "Bağlantıyı Kes" dedi
        }
        finally
        {
            Interlocked.CompareExchange(ref _yenidenBaglanmaIptal, null, iptal);
            iptal.Dispose();
        }
    }

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
        {
            if (OtomatikYenidenBaglan && _sonBaglanti is not null)
                _ = YenidenBaglanAsync(sebep ?? "Bağlantı koptu.");
            else
                BaglantiKoptu?.Invoke(this, sebep ?? "Bağlantı koptu.");
        }
    }

    public void Dispose() => BaglantiyiKes();
}

/// <summary>Kullanıcıya gösterilebilecek anlaşılır bağlantı hatası.</summary>
public sealed class ChatBaglantiHatasi : Exception
{
    public ChatBaglantiHatasi(string mesaj, Exception? ic = null) : base(mesaj, ic) { }
}
