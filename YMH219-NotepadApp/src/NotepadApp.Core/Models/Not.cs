namespace NotepadApp.Core.Models;

/// <summary>
/// Tek bir notu temsil eder. JSON serileştirme için parametresiz yapıcı ve
/// public özellikler kullanılır.
/// </summary>
public class Not
{
    public const int MaksimumBaslikUzunlugu = 120;
    public const int MaksimumEtiketSayisi = 10;
    public const int MaksimumEtiketUzunlugu = 30;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Baslik { get; set; } = string.Empty;
    public string Icerik { get; set; } = string.Empty;
    public string Kategori { get; set; } = "Genel";

    /// <summary>RichTextBox biçimlendirmesi (kalın/italik) için RTF içeriği. Boş olabilir.</summary>
    public string? RtfIcerik { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    /// <summary>
    /// Her güncellemede bir artar. Aynı not iki pencereden düzenlenirse, diskteki sürüm
    /// pencerenin bildiğinden farklıysa kaydetme çakışma olarak bildirilir (iyimser eşzamanlılık).
    /// </summary>
    public int Surum { get; set; }

    /// <summary>
    /// Etiketler: bir not birden fazla etikete sahip olabilir (kategori tek, etiket çoklu).
    /// Ör. kategori "Okul", etiketler "sınav, matematik, acil".
    /// </summary>
    public List<string> Etiketler { get; set; } = new();

    /// <summary>Sabitlenen notlar listede her zaman en üstte gösterilir.</summary>
    public bool Sabitlenmis { get; set; }
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
    public void Guncelle(string baslik, string icerik, string? kategori, string? rtfIcerik = null,
                         IEnumerable<string>? etiketler = null)
    {
        Baslik = BaslikDogrula(baslik);
        if (etiketler is not null)
            Etiketler = EtiketleriDuzenle(etiketler);
        Icerik = icerik ?? string.Empty;
        Kategori = KategoriDuzenle(kategori);
        RtfIcerik = rtfIcerik;
        DuzenlemeTarihi = DateTime.Now;
        Surum++;
    }

    /// <summary>Başlıkta veya içerikte aranan metin geçiyor mu? (büyük/küçük harf duyarsız)</summary>
    public bool Iceriyor(string aranan)
    {
        if (string.IsNullOrWhiteSpace(aranan))
            return true;

        return Baslik.Contains(aranan, StringComparison.CurrentCultureIgnoreCase)
            || Icerik.Contains(aranan, StringComparison.CurrentCultureIgnoreCase)
            || Etiketler.Any(e => e.Contains(aranan.TrimStart('#'), StringComparison.CurrentCultureIgnoreCase));
    }

    public bool EtiketiVar(string etiket) =>
        Etiketler.Any(e => string.Equals(e, etiket, StringComparison.CurrentCultureIgnoreCase));

    /// <summary>Düzenleyicide gösterilen virgüllü etiket metni.</summary>
    public string EtiketMetni => string.Join(", ", Etiketler);

    /// <summary>"okul, #proje; acil" gibi bir metni etiket listesine çevirir (virgül, noktalı virgül ve # ayırıcıdır).</summary>
    public static List<string> EtiketleriAyristir(string? metin) =>
        EtiketleriDuzenle((metin ?? "").Split(new[] { ',', ';', '#' }, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Boşlukları kırpar, tekrarları (büyük/küçük harf duyarsız) atar ve sınırları uygular.</summary>
    public static List<string> EtiketleriDuzenle(IEnumerable<string> etiketler)
    {
        var sonuc = new List<string>();
        foreach (var ham in etiketler)
        {
            string e = ham.Trim();
            if (e.Length == 0) continue;
            if (e.Length > MaksimumEtiketUzunlugu)
                throw new ArgumentException($"Bir etiket en fazla {MaksimumEtiketUzunlugu} karakter olabilir: \"{e}\"");
            if (sonuc.Any(x => string.Equals(x, e, StringComparison.CurrentCultureIgnoreCase))) continue;
            sonuc.Add(e);
        }
        if (sonuc.Count > MaksimumEtiketSayisi)
            throw new ArgumentException($"Bir notta en fazla {MaksimumEtiketSayisi} etiket olabilir.");
        return sonuc;
    }

    /// <summary>ListBox içinde görünen kısa metin.</summary>
    public override string ToString() =>
        $"{(Sabitlenmis ? "📌 " : "")}{Baslik}{(Etiketler.Count > 0 ? "  " + string.Join(" ", Etiketler.Select(e => "#" + e)) : "")}   ({DuzenlemeTarihi:dd.MM.yyyy HH:mm})";

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
