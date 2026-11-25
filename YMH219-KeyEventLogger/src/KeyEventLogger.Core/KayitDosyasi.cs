using System.Text;

namespace KeyEventLogger.Core;

/// <summary>Kayıtları kullanıcının seçtiği .txt dosyasına yazar (StreamWriter).</summary>
public static class KayitDosyasi
{
    public static void Kaydet(IEnumerable<KeyLogEntry> kayitlar, string dosyaYolu)
    {
        ArgumentNullException.ThrowIfNull(kayitlar);
        if (string.IsNullOrWhiteSpace(dosyaYolu))
            throw new ArgumentException("Dosya yolu boş olamaz.", nameof(dosyaYolu));

        var liste = kayitlar.ToList();
        using var yazici = new StreamWriter(dosyaYolu, append: false, new UTF8Encoding(true));
        yazici.WriteLine("# YMH219 Key Event Logger - uygulama içi klavye olayları");
        yazici.WriteLine($"# Dışa aktarma zamanı: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        yazici.WriteLine($"# Olay sayısı: {liste.Count}");
        yazici.WriteLine("# Zaman\tOlay\tTuş\tAyrıntı");
        foreach (var kayit in liste)
            yazici.WriteLine(kayit.SatirMetni());
    }

    public static string VarsayilanDosyaAdi() => $"klavye-olaylari-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
}
