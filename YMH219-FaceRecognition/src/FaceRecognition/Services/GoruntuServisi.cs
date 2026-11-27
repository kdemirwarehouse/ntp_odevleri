using FaceRecognition.Core;
using OpenCvSharp;
using OpenCvSharp.Face;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace FaceRecognition.Services;

/// <summary>Bir karede bulunan yüz ve (model eğitilmişse) tanıma kararı.</summary>
public sealed record AlgilananYuz(Rect Bolge, TanimaKarari? Karar, YuzBolgesi? Kaynak = null);

/// <summary>Algılayıcının bulduğu yüz bölgesi; YuNet ayrıca göz konumlarını ve güven skorunu verir.</summary>
public sealed record YuzBolgesi(Rect Bolge, Point2f? GoruntudeSolGoz = null, Point2f? GoruntudeSagGoz = null, float Skor = 1f);

/// <summary>Kullanılan yüz algılama yöntemi.</summary>
public enum AlgilayiciTuru
{
    /// <summary>OpenCV DNN tabanlı YuNet: eğik/yarı profil yüzlerde ve karmaşık arka planda daha başarılı, göz noktaları verir.</summary>
    YuNet,
    /// <summary>Klasik Haar Cascade: hızlı, önden bakan yüzlerde iyi.</summary>
    Haar
}

/// <summary>
/// OpenCV ile yüz algılama (Haar Cascade) ve yüz tanıma (LBPH) işlemlerini yürütür.
/// Arayüzden bağımsızdır; form yalnızca Mat verir ve sonuçları çizer.
/// </summary>
public sealed class GoruntuServisi : IDisposable
{
    /// <summary>Tüm yüzler bu boyuta getirilerek karşılaştırılır.</summary>
    public static readonly Size YuzBoyutu = new(100, 100);

    private readonly CascadeClassifier _algilayici;
    private readonly string? _yunetYolu;
    private readonly object _yunetKilidi = new();
    private FaceDetectorYN? _yunet;
    private Size _yunetBoyutu;
    private const int YuNetEnBuyukKenar = 640;   // büyük fotoğraflar bu boyuta küçültülerek işlenir
    private readonly object _modelKilidi = new();
    private LBPHFaceRecognizer? _tanici;
    private Dictionary<int, string> _etiketAdlari = new();

