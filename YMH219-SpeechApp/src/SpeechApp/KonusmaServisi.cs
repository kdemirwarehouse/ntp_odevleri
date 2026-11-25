using System.Globalization;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using SpeechApp.Core;

namespace SpeechApp;

/// <summary>
/// System.Speech (Windows SAPI) üzerindeki sentezleyici ve tanıyıcıyı tek bir sınıfta toplar.
/// Form, ses API'sinin ayrıntılarını bilmez; yalnızca bu sınıfın metotlarını ve olaylarını kullanır.
/// IDisposable: sentezleyici ve tanıma motoru yönetilmeyen kaynak tuttuğu için kapatılmalıdır.
/// </summary>
public sealed class KonusmaServisi : IDisposable
{
    private readonly SpeechSynthesizer _sentezleyici = new();
    private SpeechRecognitionEngine? _taniyici;
    private SesDurumu _durum = SesDurumu.Hazir;

    public event EventHandler<SesDurumu>? DurumDegisti;
    public event EventHandler<(string Metin, float Guven)>? CumleTanindi;
    public event EventHandler<string>? TahminGuncellendi;   // konuşma sürerken ara sonuç
    public event EventHandler<int>? SesSeviyesi;             // 0-100 mikrofon seviyesi
    public event EventHandler<CharacterPosition>? OkunanKelime;
    public event EventHandler<string>? Hata;

    public readonly record struct CharacterPosition(int Baslangic, int Uzunluk);

    public KonusmaServisi()
    {
        _sentezleyici.SetOutputToDefaultAudioDevice();
        _sentezleyici.SpeakStarted += (_, _) => DurumuAyarla(SesDurumu.Okunuyor);
        _sentezleyici.SpeakCompleted += (_, _) => { if (_durum is SesDurumu.Okunuyor or SesDurumu.Duraklatildi) DurumuAyarla(SesDurumu.Hazir); };
        _sentezleyici.SpeakProgress += (_, e) => OkunanKelime?.Invoke(this, new CharacterPosition(e.CharacterPosition, e.CharacterCount));
    }

    public SesDurumu Durum => _durum;

    // ------------------------------------------------------------------ Text-to-Speech

    public IReadOnlyList<string> Sesler() =>
        _sentezleyici.GetInstalledVoices().Where(v => v.Enabled)
                     .Select(v => $"{v.VoiceInfo.Name} ({v.VoiceInfo.Culture.Name})").ToList();

    public void SesSec(int indeks)
    {
        var sesler = _sentezleyici.GetInstalledVoices().Where(v => v.Enabled).ToList();
        if (indeks >= 0 && indeks < sesler.Count)
            _sentezleyici.SelectVoice(sesler[indeks].VoiceInfo.Name);
    }

    /// <summary>Okuma hızı: -10 (yavaş) ile 10 (hızlı) arası.</summary>
    public int Hiz { get => _sentezleyici.Rate; set => _sentezleyici.Rate = Math.Clamp(value, -10, 10); }

    /// <summary>Ses düzeyi: 0-100.</summary>
    public int Duzey { get => _sentezleyici.Volume; set => _sentezleyici.Volume = Math.Clamp(value, 0, 100); }

    public void Oku(string metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            throw new InvalidOperationException("Okunacak metin yok. Lütfen metin kutusuna bir şeyler yazın.");
        if (_durum is SesDurumu.Dinleniyor or SesDurumu.DosyaTaniniyor)
            throw new InvalidOperationException("Konuşma tanıma çalışırken okuma yapılamaz. Önce dinlemeyi durdurun.");

        _sentezleyici.SpeakAsyncCancelAll();
        if (_sentezleyici.State == SynthesizerState.Paused)
            _sentezleyici.Resume();
        _sentezleyici.SpeakAsync(metin);
    }

    /// <summary>Okuma sürüyorsa duraklatır, duraklatılmışsa devam ettirir.</summary>
    public void DuraklatDevam()
    {
        if (_sentezleyici.State == SynthesizerState.Speaking)
        {
            _sentezleyici.Pause();
            DurumuAyarla(SesDurumu.Duraklatildi);
        }
        else if (_sentezleyici.State == SynthesizerState.Paused)
        {
            _sentezleyici.Resume();
            DurumuAyarla(SesDurumu.Okunuyor);
        }
    }

    public void OkumayiDurdur()
    {
        if (_sentezleyici.State == SynthesizerState.Paused)
            _sentezleyici.Resume(); // duraklatılmış konuşma iptal edilemez, önce devam ettirilir
        _sentezleyici.SpeakAsyncCancelAll();
        DurumuAyarla(SesDurumu.Hazir);
    }

