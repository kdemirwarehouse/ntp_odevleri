using FaceRecognition.Core;
using OpenCvSharp;
using OpenCvSharp.Face;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace FaceRecognition.Services;

/// <summary>Bir karede bulunan yüz ve (model eğitilmişse) tanıma kararı.</summary>
public sealed record AlgilananYuz(Rect Bolge, TanimaKarari? Karar);

/// <summary>
/// OpenCV ile yüz algılama (Haar Cascade) ve yüz tanıma (LBPH) işlemlerini yürütür.
/// Arayüzden bağımsızdır; form yalnızca Mat verir ve sonuçları çizer.
/// </summary>
public sealed class GoruntuServisi : IDisposable
{
    /// <summary>Tüm yüzler bu boyuta getirilerek karşılaştırılır.</summary>
    public static readonly Size YuzBoyutu = new(100, 100);

    private readonly CascadeClassifier _algilayici;
    private readonly object _modelKilidi = new();
    private LBPHFaceRecognizer? _tanici;
    private Dictionary<int, string> _etiketAdlari = new();

    /// <summary>Model dosyası bulunamazsa veya bozuksa FileNotFoundException / InvalidDataException fırlatır.</summary>
    public GoruntuServisi(string cascadeYolu)
    {
        if (!File.Exists(cascadeYolu))
            throw new FileNotFoundException(
                "Yüz algılama modeli bulunamadı. 'Models\\haarcascade_frontalface_default.xml' dosyası uygulama klasöründe olmalı.",
                cascadeYolu);

        _algilayici = new CascadeClassifier(cascadeYolu);
        if (_algilayici.Empty())
        {
            _algilayici.Dispose();
            throw new InvalidDataException("Yüz algılama modeli yüklenemedi (dosya bozuk olabilir): " + cascadeYolu);
        }
    }

    public double Esik { get; set; } = TanimaKarari.VarsayilanEsik;
    public bool ModelHazir { get { lock (_modelKilidi) return _tanici is not null; } }
    public IReadOnlyCollection<string> ModeldekiEtiketler { get { lock (_modelKilidi) return _etiketAdlari.Values.ToList(); } }

    // ------------------------------------------------------------------ Algılama

    /// <summary>Renkli görüntüdeki tüm yüzleri bulur (birden fazla yüz desteklenir).</summary>
    public Rect[] YuzleriBul(Mat renkli)
    {
        using var gri = GriyeCevir(renkli);
        Cv2.EqualizeHist(gri, gri);
        int minKenar = Math.Max(40, Math.Min(gri.Width, gri.Height) / 10);
        return _algilayici.DetectMultiScale(gri, scaleFactor: 1.1, minNeighbors: 9,
            flags: HaarDetectionTypes.ScaleImage, minSize: new Size(minKenar, minKenar));
    }

    /// <summary>Yüz bölgesini gri, 100x100, histogramı eşitlenmiş hâle getirir (tanıma girdisi).</summary>
    public static Mat YuzuHazirla(Mat renkli, Rect bolge)
    {
        // Haar kutusu saç/arka plan da içerir; kenarlardan biraz kırpılarak yüzün kendisine odaklanılır.
        int dx = bolge.Width / 10, dy = bolge.Height / 10;
        bolge = new Rect(bolge.X + dx, bolge.Y + dy, bolge.Width - 2 * dx, bolge.Height - dy)
            .Intersect(new Rect(0, 0, renkli.Width, renkli.Height));
        using var kesit = new Mat(renkli, bolge);
        using var gri = GriyeCevir(kesit);
        var sonuc = new Mat();
        Cv2.Resize(gri, sonuc, YuzBoyutu, interpolation: InterpolationFlags.Area);
        Cv2.EqualizeHist(sonuc, sonuc);
        return sonuc;
    }

    /// <summary>
    /// Algılama + tanıma. Model eğitilmemişse Karar null döner.
    /// </summary>
    public List<AlgilananYuz> Isle(Mat renkli)
    {
        var sonuc = new List<AlgilananYuz>();
        foreach (var bolge in YuzleriBul(renkli))
        {
            TanimaKarari? karar = null;
            lock (_modelKilidi)
            {
                if (_tanici is not null)
                {
                    using var yuz = YuzuHazirla(renkli, bolge);
                    _tanici.Predict(yuz, out int id, out double mesafe);
                    karar = TanimaKarari.Olustur(_etiketAdlari.GetValueOrDefault(id), mesafe, Esik);
                }
            }
            sonuc.Add(new AlgilananYuz(bolge, karar));
        }
        return sonuc;
    }

    /// <summary>Sonuçları görüntünün üzerine çizer: tanınan yeşil, bilinmeyen kırmızı, model yoksa mavi.</summary>
    public static void Ciz(Mat renkli, IEnumerable<AlgilananYuz> yuzler)
    {
        int kalinlik = Math.Max(2, renkli.Width / 300);
        double yaziOlcegi = Math.Max(0.6, renkli.Width / 900.0);
        foreach (var y in yuzler)
        {
            Scalar renk = y.Karar is null ? new Scalar(255, 140, 0)
                        : y.Karar.Value.Taninan ? new Scalar(40, 200, 40) : new Scalar(40, 40, 230);
            Cv2.Rectangle(renkli, y.Bolge, renk, kalinlik);

            string yazi = y.Karar is null ? "Yuz" :
                y.Karar.Value.Taninan ? $"{AsciiYap(y.Karar.Value.Etiket)} %{y.Karar.Value.GuvenYuzde}" : "Bilinmiyor";
            var boyut = Cv2.GetTextSize(yazi, HersheyFonts.HersheySimplex, yaziOlcegi, kalinlik, out int taban);
            int x = Math.Clamp(y.Bolge.X, 0, Math.Max(0, renkli.Width - boyut.Width - 10)); // yazı görüntü dışına taşmasın
            var ust = new Point(x, Math.Max(0, y.Bolge.Y - boyut.Height - taban - 6));
            Cv2.Rectangle(renkli, new Rect(ust.X, ust.Y, boyut.Width + 10, boyut.Height + taban + 6), renk, -1);
            Cv2.PutText(renkli, yazi, new Point(ust.X + 5, ust.Y + boyut.Height + 3), HersheyFonts.HersheySimplex,
                yaziOlcegi, Scalar.White, kalinlik, LineTypes.AntiAlias);
        }
    }