    /// <summary>Model dosyası bulunamazsa veya bozuksa FileNotFoundException / InvalidDataException fırlatır.</summary>
    public GoruntuServisi(string cascadeYolu, string? yunetYolu = null)
    {
        _yunetYolu = yunetYolu is not null && File.Exists(yunetYolu) ? yunetYolu : null;
        Algilayici = _yunetYolu is null ? AlgilayiciTuru.Haar : AlgilayiciTuru.YuNet;
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

    /// <summary>YuNet model dosyası bulunduysa true.</summary>
    public bool YuNetKullanilabilir => _yunetYolu is not null;

    /// <summary>Etkin algılayıcı. YuNet modeli yoksa her zaman Haar kullanılır.</summary>
    public AlgilayiciTuru Algilayici
    {
        get => _algilayiciTuru;
        set => _algilayiciTuru = value == AlgilayiciTuru.YuNet && _yunetYolu is null ? AlgilayiciTuru.Haar : value;
    }
    private AlgilayiciTuru _algilayiciTuru;

    /// <summary>YuNet'in bir bölgeyi yüz sayması için gereken en düşük skor (0-1).</summary>
    public float YuNetSkorEsigi { get; set; } = 0.8f;
    public bool ModelHazir { get { lock (_modelKilidi) return _tanici is not null; } }

    /// <summary>Son eğitimde çapraz doğrulamayla hesaplanan kalibrasyon (yetersiz veride null).</summary>
    public MesafeKalibrasyonu? Kalibrasyon { get; private set; }
    public IReadOnlyCollection<string> ModeldekiEtiketler { get { lock (_modelKilidi) return _etiketAdlari.Values.ToList(); } }

    // ------------------------------------------------------------------ Algılama

    /// <summary>Renkli görüntüdeki tüm yüzleri bulur (birden fazla yüz desteklenir).</summary>
    public Rect[] YuzleriBul(Mat renkli) => YuzBolgeleriniBul(renkli).Select(y => y.Bolge).ToArray();

    /// <summary>Seçili algılayıcıyla yüz bölgelerini (YuNet'te göz noktalarıyla) bulur.</summary>
    public List<YuzBolgesi> YuzBolgeleriniBul(Mat renkli)
    {
        if (Algilayici == AlgilayiciTuru.YuNet)
        {
            try { return YuNetIleBul(renkli); }
            catch (OpenCVException) { _algilayiciTuru = AlgilayiciTuru.Haar; }   // DNN çalışmazsa Haar'a düş
        }
        return HaarIleBul(renkli).Select(r => new YuzBolgesi(r)).ToList();
    }

    /// <summary>
    /// YuNet (OpenCV FaceDetectorYN): her satır x, y, w, h, sağ göz, sol göz, burun, ağız sağ/sol (x, y) ve skor içerir.
    /// Görüntü en fazla 640 px'e küçültülür, sonuçlar orijinal ölçeğe geri çevrilir.
    /// </summary>
    private List<YuzBolgesi> YuNetIleBul(Mat renkli)
    {
        double olcek = Math.Min(1.0, (double)YuNetEnBuyukKenar / Math.Max(renkli.Width, renkli.Height));
        var boyut = new Size((int)Math.Round(renkli.Width * olcek), (int)Math.Round(renkli.Height * olcek));
        using var bgr = new Mat();
        if (renkli.Channels() == 1) Cv2.CvtColor(renkli, bgr, ColorConversionCodes.GRAY2BGR);
        else if (renkli.Channels() == 4) Cv2.CvtColor(renkli, bgr, ColorConversionCodes.BGRA2BGR);
        else renkli.CopyTo(bgr);
        using var girdi = new Mat();
        Cv2.Resize(bgr, girdi, boyut, interpolation: InterpolationFlags.Area);

        var sonuc = new List<YuzBolgesi>();
        using var yuzler = new Mat();
        lock (_yunetKilidi)
        {
            if (_yunet is null || _yunetBoyutu != boyut)
            {
                _yunet?.Dispose();
                _yunet = FaceDetectorYN.Create(_yunetYolu!, "", boyut, YuNetSkorEsigi, 0.3f, 5000);
                _yunetBoyutu = boyut;
            }
            _yunet.Detect(girdi, yuzler);
        }

        var tam = new Rect(0, 0, renkli.Width, renkli.Height);
        for (int i = 0; i < yuzler.Rows; i++)
        {
            float V(int j) => yuzler.At<float>(i, j) / (float)olcek;
            float skor = yuzler.At<float>(i, 14);
            if (skor < YuNetSkorEsigi) continue;
            var bolge = new Rect((int)V(0), (int)V(1), (int)V(2), (int)V(3)).Intersect(tam);
            if (bolge.Width < 20 || bolge.Height < 20) continue;
            // YuNet "sağ göz" kişinin sağ gözüdür, yani görüntünün solunda kalır
            sonuc.Add(new YuzBolgesi(bolge, new Point2f(V(4), V(5)), new Point2f(V(6), V(7)), skor));
        }
        return sonuc;
    }

    private Rect[] HaarIleBul(Mat renkli)
    {
        using var gri = GriyeCevir(renkli);
        Cv2.EqualizeHist(gri, gri);
        int minKenar = Math.Max(40, Math.Min(gri.Width, gri.Height) / 10);
        return _algilayici.DetectMultiScale(gri, scaleFactor: 1.1, minNeighbors: 9,
            flags: HaarDetectionTypes.ScaleImage, minSize: new Size(minKenar, minKenar));
    }

    /// <summary>
    /// Tanıma girdisini hazırlar. Göz noktaları varsa (YuNet) yüz <b>hizalanır</b>: gözler yatay olacak şekilde
    /// döndürülür, gözler arası mesafe sabitlenir ve gözler 100x100 görüntüde hep aynı yere gelir.
    /// Ardından CLAHE (yerel kontrast eşitleme) uygulanır; yandan gelen ışıkta global eşitlemeden daha kararlıdır.
    /// </summary>
    public static Mat YuzuHazirla(Mat renkli, YuzBolgesi yuz)
    {
        if (yuz.GoruntudeSolGoz is Point2f sol && yuz.GoruntudeSagGoz is Point2f sag &&
            YuzHizalama.Hesapla(sol.X, sol.Y, sag.X, sag.Y, YuzBoyutu.Width) is YuzHizalama.Parametreler p)
        {
            using var gri = GriyeCevir(renkli);
            using var m = Cv2.GetRotationMatrix2D(new Point2f((float)p.MerkezX, (float)p.MerkezY), p.Aci, p.Olcek);
            m.Set(0, 2, m.At<double>(0, 2) + p.HedefX - p.MerkezX);
            m.Set(1, 2, m.At<double>(1, 2) + p.HedefY - p.MerkezY);
            var hizali = new Mat();
            Cv2.WarpAffine(gri, hizali, m, YuzBoyutu, InterpolationFlags.Linear, BorderTypes.Replicate);
            KontrastEsitle(hizali);
            return hizali;
        }
        return YuzuHazirla(renkli, yuz.Bolge);
    }

    /// <summary>CLAHE: görüntüyü 8x8 karoya bölüp her karoda ayrı (sınırlı) histogram eşitleme yapar.</summary>
    private static void KontrastEsitle(Mat gri)
    {
        using var clahe = Cv2.CreateCLAHE(2.0, new Size(8, 8));
        clahe.Apply(gri, gri);
    }

    /// <summary>Göz noktası yoksa (Haar): yüz kutusu kırpılıp 100x100 gri ve CLAHE'li hâle getirilir.</summary>
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
        KontrastEsitle(sonuc);
        return sonuc;
    }

