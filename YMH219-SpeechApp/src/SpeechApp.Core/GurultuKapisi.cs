namespace SpeechApp.Core;

/// <summary>
/// Basit gürültü kapısı (noise gate): ses seviyesi eşiğin altındaki mikrofon blokları sessizliğe çevrilir.
/// Böylece fan, klavye veya uzak konuşmalar gibi düşük seviyeli arka plan sesleri tanıyıcıya "kelime" olarak gitmez.
/// <para>
/// Konuşma sırasında kelime aralarındaki kısa düşüşler kesilmesin diye, eşik aşıldıktan sonra kapı
/// <see cref="KuyrukBlokSayisi"/> blok boyunca açık kalır (hangover).
/// </para>
/// </summary>
public sealed class GurultuKapisi
{
    private int _kalanKuyruk;

    /// <summary>0-100 ses seviyesi eşiği. 0 = kapı kapalı (her şey geçer).</summary>
    public int Esik { get; set; }

    /// <summary>Eşik aşıldıktan sonra kapının açık kaldığı blok sayısı (100 ms bloklarda 4 = 0,4 sn).</summary>
    public int KuyrukBlokSayisi { get; set; } = 4;

    /// <summary>
    /// Bloğu değerlendirir. Blok geçmeliyse true döner; geçmemeliyse tamponu sıfırlayıp (sessizlik) false döner.
    /// </summary>
    public bool Uygula(byte[] tampon, int uzunluk, int seviye)
    {
        if (Esik <= 0)
            return true;
        if (seviye >= Esik)
        {
            _kalanKuyruk = KuyrukBlokSayisi;
            return true;
        }
        if (_kalanKuyruk > 0)
        {
            _kalanKuyruk--;
            return true;
        }
        Array.Clear(tampon, 0, uzunluk);
        return false;
    }

    /// <summary>
    /// Sessiz ortamda ölçülen seviyelerden önerilen eşiği hesaplar: ortalamanın 1,5 katı + 3, en fazla 60.
    /// Tepe değerler (ör. tek bir tık) ortalamayı fazla etkilemesin diye en yüksek %10 atılır.
    /// </summary>
    public static int OnerilenEsik(IReadOnlyCollection<int> sessizOrtamSeviyeleri)
    {
        if (sessizOrtamSeviyeleri.Count == 0)
            return 0;
        var sirali = sessizOrtamSeviyeleri.OrderBy(x => x).ToList();
        int tut = Math.Max(1, (int)Math.Ceiling(sirali.Count * 0.9));
        double ortalama = sirali.Take(tut).Average();
        return (int)Math.Min(60, Math.Round(ortalama * 1.5 + 3));
    }
}