    /// <summary>OpenCV'nin yerleşik yazı tipi Türkçe karakter çizemediği için harfleri sadeleştirir.</summary>
    public static string AsciiYap(string s) => new(s.Select(c => c switch
    {
        'ç' => 'c', 'Ç' => 'C', 'ğ' => 'g', 'Ğ' => 'G', 'ı' => 'i', 'İ' => 'I',
        'ö' => 'o', 'Ö' => 'O', 'ş' => 's', 'Ş' => 'S', 'ü' => 'u', 'Ü' => 'U',
        _ => c < 128 ? c : '?'
    }).ToArray());

    // ------------------------------------------------------------------ Eğitim

    /// <summary>
    /// Yüz örneğini (ve veri artırma açıksa çevrilmiş/aydınlatılmış kopyalarını) depoya kaydeder.
    /// Kaydedilen dosya sayısını döner.
    /// </summary>
    public static int OrnekKaydet(OrnekDeposu depo, string etiket, Mat hazirYuz, bool veriArtirma)
    {
        int adet = 0;
        foreach (var varyant in Varyantlar(hazirYuz, veriArtirma))
        {
            using (varyant)
                Cv2.ImWrite(depo.YeniOrnekYolu(etiket), varyant);
            adet++;
        }
        return adet;
    }

    /// <summary>Az örnekle daha kararlı tanıma için basit veri artırma: ayna, parlaklık ve küçük döndürmeler.</summary>
    private static IEnumerable<Mat> Varyantlar(Mat yuz, bool artir)
    {
        yield return yuz.Clone();
        if (!artir)
            yield break;

        var ayna = new Mat();
        Cv2.Flip(yuz, ayna, FlipMode.Y);
        yield return ayna;

        foreach (double fark in new[] { -35.0, 35.0 })
        {
            var parlak = new Mat();
            yuz.ConvertTo(parlak, -1, 1.0, fark);
            yield return parlak;
        }

        foreach (double aci in new[] { -8.0, 8.0 })
        {
            using var donus = Cv2.GetRotationMatrix2D(new Point2f(yuz.Width / 2f, yuz.Height / 2f), aci, 1.05);
            var dondurulmus = new Mat();
            Cv2.WarpAffine(yuz, dondurulmus, donus, yuz.Size(), borderMode: BorderTypes.Replicate);
            yield return dondurulmus;
        }
    }

    /// <summary>Depodaki tüm örneklerle LBPH modelini yeniden eğitir. Kullanılan örnek sayısını döner.</summary>
    public int Egit(OrnekDeposu depo)
    {
        string? hata = depo.EgitimHatasi();
        if (hata is not null)
            throw new InvalidOperationException(hata);

        var goruntuler = new List<Mat>();
        var idler = new List<int>();
        var adlar = new Dictionary<int, string>();
        try
        {
            foreach (var etiket in depo.Etiketler())
            {
                adlar[etiket.Id] = etiket.Etiket;
                foreach (var dosya in etiket.Dosyalar)
                {
                    var g = Cv2.ImRead(dosya, ImreadModes.Grayscale);
                    if (g.Empty()) { g.Dispose(); continue; } // okunamayan dosyayı atla
                    if (g.Size() != YuzBoyutu)
                        Cv2.Resize(g, g, YuzBoyutu);
                    goruntuler.Add(g);
                    idler.Add(etiket.Id);
                }
            }
            if (goruntuler.Count == 0)
                throw new InvalidOperationException("Okunabilir örnek görüntü bulunamadı.");

            var yeni = LBPHFaceRecognizer.Create(radius: 1, neighbors: 8, gridX: 8, gridY: 8);
            yeni.Train(goruntuler, idler);

            lock (_modelKilidi)
            {
                _tanici?.Dispose();
                _tanici = yeni;
                _etiketAdlari = adlar;
            }
            return goruntuler.Count;
        }
        finally
        {
            foreach (var g in goruntuler) g.Dispose();
        }
    }

    public void ModeliSifirla()
    {
        lock (_modelKilidi)
        {
            _tanici?.Dispose();
            _tanici = null;
            _etiketAdlari = new();
        }
    }

    private static Mat GriyeCevir(Mat m)
    {
        var gri = new Mat();
        if (m.Channels() == 1) m.CopyTo(gri);
        else Cv2.CvtColor(m, gri, m.Channels() == 4 ? ColorConversionCodes.BGRA2GRAY : ColorConversionCodes.BGR2GRAY);
        return gri;
    }

    public void Dispose()
    {
        ModeliSifirla();
        _algilayici.Dispose();
    }
}
