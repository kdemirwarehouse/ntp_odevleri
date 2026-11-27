namespace KeyEventLogger.Core;

/// <summary>
/// Tuşa basılı tutulduğunda Windows'un ürettiği otomatik tekrar (auto-repeat) KeyDown olaylarını ayırt eder.
/// <para>
/// Bir tuş için KeyUp gelmeden ikinci bir KeyDown gelirse bu fiziksel bir basım değil, tekrardır.
/// Basılı tuşlar bir kümede tutulur; KeyUp gelince kümeden çıkarılır.
/// </para>
/// </summary>
public class TekrarAlgilayici
{
    private readonly HashSet<string> _basiliTuslar = new(StringComparer.Ordinal);

    /// <summary>Son KeyDown bir tekrar mıydı? (Ardından gelen KeyPress'i de tekrar saymak için.)</summary>
    public bool SonBasimTekrar { get; private set; }

    /// <summary>Şu anda basılı tutulan tuş sayısı.</summary>
    public int BasiliTusSayisi => _basiliTuslar.Count;

    /// <summary>KeyDown geldiğinde çağrılır. Tuş zaten basılıysa true (tekrar) döner.</summary>
    public bool TusBasildi(string tus)
    {
        SonBasimTekrar = !_basiliTuslar.Add(tus);
        return SonBasimTekrar;
    }

    /// <summary>KeyUp geldiğinde çağrılır.</summary>
    public void TusBirakildi(string tus)
    {
        _basiliTuslar.Remove(tus);
        SonBasimTekrar = false;
    }

    /// <summary>
    /// Pencere odağı kaybedilince çağrılır: odak dışındayken bırakılan tuşların KeyUp'ı gelmeyeceği için
    /// küme temizlenir, aksi hâlde sonraki ilk basım yanlışlıkla tekrar sayılırdı.
    /// </summary>
    public void Sifirla()
    {
        _basiliTuslar.Clear();
        SonBasimTekrar = false;
    }
}
