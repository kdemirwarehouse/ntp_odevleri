namespace NotepadApp.Core.Models;

/// <summary>
/// Tek bir notu temsil eder. JSON serileştirme için parametresiz yapıcı ve
/// public özellikler kullanılır.
/// </summary>
public class Not
{
    public const int MaksimumBaslikUzunlugu = 120;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Baslik { get; set; } = string.Empty;
    public string Icerik { get; set; } = string.Empty;
    public string Kategori { get; set; } = "Genel";

    /// <summary>RichTextBox biçimlendirmesi (kalın/italik) için RTF içeriği. Boş olabilir.</summary>
    public string? RtfIcerik { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime DuzenlemeTarihi { get; set; }

    public Not()
    {
    }

    public Not(string baslik, string icerik, string? kategori = null)
    {
        Baslik = BaslikDogrula(baslik);
        Icerik = icerik ?? string.Empty;
        Kategori = KategoriDuzenle(kategori);
        OlusturmaTarihi = DateTime.Now;
        DuzenlemeTarihi = OlusturmaTarihi;
    }

    /// <summary>Başlık, içerik ve kategoriyi değiştirir, düzenleme tarihini günceller.</summary>
    public void Guncelle(string baslik, string icerik, string? kategori, string? rtfIcerik = null)
    {
        Baslik = BaslikDogrula(baslik);
        Icerik = icerik ?? string.Empty;
        Kategori = KategoriDuzenle(kategori);
        RtfIcerik = rtfIcerik;
        DuzenlemeTarihi = DateTime.Now;
    }

    /// <summary>Başlıkta veya içerikte aranan metin geçiyor mu? (büyük/küçük harf duyarsız)</summary>
    public bool Iceriyor(string aranan)
    {
        if (string.IsNullOrWhiteSpace(aranan))
            return true;

        return Baslik.Contains(aranan, StringComparison.CurrentCultureIgnoreCase)
            || Icerik.Contains(aranan, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>ListBox içinde görünen kısa metin.</summary>
    public override string ToString() => $"{Baslik}   ({DuzenlemeTarihi:dd.MM.yyyy HH:mm})";

    private static string BaslikDogrula(string baslik)
    {
        if (string.IsNullOrWhiteSpace(baslik))
            throw new ArgumentException("Not başlığı boş olamaz.", nameof(baslik));

        baslik = baslik.Trim();
        if (baslik.Length > MaksimumBaslikUzunlugu)
            throw new ArgumentException($"Başlık en fazla {MaksimumBaslikUzunlugu} karakter olabilir.", nameof(baslik));

        return baslik;
    }

    private static string KategoriDuzenle(string? kategori) =>
        string.IsNullOrWhiteSpace(kategori) ? "Genel" : kategori.Trim();
}
