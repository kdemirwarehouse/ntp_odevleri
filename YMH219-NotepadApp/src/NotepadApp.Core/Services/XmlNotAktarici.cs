using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NotepadApp.Core.Models;

namespace NotepadApp.Core.Services;

/// <summary>XML dosyasının kök elemanı: &lt;NotArsivi&gt; içinde &lt;Not&gt; listesi.</summary>
[XmlRoot("NotArsivi")]
public class NotArsivi
{
    [XmlAttribute("surum")] public int BicimSurumu { get; set; } = 1;
    [XmlAttribute("olusturma")] public DateTime OlusturmaZamani { get; set; } = DateTime.Now;
    [XmlElement("Not")] public List<Not> Notlar { get; set; } = new();
}

/// <summary>
/// JSON'a alternatif olarak tüm notları <see cref="XmlSerializer"/> ile tek bir XML dosyasına yazar ve geri okur.
/// Yedekleme veya başka bir bilgisayara taşıma için kullanılır.
/// </summary>
public static class XmlNotAktarici
{
    private static readonly XmlSerializer Serilestirici = new(typeof(NotArsivi));

    public static void DisaAktar(IEnumerable<Not> notlar, string dosyaYolu)
    {
        var arsiv = new NotArsivi { Notlar = notlar.ToList() };
        var ayarlar = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using var yazici = XmlWriter.Create(dosyaYolu, ayarlar);
        Serilestirici.Serialize(yazici, arsiv);
    }

    /// <summary>XML arşivini okur. Geçersiz XML'de <see cref="InvalidDataException"/> fırlatır.</summary>
    public static List<Not> IceAktar(string dosyaYolu)
    {
        try
        {
            using var okuyucu = XmlReader.Create(dosyaYolu, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            var arsiv = (NotArsivi?)Serilestirici.Deserialize(okuyucu) ?? new NotArsivi();
            return arsiv.Notlar.Where(n => !string.IsNullOrWhiteSpace(n.Baslik)).ToList();
        }
        catch (InvalidOperationException ex)   // XmlSerializer biçim hatalarını bu türle sarar
        {
            throw new InvalidDataException("XML dosyası geçerli bir not arşivi değil: " + (ex.InnerException?.Message ?? ex.Message), ex);
        }
    }
}
