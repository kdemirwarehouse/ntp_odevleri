using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ChatApp.Core;

/// <summary>
/// TLS (SslStream) için sertifika üretimi ve istemci tarafında sunucu doğrulaması.
/// <para>
/// Ders/yerel ağ ortamında bir sertifika otoritesi (CA) olmadığından sunucu kendinden imzalı bir sertifika üretir.
/// İstemci bu sertifikayı ilk bağlantıda görür ve SHA-256 parmak izini kaydeder (TOFU: trust on first use,
/// SSH'deki known_hosts mantığı). Sonraki bağlantılarda parmak izi değişirse bağlantı reddedilir; böylece
/// araya giren (man-in-the-middle) bir sunucu fark edilir.
/// </para>
/// </summary>
public static class TlsYardimcisi
{
    public static string VarsayilanSertifikaDosyasi => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YMH219-ChatApp", "sunucu-sertifikasi.pfx");

    public static string VarsayilanBilinenSunucularDosyasi => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YMH219-ChatApp", "bilinen-sunucular.txt");

    /// <summary>Kayıtlı sertifikayı yükler; yoksa yeni bir kendinden imzalı sertifika üretip kaydeder.</summary>
    public static X509Certificate2 SertifikaYukleVeyaOlustur(string? dosya = null)
    {
        dosya ??= VarsayilanSertifikaDosyasi;
        if (File.Exists(dosya))
        {
            try { return new X509Certificate2(dosya, (string?)null, X509KeyStorageFlags.Exportable); }
            catch (CryptographicException) { /* bozuk dosya: yeniden üret */ }
        }

        using var rsa = RSA.Create(2048);
        var istek = new CertificateRequest("CN=YMH219-ChatApp Sunucusu", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddDnsName(Environment.MachineName);
        san.AddIpAddress(System.Net.IPAddress.Loopback);
        istek.CertificateExtensions.Add(san.Build());
        istek.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        istek.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

        using var gecici = istek.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));
        byte[] pfx = gecici.Export(X509ContentType.Pfx);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dosya)!);
            File.WriteAllBytes(dosya, pfx);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* kaydedilemezse bu oturumluk kullan */ }

        // Windows SChannel, bellekte üretilen geçici anahtarlarla çalışmaz; PFX'ten yeniden yüklenir.
        return new X509Certificate2(pfx, (string?)null, X509KeyStorageFlags.Exportable);
    }

    /// <summary>Sertifikanın SHA-256 parmak izi (AA:BB:... biçiminde).</summary>
    public static string ParmakIzi(X509Certificate sertifika) =>
        Convert.ToHexString(SHA256.HashData(sertifika.GetRawCertData())).Chunk(2)
               .Select(c => new string(c)).Aggregate((a, b) => a + ":" + b);
}

/// <summary>Sunucu parmak izlerini "sunucu:port=parmakizi" satırlarıyla saklayan TOFU deposu.</summary>
public sealed class BilinenSunucular
{
    private readonly string _dosya;
    private readonly object _kilit = new();

    public BilinenSunucular(string? dosya = null) => _dosya = dosya ?? TlsYardimcisi.VarsayilanBilinenSunucularDosyasi;

    public string? Bul(string anahtar)
    {
        lock (_kilit)
        {
            if (!File.Exists(_dosya)) return null;
            foreach (var satir in File.ReadAllLines(_dosya))
            {
                int i = satir.IndexOf('=');
                if (i > 0 && satir[..i].Equals(anahtar, StringComparison.OrdinalIgnoreCase))
                    return satir[(i + 1)..].Trim();
            }
            return null;
        }
    }

    public void Kaydet(string anahtar, string parmakIzi)
    {
        lock (_kilit)
        {
            var satirlar = File.Exists(_dosya)
                ? File.ReadAllLines(_dosya).Where(s => !s.StartsWith(anahtar + "=", StringComparison.OrdinalIgnoreCase)).ToList()
                : new List<string>();
            satirlar.Add($"{anahtar}={parmakIzi}");
            Directory.CreateDirectory(Path.GetDirectoryName(_dosya)!);
            File.WriteAllLines(_dosya, satirlar);
        }
    }

    public void Sil(string anahtar)
    {
        lock (_kilit)
        {
            if (!File.Exists(_dosya)) return;
            File.WriteAllLines(_dosya, File.ReadAllLines(_dosya).Where(s => !s.StartsWith(anahtar + "=", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
