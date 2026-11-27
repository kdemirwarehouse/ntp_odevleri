namespace KeyEventLogger.Core;

/// <summary>
/// Kayıt durumunu (açık/kapalı) ve bellekteki olay listesini yönetir.
/// Arayüzden bağımsızdır; Windows Forms yalnızca bu sınıfa olay ekler.
/// </summary>
public class KayitOturumu
{
    public const int VarsayilanSinir = 5000;

    private readonly List<KeyLogEntry> _kayitlar = new();
    private readonly Func<DateTime> _saat;

    /// <summary>Kayıt başladığında / durduğunda tetiklenir.</summary>
    public event EventHandler? DurumDegisti;

    /// <summary>Listeye yeni bir olay eklendiğinde tetiklenir.</summary>
    public event EventHandler<KeyLogEntry>? KayitEklendi;

    public KayitOturumu(int sinir = VarsayilanSinir, Func<DateTime>? saat = null)
    {
        if (sinir <= 0)
            throw new ArgumentOutOfRangeException(nameof(sinir), "Kayıt sınırı pozitif olmalı.");
        Sinir = sinir;
        _saat = saat ?? (() => DateTime.Now);
    }

    public bool KayitAcik { get; private set; }
    public int Sinir { get; }
    public DateTime? BaslamaZamani { get; private set; }
    public IReadOnlyList<KeyLogEntry> Kayitlar => _kayitlar;
    public bool SinirDoldu => _kayitlar.Count >= Sinir;

    /// <summary>Son başarılı kaydetmeden (veya temizlemeden) bu yana eklenen olay sayısı.</summary>
    public int KaydedilmemisOlaySayisi { get; private set; }

    /// <summary>Kapatırken kullanıcıya sorulması gereken, dosyaya yazılmamış olay var mı?</summary>
    public bool KaydedilmemisDegisiklikVar => KaydedilmemisOlaySayisi > 0;

    /// <summary>Kayıtlar dosyaya başarıyla yazıldıktan sonra çağrılır.</summary>
    public void KaydedildiOlarakIsaretle() => KaydedilmemisOlaySayisi = 0;

    public void Baslat()
    {
        if (KayitAcik)
            return;
        if (SinirDoldu)
            throw new InvalidOperationException(
                $"Kayıt sınırına ({Sinir} olay) ulaşıldı. Devam etmek için kayıtları kaydedip temizleyin.");

        KayitAcik = true;
        BaslamaZamani = _saat();
        DurumDegisti?.Invoke(this, EventArgs.Empty);
    }

    public void Durdur()
    {
        if (!KayitAcik)
            return;
        KayitAcik = false;
        DurumDegisti?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ekrandaki/bellekteki kayıtları siler. Kayıt durumu değişmez.</summary>
    public void Temizle()
    {
        _kayitlar.Clear();
        KaydedilmemisOlaySayisi = 0;
    }

    /// <summary>
    /// Kayıt açıksa olayı listeye ekler ve true döner. Kapalıysa olay yok sayılır.
    /// Sınır dolduğunda kayıt kendiliğinden durur.
    /// </summary>
    public bool Ekle(OlayTuru tur, string tus, string ayrinti = "", bool tekrar = false)
    {
        if (!KayitAcik)
            return false;

        var kayit = new KeyLogEntry(_saat(), tur, string.IsNullOrEmpty(tus) ? "(bilinmiyor)" : tus, ayrinti ?? "", tekrar);
        _kayitlar.Add(kayit);
        KaydedilmemisOlaySayisi++;
        KayitEklendi?.Invoke(this, kayit);

        if (SinirDoldu)
            Durdur();
        return true;
    }

    // ------------------------------------------------------------------ İstatistik

    /// <summary>Fiziksel tuş basımı sayısı (tekrar olmayan KeyDown olayları).</summary>
    public int ToplamTusBasimi => _kayitlar.Count(k => k.Tur == OlayTuru.KeyDown && !k.Tekrar);

    /// <summary>Basılı tutma nedeniyle oluşan tekrar olayı sayısı (KeyDown + KeyPress).</summary>
    public int TekrarSayisi => _kayitlar.Count(k => k.Tekrar);

    /// <summary>En sık basılan tuşlar (fiziksel KeyDown basımlarına göre), çoktan aza.</summary>
    public List<(string Tus, int Adet)> EnSikTuslar(int adet = 5) =>
        _kayitlar.Where(k => k.Tur == OlayTuru.KeyDown && !k.Tekrar)
                 .GroupBy(k => k.Tus)
                 .Select(g => (Tus: g.Key, Adet: g.Count()))
                 .OrderByDescending(x => x.Adet)
                 .ThenBy(x => x.Tus, StringComparer.Ordinal)
                 .Take(adet)
                 .ToList();

    /// <summary>Verilen tarih-saat aralığındaki kayıtlar (sınırlar dahil).</summary>
    public List<KeyLogEntry> Filtrele(DateTime baslangic, DateTime bitis)
    {
        if (bitis < baslangic)
            throw new ArgumentException("Bitiş zamanı başlangıç zamanından önce olamaz.");
        return _kayitlar.Where(k => k.Zaman >= baslangic && k.Zaman <= bitis).ToList();
    }
}
