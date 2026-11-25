using System.Text;

namespace SpeechApp.Core;

/// <summary>Konuşma tanıma motorunun ürettiği tek bir sonuç.</summary>
public sealed record TanimaKaydi(DateTime Zaman, string Metin, float Guven)
{
    /// <summary>Güven skoru yüzde olarak (0-100).</summary>
    public int GuvenYuzde => (int)Math.Round(Math.Clamp(Guven, 0f, 1f) * 100);

    public string SatirMetni() => $"[{Zaman:HH:mm:ss}] ({GuvenYuzde}%) {Metin}";
}

/// <summary>
/// Tanınan cümleleri zaman damgasıyla tutar; düşük güvenli sonuçları ayırır ve .txt'ye aktarır.
/// </summary>
public class TanimaGecmisi
{
    private readonly List<TanimaKaydi> _kayitlar = new();

    public TanimaGecmisi(float minimumGuven = 0.3f)
    {
        if (minimumGuven is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(minimumGuven));
        MinimumGuven = minimumGuven;
    }

    /// <summary>Bu değerin altındaki sonuçlar "emin değilim" olarak sayılır ve metne eklenmez.</summary>
    public float MinimumGuven { get; }

    public IReadOnlyList<TanimaKaydi> Kayitlar => _kayitlar;

    /// <summary>Sonucu ekler. Boş metin veya düşük güvenli sonuç için false döner.</summary>
    public bool Ekle(string metin, float guven, DateTime? zaman = null)
    {
        if (string.IsNullOrWhiteSpace(metin) || guven < MinimumGuven)
            return false;
        _kayitlar.Add(new TanimaKaydi(zaman ?? DateTime.Now, metin.Trim(), guven));
        return true;
    }

    public void Temizle() => _kayitlar.Clear();

    /// <summary>Tüm tanınan cümleleri tek bir metin olarak birleştirir.</summary>
    public string BirlesikMetin() => string.Join(" ", _kayitlar.Select(k => k.Metin));

    public double OrtalamaGuven => _kayitlar.Count == 0 ? 0 : _kayitlar.Average(k => k.Guven);

    public void TxtOlarakKaydet(string dosyaYolu)
    {
        using var yazici = new StreamWriter(dosyaYolu, false, new UTF8Encoding(true));
        yazici.WriteLine("# YMH219 SpeechApp - konuşma tanıma sonuçları");
        yazici.WriteLine($"# {DateTime.Now:dd.MM.yyyy HH:mm:ss}, {_kayitlar.Count} cümle");
        foreach (var k in _kayitlar)
            yazici.WriteLine(k.SatirMetni());
        yazici.WriteLine();
        yazici.WriteLine(BirlesikMetin());
    }
}
