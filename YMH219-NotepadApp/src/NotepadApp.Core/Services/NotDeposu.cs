using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using NotepadApp.Core.Models;

namespace NotepadApp.Core.Services;

/// <summary>Dosyadan yükleme işleminin sonucu: notlar ve varsa kullanıcıya gösterilecek uyarı.</summary>
public record YuklemeSonucu(List<Not> Notlar, string? Uyari);

/// <summary>
/// Notları JSON biçiminde dosyaya serileştirir ve geri yükler (File I/O + Serialization).
/// Dosya yoksa boş liste döner; dosya bozuksa yedeğini alır ve uyarı üretir.
/// </summary>
public class NotDeposu
{
    private static readonly JsonSerializerOptions JsonAyarlari = new()
    {
        WriteIndented = true,
        // Türkçe karakterler kaçış dizisine çevrilmesin, dosya okunabilir kalsın.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string DosyaYolu { get; }

    public NotDeposu(string dosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(dosyaYolu))
            throw new ArgumentException("Dosya yolu boş olamaz.", nameof(dosyaYolu));
        DosyaYolu = Path.GetFullPath(dosyaYolu);
    }

    /// <summary>Varsayılan konum: %APPDATA%\YMH219-NotepadApp\notlar.json</summary>
    public static string VarsayilanYol() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "YMH219-NotepadApp", "notlar.json");

    public void Kaydet(IEnumerable<Not> notlar)
    {
        string? klasor = Path.GetDirectoryName(DosyaYolu);
        if (!string.IsNullOrEmpty(klasor))
            Directory.CreateDirectory(klasor);

        string json = JsonSerializer.Serialize(notlar.ToList(), JsonAyarlari);

        // Önce geçici dosyaya yazılır, sonra asıl dosyanın yerine taşınır.
        // Yazma yarıda kesilirse eski not dosyası bozulmamış olur.
        string geciciYol = DosyaYolu + ".tmp";
        using (var yazici = new StreamWriter(geciciYol, false, new UTF8Encoding(false)))
        {
            yazici.Write(json);
        }
        File.Move(geciciYol, DosyaYolu, overwrite: true);
    }

    public YuklemeSonucu Yukle()
    {
        if (!File.Exists(DosyaYolu))
            return new YuklemeSonucu(new List<Not>(), null);

        string json;
        using (var okuyucu = new StreamReader(DosyaYolu, Encoding.UTF8))
        {
            json = okuyucu.ReadToEnd();
        }

        if (string.IsNullOrWhiteSpace(json))
            return new YuklemeSonucu(new List<Not>(), "Not dosyası boştu; yeni bir liste başlatıldı.");

        try
        {
            var notlar = JsonSerializer.Deserialize<List<Not>>(json, JsonAyarlari) ?? new List<Not>();
            notlar.RemoveAll(n => n is null || string.IsNullOrWhiteSpace(n.Baslik));
            return new YuklemeSonucu(notlar, null);
        }
        catch (JsonException ex)
        {
            string yedek = DosyaYolu + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak";
            File.Copy(DosyaYolu, yedek, overwrite: true);
            return new YuklemeSonucu(new List<Not>(),
                "Not dosyası bozuk olduğu için okunamadı.\n" +
                $"Ayrıntı: {ex.Message}\n\n" +
                $"Bozuk dosyanın yedeği alındı:\n{yedek}\n\nBoş bir liste ile devam ediliyor.");
        }
    }
}
