using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatApp.Core;

/// <summary>Ağ üzerinden gönderilen mesajın türü.</summary>
public enum MesajTuru
{
    /// <summary>İstemci → sunucu: kullanıcı adıyla katılma isteği.</summary>
    Katil,
    /// <summary>Herkese açık sohbet mesajı.</summary>
    Sohbet,
    /// <summary>Yalnızca bir kullanıcıya giden özel mesaj.</summary>
    Ozel,
    /// <summary>Sunucunun bilgilendirme mesajı (katıldı / ayrıldı ...).</summary>
    Sistem,
    /// <summary>Sunucu → istemci: çevrimiçi kullanıcıların güncel listesi.</summary>
    KullaniciListesi,
    /// <summary>Sunucu → istemci: hata (ör. kullanıcı adı alınmış).</summary>
    Hata,
    /// <summary>İstemci → sunucu: başka bir odaya geçme isteği (Oda alanında yeni oda adı).</summary>
    OdaDegistir
}

/// <summary>
/// Protokoldeki tek mesaj. Her mesaj tek satırlık JSON olarak gönderilir
/// (satır sonu = mesaj sınırı), böylece StreamReader.ReadLineAsync ile okunabilir.
/// </summary>
public sealed record ChatMesaji
{
    public MesajTuru Tur { get; init; }
    public string Gonderen { get; init; } = string.Empty;
    public string? Alici { get; init; }
    public string Metin { get; init; } = string.Empty;
    public DateTime Zaman { get; init; } = DateTime.Now;
    public List<string>? Kullanicilar { get; init; }

    /// <summary>Mesajın ait olduğu sohbet odası (Katil / OdaDegistir isteğinde istenen oda).</summary>
    public string? Oda { get; init; }

    /// <summary>KullaniciListesi mesajında: sunucudaki tüm aktif odalar.</summary>
    public List<string>? Odalar { get; init; }

    public const string VarsayilanOda = "Genel";

    /// <summary>Yalnızca Katil mesajında: sunucu parola istiyorsa istemcinin gönderdiği parola. Hiçbir zaman yayınlanmaz.</summary>
    public string? Parola { get; init; }

    /// <summary>Yeni katılana gönderilen eski (geçmiş) mesaj mı? Arayüz bunları soluk gösterir.</summary>
    public bool? Gecmis { get; init; }

    private static readonly JsonSerializerOptions Ayarlar = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string JsonSatiri() => JsonSerializer.Serialize(this, Ayarlar);

    /// <summary>Gelen satırı mesaja çevirir. Geçersiz satırda <see cref="JsonException"/> fırlatır.</summary>
    public static ChatMesaji JsondanOku(string satir) =>
        JsonSerializer.Deserialize<ChatMesaji>(satir, Ayarlar)
        ?? throw new JsonException("Boş mesaj.");

    /// <summary>Arayüzde gösterilecek "[12:30:05] Ali: merhaba" biçimi.</summary>
    public string GosterimMetni() => Tur switch
    {
        MesajTuru.Sohbet when Gecmis == true => $"[{Zaman:dd.MM HH:mm}] {Gonderen}: {Metin}",
        MesajTuru.Sistem when Gecmis == true => $"[{Zaman:dd.MM HH:mm}] * {Metin}",
        MesajTuru.Sohbet => $"[{Zaman:HH:mm:ss}] {Gonderen}: {Metin}",
        MesajTuru.Ozel => $"[{Zaman:HH:mm:ss}] (özel) {Gonderen} → {Alici}: {Metin}",
        MesajTuru.Hata => $"[{Zaman:HH:mm:ss}] HATA: {Metin}",
        _ => $"[{Zaman:HH:mm:ss}] * {Metin}"
    };

    // Kolay oluşturma yardımcıları
    public static ChatMesaji Sistem(string metin) => new() { Tur = MesajTuru.Sistem, Gonderen = "Sunucu", Metin = metin };
    public static ChatMesaji HataMesaji(string metin) => new() { Tur = MesajTuru.Hata, Gonderen = "Sunucu", Metin = metin };
}