    /// <summary>Metni okumak yerine .wav dosyasına kaydeder.</summary>
    public void WavOlarakKaydet(string metin, string dosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(metin))
            throw new InvalidOperationException("Kaydedilecek metin yok.");
        using var gecici = new SpeechSynthesizer { Rate = Hiz, Volume = Duzey };
        gecici.SelectVoice(_sentezleyici.Voice.Name);
        gecici.SetOutputToWaveFile(dosyaYolu);
        gecici.Speak(metin);
    }

    // ------------------------------------------------------------------ Speech-to-Text

    public static IReadOnlyList<string> KuruluTaniyicilar() =>
        SpeechRecognitionEngine.InstalledRecognizers().Select(r => r.Culture.Name).ToList();

    public string? TaniyiciDili { get; private set; }

    /// <summary>Mikrofondan sürekli dikte (DictationGrammar) başlatır.</summary>
    public void DinlemeyiBaslat() => TanimayiBaslat(dosya: null);

    /// <summary>Bir .wav dosyasındaki konuşmayı metne çevirir.</summary>
    public void DosyadanTani(string wavYolu) => TanimayiBaslat(wavYolu);

    private void TanimayiBaslat(string? dosya)
    {
        if (_durum is SesDurumu.Dinleniyor or SesDurumu.DosyaTaniniyor)
            return;
        if (_durum is SesDurumu.Okunuyor or SesDurumu.Duraklatildi)
            OkumayiDurdur();

        string? dil = DilSecici.EnUygun(KuruluTaniyicilar(), CultureInfo.CurrentUICulture.Name);
        if (dil is null)
            throw new InvalidOperationException(
                "Bu bilgisayarda kurulu bir konuşma tanıma motoru bulunamadı.\n" +
                "Ayarlar > Saat ve dil > Konuşma bölümünden bir konuşma dili ekleyin.");

        var taniyici = new SpeechRecognitionEngine(new CultureInfo(dil));
        try
        {
            taniyici.LoadGrammar(new DictationGrammar());
            if (dosya is null)
                taniyici.SetInputToDefaultAudioDevice(); // mikrofon yoksa InvalidOperationException
            else
                taniyici.SetInputToWaveFile(dosya);      // dosya yoksa/bozuksa hata
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException or ArgumentException or FormatException)
        {
            taniyici.Dispose();
            throw new InvalidOperationException(dosya is null
                ? "Mikrofona erişilemedi. Bir mikrofonun bağlı olduğundan ve Windows Gizlilik ayarlarında " +
                  "masaüstü uygulamalarının mikrofona erişimine izin verildiğinden emin olun.\n\nAyrıntı: " + ex.Message
                : "Ses dosyası açılamadı (yalnızca .wav desteklenir).\n\nAyrıntı: " + ex.Message, ex);
        }

        taniyici.SpeechRecognized += (_, e) => CumleTanindi?.Invoke(this, (e.Result.Text, e.Result.Confidence));
        taniyici.SpeechHypothesized += (_, e) => TahminGuncellendi?.Invoke(this, e.Result.Text);
        taniyici.AudioLevelUpdated += (_, e) => SesSeviyesi?.Invoke(this, e.AudioLevel);
        taniyici.RecognizeCompleted += (_, e) =>
        {
            if (e.Error is not null)
                Hata?.Invoke(this, "Konuşma tanıma hatası: " + e.Error.Message);
            if (dosya is not null || e.Error is not null)
                Task.Run(DinlemeyiDurdur); // dosya bitince tanıma tamamlanır; motor kendi olayı içinde kapatılmaz
        };

        _taniyici = taniyici;
        TaniyiciDili = dil;
        taniyici.RecognizeAsync(RecognizeMode.Multiple);
        DurumuAyarla(dosya is null ? SesDurumu.Dinleniyor : SesDurumu.DosyaTaniniyor);
    }

    public void DinlemeyiDurdur()
    {
        var taniyici = Interlocked.Exchange(ref _taniyici, null);
        if (taniyici is null)
            return;
        try { taniyici.RecognizeAsyncCancel(); } catch (InvalidOperationException) { }
        taniyici.Dispose();
        SesSeviyesi?.Invoke(this, 0);
        DurumuAyarla(SesDurumu.Hazir);
    }

    // ------------------------------------------------------------------

    private void DurumuAyarla(SesDurumu yeni)
    {
        if (_durum == yeni)
            return;
        _durum = yeni;
        DurumDegisti?.Invoke(this, yeni);
    }

    public void Dispose()
    {
        DinlemeyiDurdur();
        _sentezleyici.SpeakAsyncCancelAll();
        _sentezleyici.Dispose();
    }
}
