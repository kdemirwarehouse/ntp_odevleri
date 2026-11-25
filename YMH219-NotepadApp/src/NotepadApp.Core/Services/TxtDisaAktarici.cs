using System.Text;
using NotepadApp.Core.Models;

namespace NotepadApp.Core.Services;

/// <summary>Bir notu düz metin (.txt) dosyası olarak dışa aktarır.</summary>
public static class TxtDisaAktarici
{
    public static void DisaAktar(Not not, string dosyaYolu)
    {
        ArgumentNullException.ThrowIfNull(not);
        if (string.IsNullOrWhiteSpace(dosyaYolu))
            throw new ArgumentException("Dosya yolu boş olamaz.", nameof(dosyaYolu));

        using var yazici = new StreamWriter(dosyaYolu, false, new UTF8Encoding(true));
        yazici.WriteLine(not.Baslik);
        yazici.WriteLine(new string('=', Math.Min(not.Baslik.Length, 60)));
        yazici.WriteLine($"Kategori      : {not.Kategori}");
        yazici.WriteLine($"Oluşturulma   : {not.OlusturmaTarihi:dd.MM.yyyy HH:mm}");
        yazici.WriteLine($"Son düzenleme : {not.DuzenlemeTarihi:dd.MM.yyyy HH:mm}");
        yazici.WriteLine();
        yazici.Write(not.Icerik);
    }

    /// <summary>Not başlığından geçerli bir dosya adı üretir.</summary>
    public static string DosyaAdiOner(Not not)
    {
        var gecersiz = Path.GetInvalidFileNameChars();
        string ad = new string(not.Baslik.Select(c => gecersiz.Contains(c) ? '_' : c).ToArray()).Trim();
        return (string.IsNullOrEmpty(ad) ? "not" : ad) + ".txt";
    }
}