    /// <summary>
    /// Algılama + tanıma. Model eğitilmemişse Karar null döner.
    /// </summary>
    public List<AlgilananYuz> Isle(Mat renkli)
    {
        var sonuc = new List<AlgilananYuz>();
        foreach (var bolgeBilgisi in YuzBolgeleriniBul(renkli))
        {
            var bolge = bolgeBilgisi.Bolge;
            TanimaKarari? karar = null;
            lock (_modelKilidi)
            {
                if (_tanici is not null)
                {
                    using var yuz = YuzuHazirla(renkli, bolgeBilgisi);
                    _tanici.Predict(yuz, out int id, out double mesafe);
                    karar = TanimaKarari.Olustur(_etiketAdlari.GetValueOrDefault(id), mesafe, Esik, Kalibrasyon);
                }
            }
            sonuc.Add(new AlgilananYuz(bolge, karar, bolgeBilgisi));
        }
        return sonuc;
    }

    /// <summary>Yüz çerçevelerini çizer: tanınan yeşil, bilinmeyen kırmızı, model yoksa mavi.</summary>
    public static void Ciz(Mat renkli, IEnumerable<AlgilananYuz> yuzler)
    {
        int kalinlik = Math.Max(2, renkli.Width / 300);
        foreach (var y in yuzler)
            Cv2.Rectangle(renkli, y.Bolge, Renk(y), kalinlik);
    }

    private static Scalar Renk(AlgilananYuz y) =>
        y.Karar is null ? new Scalar(255, 140, 0)
        : y.Karar.Value.Taninan ? new Scalar(40, 200, 40) : new Scalar(40, 40, 230);

    /// <summary>
    /// Etiketleri GDI+ ile Bitmap üzerine yazar. OpenCV'nin yerleşik Hershey yazı tipi yalnızca ASCII çizebildiği
    /// için ("Ayşe" → "Ay?e") yazılar artık Segoe UI ile, Türkçe karakterler korunarak çizilir.
    /// </summary>
    public static void EtiketleriCiz(System.Drawing.Bitmap bitmap, IEnumerable<AlgilananYuz> yuzler)
    {
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        float punto = Math.Max(11f, bitmap.Width / 70f);
        using var yazi = new System.Drawing.Font("Segoe UI", punto, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
        foreach (var y in yuzler)
        {
            string metin = y.Karar is null ? "Yüz"
                : y.Karar.Value.Taninan ? $"{y.Karar.Value.Etiket}  %{y.Karar.Value.GuvenYuzde}"
                : TanimaKarari.Bilinmiyor;
            var olcu = g.MeasureString(metin, yazi);
            float x = Math.Clamp(y.Bolge.X, 0, Math.Max(0, bitmap.Width - olcu.Width - 4));
            float ust = Math.Max(0, y.Bolge.Y - olcu.Height - 2);
            var r = Renk(y);   // OpenCV BGR → GDI+ RGB
            using var zemin = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(220, (int)r.Val2, (int)r.Val1, (int)r.Val0));
            g.FillRectangle(zemin, x, ust, olcu.Width + 4, olcu.Height);
            g.DrawString(metin, yazi, System.Drawing.Brushes.White, x + 2, ust);
        }
    }

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

