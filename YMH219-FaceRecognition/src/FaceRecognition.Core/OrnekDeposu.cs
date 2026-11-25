namespace FaceRecognition.Core;

/// <summary>Bir etiket ve o etikete ait örnek görüntü dosyaları.</summary>
public sealed record EtiketBilgisi(string Etiket, int Id, IReadOnlyList<string> Dosyalar)
{
    public int OrnekSayisi => Dosyalar.Count;
}

/// <summary>
/// Yerel yüz örneklerini klasör yapısında saklar:
///   ornekler/
///     Ali/   001.png, 002.png ...
///     Ayşe/  001.png ...
/// Klasör adı = etiket. Görüntü işleme yapmaz; yalnızca dosya/klasör yönetimi (OpenCV'den bağımsız, test edilebilir).
/// </summary>
public class OrnekDeposu
{
    public static readonly string[] Uzantilar = { ".png", ".jpg", ".jpeg", ".bmp" };

    public OrnekDeposu(string kokKlasor)
    {
        if (string.IsNullOrWhiteSpace(kokKlasor))
            throw new ArgumentException("Klasör yolu boş olamaz.", nameof(kokKlasor));
        KokKlasor = Path.GetFullPath(kokKlasor);
    }

    public string KokKlasor { get; }

    public static string VarsayilanKlasor() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "YMH219-FaceRecognition", "ornekler");

    /// <summary>
    /// Etiketleri alfabetik sırada döner. Her etikete 0'dan başlayan sayısal Id verilir
    /// (LBPH tanıyıcısı etiketleri sayı olarak ister).
    /// </summary>
    public List<EtiketBilgisi> Etiketler()
    {
        if (!Directory.Exists(KokKlasor))
            return new List<EtiketBilgisi>();

        return Directory.GetDirectories(KokKlasor)
            .Select(Path.GetFileName)
            .Where(ad => ad is not null && EtiketKurallari.Hata(ad) is null)
            .OrderBy(ad => ad, StringComparer.CurrentCultureIgnoreCase)
            .Select((ad, i) => new EtiketBilgisi(ad!, i, OrnekDosyalari(ad!)))
            .ToList();
    }

    public IReadOnlyList<string> OrnekDosyalari(string etiket)
    {
        string klasor = Path.Combine(KokKlasor, EtiketKurallari.Normallestir(etiket));
        if (!Directory.Exists(klasor))
            return Array.Empty<string>();
        return Directory.GetFiles(klasor)
            .Where(f => Uzantilar.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Etiket klasörünü oluşturur ve kaydedilecek bir sonraki örnek dosyasının yolunu döner.</summary>
    public string YeniOrnekYolu(string etiket)
    {
        string ad = EtiketKurallari.Normallestir(etiket);
        string klasor = Path.Combine(KokKlasor, ad);
        Directory.CreateDirectory(klasor);

        int sonraki = Directory.GetFiles(klasor)
            .Select(f => int.TryParse(Path.GetFileNameWithoutExtension(f), out int n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        return Path.Combine(klasor, $"{sonraki:000}.png");
    }

    public bool EtiketSil(string etiket)
    {
        string klasor = Path.Combine(KokKlasor, EtiketKurallari.Normallestir(etiket));
        if (!Directory.Exists(klasor))
            return false;
        Directory.Delete(klasor, recursive: true);
        return true;
    }

    /// <summary>Tanıma için en az iki etiket ve her etikette en az bir örnek gerekir.</summary>
    public string? EgitimHatasi(int minimumEtiket = 2)
    {
        var etiketler = Etiketler().Where(e => e.OrnekSayisi > 0).ToList();
        if (etiketler.Count < minimumEtiket)
            return $"Eğitim için en az {minimumEtiket} farklı etikette örnek gerekli (şu an {etiketler.Count}).";
        return null;
    }
}
