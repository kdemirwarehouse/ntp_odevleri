using NotepadApp.Core.Models;

namespace NotepadApp.Core.Services;

/// <summary>
/// Notların bellekteki koleksiyonunu (List&lt;Not&gt;) yönetir: ekleme, düzenleme,
/// silme, arama ve kategoriye göre filtreleme.
/// </summary>
public class NotDefteri
{
    public const string TumKategoriler = "(Tümü)";
    public const string TumEtiketler = "(Tüm etiketler)";

    private readonly List<Not> _notlar = new();

    /// <summary>Koleksiyon her değiştiğinde tetiklenir (otomatik kaydetme bu olayı dinler).</summary>
    public event EventHandler? Degisti;

    public IReadOnlyList<Not> Notlar => _notlar;
    public int Sayi => _notlar.Count;

    public Not Ekle(string baslik, string icerik, string? kategori = null, string? rtfIcerik = null,
                    IEnumerable<string>? etiketler = null)
    {
        var not = new Not(baslik, icerik, kategori)
        {
            RtfIcerik = rtfIcerik,
            Etiketler = etiketler is null ? new() : Not.EtiketleriDuzenle(etiketler)
        };
        _notlar.Add(not);
        DegistiBildir();
        return not;
    }

    public void Guncelle(Guid id, string baslik, string icerik, string? kategori, string? rtfIcerik = null,
                         IEnumerable<string>? etiketler = null)
    {
        var not = Bul(id) ?? throw new KeyNotFoundException("Düzenlenecek not bulunamadı.");
        not.Guncelle(baslik, icerik, kategori, rtfIcerik, etiketler);
        DegistiBildir();
    }

    /// <summary>Notu sabitler veya sabitlemesini kaldırır; yeni durumu döndürür.</summary>
    public bool SabitlemeDegistir(Guid id)
    {
        var not = Bul(id) ?? throw new KeyNotFoundException("Not bulunamadı.");
        not.Sabitlenmis = !not.Sabitlenmis;
        not.Surum++;
        DegistiBildir();
        return not.Sabitlenmis;
    }

    public bool Sil(Guid id)
    {
        int silinen = _notlar.RemoveAll(n => n.Id == id);
        if (silinen > 0)
            DegistiBildir();
        return silinen > 0;
    }

    public Not? Bul(Guid id) => _notlar.FirstOrDefault(n => n.Id == id);

    /// <summary>Aynı Id'li notu verilen nesneyle değiştirir (yoksa ekler). Çakışma çözümü / dış değişiklik için.</summary>
    public void Degistir(Not not)
    {
        int i = _notlar.FindIndex(n => n.Id == not.Id);
        if (i >= 0) _notlar[i] = not; else _notlar.Add(not);
        DegistiBildir();
    }

    /// <summary>Notun içeriğini yeni bir Id ile kopyalar ("(kopya)" ekiyle). Çakışmada iki sürümü de tutmak için.</summary>
    public Not KopyasiniEkle(Not kaynak)
    {
        string baslik = kaynak.Baslik.Length + 8 > Not.MaksimumBaslikUzunlugu
            ? kaynak.Baslik[..(Not.MaksimumBaslikUzunlugu - 8)] + " (kopya)"
            : kaynak.Baslik + " (kopya)";
        return Ekle(baslik, kaynak.Icerik, kaynak.Kategori, kaynak.RtfIcerik);
    }

    /// <summary>
    /// Arama metni ve kategoriye göre süzülmüş liste döner.
    /// Sabitlenmiş notlar en üstte, kendi aralarında ve diğerleri son düzenlenme tarihine göre sıralanır.
    /// </summary>
    public List<Not> Filtrele(string? aranan, string? kategori, string? etiket = null)
    {
        IEnumerable<Not> sorgu = _notlar;

        if (!string.IsNullOrWhiteSpace(aranan))
            sorgu = sorgu.Where(n => n.Iceriyor(aranan));

        if (!string.IsNullOrWhiteSpace(kategori) && kategori != TumKategoriler)
            sorgu = sorgu.Where(n => string.Equals(n.Kategori, kategori, StringComparison.CurrentCultureIgnoreCase));

        if (!string.IsNullOrWhiteSpace(etiket) && etiket != TumEtiketler)
            sorgu = sorgu.Where(n => n.EtiketiVar(etiket));

        return sorgu.OrderByDescending(n => n.Sabitlenmis)
                    .ThenByDescending(n => n.DuzenlemeTarihi)
                    .ToList();
    }

    public List<string> Kategoriler() =>
        _notlar.Select(n => n.Kategori)
               .Distinct(StringComparer.CurrentCultureIgnoreCase)
               .OrderBy(k => k)
               .ToList();

    /// <summary>
    /// Dışarıdan (ör. XML arşivinden) gelen notları birleştirir: yeni Id'ler eklenir; aynı Id'li not varsa
    /// son düzenleme tarihi daha yeni olan kazanır.
    /// </summary>
    public (int Eklenen, int Guncellenen, int Atlanan) IceAktar(IEnumerable<Not> gelenler)
    {
        int eklenen = 0, guncellenen = 0, atlanan = 0;
        foreach (var gelen in gelenler)
        {
            gelen.Etiketler ??= new();
            int i = _notlar.FindIndex(n => n.Id == gelen.Id);
            if (i < 0) { _notlar.Add(gelen); eklenen++; }
            else if (gelen.DuzenlemeTarihi > _notlar[i].DuzenlemeTarihi)
            {
                gelen.Surum = Math.Max(gelen.Surum, _notlar[i].Surum) + 1;
                _notlar[i] = gelen;
                guncellenen++;
            }
            else atlanan++;
        }
        if (eklenen + guncellenen > 0)
            DegistiBildir();
        return (eklenen, guncellenen, atlanan);
    }

    /// <summary>Tüm notlardaki etiketler (tekil, alfabetik).</summary>
    public List<string> Etiketler() =>
        _notlar.SelectMany(n => n.Etiketler)
               .Distinct(StringComparer.CurrentCultureIgnoreCase)
               .OrderBy(e => e)
               .ToList();

    /// <summary>Dosyadan yüklenen notlarla koleksiyonun içeriğini değiştirir.</summary>
    public void Yukle(IEnumerable<Not> notlar)
    {
        _notlar.Clear();
        _notlar.AddRange(notlar.Where(n => n is not null && !string.IsNullOrWhiteSpace(n.Baslik)));
        foreach (var n in _notlar)
            n.Etiketler ??= new();   // eski JSON dosyalarında alan yok
    }

    private void DegistiBildir() => Degisti?.Invoke(this, EventArgs.Empty);
}
