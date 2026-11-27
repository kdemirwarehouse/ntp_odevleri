namespace FaceRecognition.Core;

/// <summary>
/// LBPH mesafesini anlamlı bir güven skoruna ve önerilen eşiğe çevirir.
/// <para>
/// Eğitim örnekleri üzerinde çapraz doğrulama ile iki dağılım ölçülür:
/// <b>gerçek</b> (bir örneğin kendi kişisinin diğer örneklerine mesafesi) ve <b>sahte</b> (en yakın başka kişiye mesafesi).
/// Önerilen eşik, yanlış kabul (FAR) + yanlış ret (FRR) oranlarının toplamını en aza indiren mesafedir.
/// Güven skoru, bu eşik etrafında lojistik bir eğriyle hesaplanır: eşikte %50; eğim, gerçek ve sahte dağılımların
/// medyanları arasındaki mesafeden belirlenir.
/// </para>
/// Böylece eski "100 − mesafe" ölçeğinin aksine skor, bu kişilere ve bu kameraya özgü verilerden türetilir.
/// </summary>
public sealed class MesafeKalibrasyonu
{
    public double Esik { get; }
    public double Egim { get; }
    public double YanlisKabulOrani { get; }   // FAR: eşiğin altında kalan sahte mesafe oranı
    public double YanlisRetOrani { get; }     // FRR: eşiğin üstünde kalan gerçek mesafe oranı
    public int GercekSayisi { get; }
    public int SahteSayisi { get; }

    private MesafeKalibrasyonu(double esik, double egim, double far, double frr, int gercek, int sahte)
    {
        Esik = esik; Egim = egim; YanlisKabulOrani = far; YanlisRetOrani = frr; GercekSayisi = gercek; SahteSayisi = sahte;
    }

    /// <summary>
    /// Ölçülen mesafelerden kalibrasyon üretir. Her iki dağılımda da en az 3 ölçüm yoksa null döner.
    /// </summary>
    public static MesafeKalibrasyonu? Olustur(IReadOnlyCollection<double> gercek, IReadOnlyCollection<double> sahte)
    {
        var g = gercek.Where(d => double.IsFinite(d) && d >= 0).OrderBy(d => d).ToArray();
        var s = sahte.Where(d => double.IsFinite(d) && d >= 0).OrderBy(d => d).ToArray();
        if (g.Length < 3 || s.Length < 3)
            return null;

        // Aday eşikler: tüm ölçümlerin arasındaki orta noktalar
        var adaylar = g.Concat(s).Distinct().OrderBy(d => d).ToArray();
        double enIyiEsik = adaylar[0], enIyiHata = double.MaxValue, enIyiFar = 0, enIyiFrr = 1;
        for (int i = 0; i <= adaylar.Length; i++)
        {
            double t = i == 0 ? adaylar[0] - 1 : i == adaylar.Length ? adaylar[^1] + 1 : (adaylar[i - 1] + adaylar[i]) / 2;
            double far = s.Count(d => d <= t) / (double)s.Length;
            double frr = g.Count(d => d > t) / (double)g.Length;
            if (far + frr < enIyiHata - 1e-12)
            {
                enIyiHata = far + frr; enIyiEsik = t; enIyiFar = far; enIyiFrr = frr;
            }
        }

        // Eğim: gerçek dağılımın medyanından sahte dağılımın medyanına giderken güven ~%95'ten ~%5'e insin
        // (iki taraf toplam 2·ln(19) ≈ 5,89 birim). Dağılımlar çok yakınsa eğri yumuşak kalsın diye alt sınır 2.
        double gercekMedyan = g[g.Length / 2], sahteMedyan = s[s.Length / 2];
        double egim = Math.Max(2.0, (sahteMedyan - gercekMedyan) / 5.89);
        return new MesafeKalibrasyonu(enIyiEsik, egim, enIyiFar, enIyiFrr, g.Length, s.Length);
    }

    /// <summary>Mesafeyi 0-100 güven yüzdesine çevirir (eşikte 50).</summary>
    public int GuvenYuzde(double mesafe)
    {
        if (!double.IsFinite(mesafe)) return 0;
        double p = 1.0 / (1.0 + Math.Exp((mesafe - Esik) / Egim));
        return (int)Math.Round(p * 100);
    }

    public string Ozet() =>
        $"önerilen eşik {Esik:0.0} · yanlış kabul %{YanlisKabulOrani * 100:0} · yanlış ret %{YanlisRetOrani * 100:0} " +
        $"({GercekSayisi} gerçek / {SahteSayisi} sahte ölçüm)";
}
