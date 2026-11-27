namespace FaceRecognition.Core;

/// <summary>
/// Göz noktalarına göre yüz hizalama geometrisi (OpenCV'den bağımsız, test edilebilir).
/// <para>
/// LBPH, yüzün görüntüdeki konumuna ve eğikliğine duyarlıdır: aynı kişinin başı 10° eğikken çekilmiş fotoğrafı
/// bile farklı doku histogramları üretir. Bu yüzden her yüz, iki göz yatay olacak şekilde döndürülür, gözler arası
/// mesafe sabit bir orana ölçeklenir ve gözler çıktı görüntüsünde hep aynı noktaya gelecek biçimde kırpılır.
/// </para>
/// </summary>
public static class YuzHizalama
{
    /// <summary>Çıktı görüntüsünde gözler arası mesafenin genişliğe oranı.</summary>
    public const double GozMesafesiOrani = 0.40;

    /// <summary>Çıktı görüntüsünde gözlerin yukarıdan yüksekliği (genişliğe oran).</summary>
    public const double GozYuksekligiOrani = 0.36;

    /// <param name="Aci">Görüntünün göz ortası etrafında döndürüleceği açı (derece, saat yönünün tersi pozitif — OpenCV kuralı).</param>
    /// <param name="Olcek">Kaynak pikselin çıktıda kaç piksele karşılık geldiği.</param>
    /// <param name="MerkezX">Göz ortası (kaynak görüntüde).</param>
    /// <param name="HedefX">Göz ortasının çıktı görüntüsündeki konumu.</param>
    public readonly record struct Parametreler(double Aci, double Olcek, double MerkezX, double MerkezY, double HedefX, double HedefY);

    /// <summary>
    /// Görüntünün solundaki ve sağındaki göz konumlarından hizalama parametrelerini hesaplar.
    /// Gözler çakışıksa veya mesafe çok küçükse null döner (bu durumda hizalama yapılmaz).
    /// </summary>
    public static Parametreler? Hesapla(double solX, double solY, double sagX, double sagY, int ciktiKenari)
    {
        if (ciktiKenari <= 0)
            throw new ArgumentOutOfRangeException(nameof(ciktiKenari));
        double dx = sagX - solX, dy = sagY - solY;
        double mesafe = Math.Sqrt(dx * dx + dy * dy);
        if (mesafe < 4 || dx <= 0)
            return null;   // gözler yanlış sırada / çok yakın: güvenilir değil

        double aci = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        double olcek = ciktiKenari * GozMesafesiOrani / mesafe;
        return new Parametreler(aci, olcek, (solX + sagX) / 2, (solY + sagY) / 2,
                                ciktiKenari / 2.0, ciktiKenari * GozYuksekligiOrani);
    }

    /// <summary>
    /// Parametrelerle bir kaynak noktayı çıktı koordinatına dönüştürür (OpenCV GetRotationMatrix2D + öteleme ile aynı).
    /// </summary>
    public static (double X, double Y) Donustur(Parametreler p, double x, double y)
    {
        double r = p.Aci * Math.PI / 180.0;
        double a = p.Olcek * Math.Cos(r), b = p.Olcek * Math.Sin(r);
        double rx = a * (x - p.MerkezX) + b * (y - p.MerkezY);
        double ry = -b * (x - p.MerkezX) + a * (y - p.MerkezY);
        return (rx + p.HedefX, ry + p.HedefY);
    }
}