            var yeni = YeniTanici();
            yeni.Train(goruntuler, idler);
            var kalibrasyon = Kalibre(goruntuler, idler);

            lock (_modelKilidi)
            {
                _tanici?.Dispose();
                _tanici = yeni;
                _etiketAdlari = adlar;
                Kalibrasyon = kalibrasyon;
            }
            return goruntuler.Count;
        }
        finally
        {
            foreach (var g in goruntuler) g.Dispose();
        }
    }

    private static LBPHFaceRecognizer YeniTanici() => LBPHFaceRecognizer.Create(radius: 1, neighbors: 8, gridX: 8, gridY: 8);

    /// <summary>
    /// 5 katlı çapraz doğrulama ile gerçek ve sahte mesafe dağılımlarını ölçer.
    /// Her kişinin örnekleri dosya sırasına göre ardışık 5 parçaya bölünür (aynı örneğin veri artırma kopyaları
    /// ardışık kaydedildiği için büyük ölçüde aynı parçada kalır, sızıntı azalır):
    /// <list type="bullet">
    /// <item>Gerçek: kişinin diğer parçalarıyla eğitilen modelin, dışarıda bırakılan parçaya verdiği mesafe.</item>
    /// <item>Sahte: yalnızca diğer kişilerle eğitilen modelin bu kişinin örneklerine verdiği en küçük mesafe.</item>
    /// </list>
    /// </summary>
    private static MesafeKalibrasyonu? Kalibre(List<Mat> goruntuler, List<int> idler)
    {
        const int Kat = 5;
        var etiketler = idler.Distinct().ToList();
        if (etiketler.Count < 2)
            return null;

        var gercek = new List<double>();
        var sahte = new List<double>();
        foreach (int etiket in etiketler)
        {
            var kendi = Enumerable.Range(0, idler.Count).Where(i => idler[i] == etiket).ToList();
            var diger = Enumerable.Range(0, idler.Count).Where(i => idler[i] != etiket).ToList();
            if (kendi.Count < Kat || diger.Count == 0)
                continue;

            using (var sahteModel = YeniTanici())
            {
                sahteModel.Train(diger.Select(i => goruntuler[i]), diger.Select(i => idler[i]));
                foreach (int i in kendi.Where((_, k) => k % 2 == 0))   // örneklerin yarısı yeterli
                {
                    sahteModel.Predict(goruntuler[i], out _, out double d);
                    sahte.Add(d);
                }
            }

            for (int kat = 0; kat < Kat; kat++)
            {
                int bas = kat * kendi.Count / Kat, son = (kat + 1) * kendi.Count / Kat;
                var test = kendi.Skip(bas).Take(son - bas).ToList();
                var egitim = kendi.Take(bas).Concat(kendi.Skip(son)).ToList();
                if (test.Count == 0 || egitim.Count == 0) continue;
                using var gercekModel = YeniTanici();
                gercekModel.Train(egitim.Select(i => goruntuler[i]), egitim.Select(i => idler[i]));
                foreach (int i in test)
                {
                    gercekModel.Predict(goruntuler[i], out _, out double d);
                    gercek.Add(d);
                }
            }
        }
        return MesafeKalibrasyonu.Olustur(gercek, sahte);
    }

    public void ModeliSifirla()
    {
        lock (_modelKilidi)
        {
            _tanici?.Dispose();
            _tanici = null;
            _etiketAdlari = new();
            Kalibrasyon = null;
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
        lock (_yunetKilidi) _yunet?.Dispose();
    }
}
