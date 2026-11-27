namespace NotepadApp.Core.Services;

/// <summary>
/// Genel amaçlı, sınırlı kapasiteli geri al / yinele geçmişi (iki yığın: geri al ve yinele).
/// <para>
/// RichTextBox'ın kendi geri alması biçimlendirme değişikliklerini ve not değiştirildiğinde geçmişi tutarlı
/// izleyemez; bu sınıf ise düzenleyicinin <b>tam durumunu</b> (RTF + imleç) anlık görüntü olarak saklar.
/// Yeni bir durum kaydedildiğinde yinele yığını temizlenir (standart düzenleyici davranışı).
/// </para>
/// </summary>
public class GeriAlGecmisi<T>
{
    private readonly LinkedList<T> _geriAl = new();
    private readonly Stack<T> _yinele = new();
    private readonly IEqualityComparer<T> _karsilastirici;

    public GeriAlGecmisi(int kapasite = 200, IEqualityComparer<T>? karsilastirici = null)
    {
        if (kapasite < 2)
            throw new ArgumentOutOfRangeException(nameof(kapasite), "Kapasite en az 2 olmalı.");
        Kapasite = kapasite;
        _karsilastirici = karsilastirici ?? EqualityComparer<T>.Default;
    }

    public int Kapasite { get; }

    /// <summary>Şu anki durum (en son kaydedilen); hiç kayıt yoksa default.</summary>
    public T? Simdiki => _geriAl.Count > 0 ? _geriAl.Last!.Value : default;

    public bool GeriAlinabilir => _geriAl.Count > 1;
    public bool Yinelenebilir => _yinele.Count > 0;
    public int GeriAlAdimSayisi => Math.Max(0, _geriAl.Count - 1);
    public int YineleAdimSayisi => _yinele.Count;

    /// <summary>Geçmişi tek bir başlangıç durumuyla sıfırlar (ör. başka not açıldığında).</summary>
    public void Sifirla(T baslangic)
    {
        _geriAl.Clear();
        _yinele.Clear();
        _geriAl.AddLast(baslangic);
    }

    /// <summary>Yeni durumu kaydeder. Şu anki durumla aynıysa yok sayılır ve false döner.</summary>
    public bool Kaydet(T durum)
    {
        if (_geriAl.Count > 0 && _karsilastirici.Equals(_geriAl.Last!.Value, durum))
            return false;
        _geriAl.AddLast(durum);
        while (_geriAl.Count > Kapasite)
            _geriAl.RemoveFirst();   // en eski adım düşer
        _yinele.Clear();
        return true;
    }

    /// <summary>Bir önceki duruma döner ve onu verir; geri alınacak adım yoksa default.</summary>
    public T? GeriAl()
    {
        if (!GeriAlinabilir) return default;
        _yinele.Push(_geriAl.Last!.Value);
        _geriAl.RemoveLast();
        return _geriAl.Last!.Value;
    }

    /// <summary>Geri alınan durumu yeniden uygular; yinelenecek adım yoksa default.</summary>
    public T? Yinele()
    {
        if (!Yinelenebilir) return default;
        var durum = _yinele.Pop();
        _geriAl.AddLast(durum);
        return durum;
    }
}
