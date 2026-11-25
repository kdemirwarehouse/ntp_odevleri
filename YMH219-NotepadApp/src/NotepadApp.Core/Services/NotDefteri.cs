using NotepadApp.Core.Models;

namespace NotepadApp.Core.Services;

/// <summary>
/// Notların bellekteki koleksiyonunu (List&lt;Not&gt;) yönetir: ekleme, düzenleme,
/// silme, arama ve kategoriye göre filtreleme.
/// </summary>
public class NotDefteri
{
    public const string TumKategoriler = "(Tümü)";

    private readonly List<Not> _notlar = new();

    /// <summary>Koleksiyon her değiştiğinde tetiklenir (otomatik kaydetme bu olayı dinler).</summary>
    public event EventHandler? Degisti;

    public IReadOnlyList<Not> Notlar => _notlar;
    public int Sayi => _notlar.Count;

    public Not Ekle(string baslik, string icerik, string? kategori = null, string? rtfIcerik = null)
    {
        var not = new Not(baslik, icerik, kategori) { RtfIcerik = rtfIcerik };
        _notlar.Add(not);
        DegistiBildir();
        return not;
    }

    public void Guncelle(Guid id, string baslik, string icerik, string? kategori, string? rtfIcerik = null)
    {
        var not = Bul(id) ?? throw new KeyNotFoundException("Düzenlenecek not bulunamadı.");
        not.Guncelle(baslik, icerik, kategori, rtfIcerik);
        DegistiBildir();
    }

    public bool Sil(Guid id)
    {
        int silinen = _notlar.RemoveAll(n => n.Id == id);
        if (silinen > 0)
            DegistiBildir();
        return silinen > 0;
    }

    public Not? Bul(Guid id) => _notlar.FirstOrDefault(n => n.Id == id);

    /// <summary>
    /// Arama metni ve kategoriye göre süzülmüş liste döner.
    /// Son düzenlenen not en üstte yer alır.
    /// </summary>
    public List<Not> Filtrele(string? aranan, string? kategori)
    {
        IEnumerable<Not> sorgu = _notlar;

        if (!string.IsNullOrWhiteSpace(aranan))
            sorgu = sorgu.Where(n => n.Iceriyor(aranan));

        if (!string.IsNullOrWhiteSpace(kategori) && kategori != TumKategoriler)
            sorgu = sorgu.Where(n => string.Equals(n.Kategori, kategori, StringComparison.CurrentCultureIgnoreCase));

        return sorgu.OrderByDescending(n => n.DuzenlemeTarihi).ToList();
    }

    public List<string> Kategoriler() =>
        _notlar.Select(n => n.Kategori)
               .Distinct(StringComparer.CurrentCultureIgnoreCase)
               .OrderBy(k => k)
               .ToList();

    /// <summary>Dosyadan yüklenen notlarla koleksiyonun içeriğini değiştirir.</summary>
    public void Yukle(IEnumerable<Not> notlar)
    {
        _notlar.Clear();
        _notlar.AddRange(notlar.Where(n => n is not null && !string.IsNullOrWhiteSpace(n.Baslik)));
    }

    private void DegistiBildir() => Degisti?.Invoke(this, EventArgs.Empty);
}
