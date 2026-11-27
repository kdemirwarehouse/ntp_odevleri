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
    private readonly OneCoreSeslendirici? _oneCore;
    private readonly List<SesSecenegi> _sesler = new();
    private SesSecenegi? _seciliSes;
    private SpeechRecognitionEngine? _taniyici;
    private VoskTaniyici? _vosk;
    private SesDurumu _durum = SesDurumu.Hazir;

    /// <summary>Kullanılacak tanıma motoru. Vosk, Türkçe modeli kurulu olduğunda seçilebilir.</summary>
    public TanimaMotoru Motor { get; set; } = TanimaMotoru.Sapi;

    /// <summary>Vosk mikrofonu için gürültü kapısı eşiği (0-100, 0 = kapalı).</summary>
    public int GurultuEsigi
    {
        get => _gurultuEsigi;
        set { _gurultuEsigi = Math.Clamp(value, 0, 100); if (_vosk is not null) _vosk.Kapi.Esik = _gurultuEsigi; }
    }
    private int _gurultuEsigi;

    public event EventHandler<SesDurumu>? DurumDegisti;
    public event EventHandler<(string Metin, float Guven)>? CumleTanindi;
    public event EventHandler<string>? TahminGuncellendi;   // konuşma sürerken ara sonuç
    public event EventHandler<int>? SesSeviyesi;             // 0-100 mikrofon seviyesi
    public event EventHandler<CharacterPosition>? OkunanKelime;
    public event EventHandler<string>? Hata;

    public readonly record struct CharacterPosition(int Baslangic, int Uzunluk);

    /// <summary>Seçilebilir ses: hangi motora (SAPI / OneCore) ait olduğu ile birlikte.</summary>
    public sealed record SesSecenegi(string Ad, string Kultur, SesMotoru Motor)
    {
        public override string ToString() => $"{Ad} ({Kultur}) — {(Motor == SesMotoru.OneCore ? "OneCore" : "SAPI")}";
    }

    public KonusmaServisi()
    {
        _sentezleyici.SetOutputToDefaultAudioDevice();
        _sentezleyici.SpeakStarted += (_, _) => DurumuAyarla(SesDurumu.Okunuyor);
        _sentezleyici.SpeakCompleted += (_, _) => { if (_durum is SesDurumu.Okunuyor or SesDurumu.Duraklatildi) DurumuAyarla(SesDurumu.Hazir); };
        _sentezleyici.SpeakProgress += (_, e) => OkunanKelime?.Invoke(this, new CharacterPosition(e.CharacterPosition, e.CharacterCount));

        foreach (var v in _sentezleyici.GetInstalledVoices().Where(v => v.Enabled))
            _sesler.Add(new SesSecenegi(v.VoiceInfo.Name, v.VoiceInfo.Culture.Name, SesMotoru.Sapi));

        // OneCore (Windows 10+) sesleri: Türkçe gibi SAPI'de olmayan dilleri de içerir
        var oneCoreSesleri = OneCoreSeslendirici.Sesler();
        if (oneCoreSesleri.Count > 0)
        {
            _oneCore = new OneCoreSeslendirici();
            _oneCore.Basladi += (_, _) => DurumuAyarla(SesDurumu.Okunuyor);
            _oneCore.Bitti += (_, _) => { if (_durum is SesDurumu.Okunuyor or SesDurumu.Duraklatildi) DurumuAyarla(SesDurumu.Hazir); };
            _oneCore.KelimeOkunuyor += (_, k) => OkunanKelime?.Invoke(this, new CharacterPosition(k.Baslangic, k.Uzunluk));
            _oneCore.Hata += (_, m) => Hata?.Invoke(this, m);
            foreach (var (ad, kultur) in oneCoreSesleri)
                _sesler.Add(new SesSecenegi(ad, kultur, SesMotoru.OneCore));
        }
    }

    public SesDurumu Durum => _durum;

    // ------------------------------------------------------------------ Text-to-Speech

    /// <summary>SAPI ve OneCore seslerinin birleşik listesi.</summary>
    public IReadOnlyList<SesSecenegi> Sesler() => _sesler;

    /// <summary>Arayüz diline (ör. tr-TR) en uygun sesin listedeki sırası; ses yoksa -1.</summary>
    public int VarsayilanSesIndeksi(string tercihKultur)
    {
        string? kultur = DilSecici.EnUygun(_sesler.Select(s => s.Kultur), tercihKultur);
        return kultur is null ? -1 : _sesler.FindIndex(s => s.Kultur == kultur);
    }

    public SesSecenegi? SeciliSes => _seciliSes;

    public void SesSec(int indeks)
    {
        if (indeks < 0 || indeks >= _sesler.Count)
            return;
        OkumayiDurdur();
        _seciliSes = _sesler[indeks];
        if (_seciliSes.Motor == SesMotoru.Sapi)
            _sentezleyici.SelectVoice(_seciliSes.Ad);
        else
            _oneCore!.SesSec(_seciliSes.Ad);
    }

    private bool OneCoreAktif => _seciliSes?.Motor == SesMotoru.OneCore && _oneCore is not null;

    /// <summary>Okuma hızı: -10 (yavaş) ile 10 (hızlı) arası.</summary>
    public int Hiz
    {
        get => _sentezleyici.Rate;
        set { _sentezleyici.Rate = Math.Clamp(value, -10, 10); if (_oneCore is not null) _oneCore.Hiz = _sentezleyici.Rate; }
    }

    /// <summary>Ses düzeyi: 0-100.</summary>
    public int Duzey
    {
        get => _sentezleyici.Volume;
        set { _sentezleyici.Volume = Math.Clamp(value, 0, 100); if (_oneCore is not null) _oneCore.Duzey = _sentezleyici.Volume; }
    }

    public void Oku(string metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            throw new InvalidOperationException("Okunacak metin yok. Lütfen metin kutusuna bir şeyler yazın.");
        if (_durum is SesDurumu.Dinleniyor or SesDurumu.DosyaTaniniyor)
            throw new InvalidOperationException("Konuşma tanıma çalışırken okuma yapılamaz. Önce dinlemeyi durdurun.");

        if (OneCoreAktif)
        {
            _ = _oneCore!.OkuAsync(metin);
            return;
        }
        _sentezleyici.SpeakAsyncCancelAll();
        if (_sentezleyici.State == SynthesizerState.Paused)
            _sentezleyici.Resume();
        _sentezleyici.SpeakAsync(metin);
    }

    /// <summary>Okuma sürüyorsa duraklatır, duraklatılmışsa devam ettirir.</summary>
    public void DuraklatDevam()
    {
        if (OneCoreAktif)
        {
            if (_durum == SesDurumu.Okunuyor) { _oneCore!.Duraklat(); DurumuAyarla(SesDurumu.Duraklatildi); }
            else if (_durum == SesDurumu.Duraklatildi) { _oneCore!.Devam(); DurumuAyarla(SesDurumu.Okunuyor); }
            return;
        }
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
        _oneCore?.Durdur();
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
        if (OneCoreAktif)
        {
            // UI iş parçacığında WinRT async işlemini beklerken kilitlenmemek için iş parçacığı havuzunda çalıştır
            Task.Run(() => _oneCore!.WavOlarakKaydetAsync(metin, dosyaYolu)).GetAwaiter().GetResult();
            return;
        }
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

        if (Motor == TanimaMotoru.Vosk)
        {
            VoskIleBaslat(dosya);
            return;
        }

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

    private void VoskIleBaslat(string? dosya)
    {
        if (_vosk is null)
        {
            _vosk = new VoskTaniyici();
            _vosk.CumleTanindi += (_, s) => CumleTanindi?.Invoke(this, s);
            _vosk.TahminGuncellendi += (_, t) => TahminGuncellendi?.Invoke(this, t);
            _vosk.SesSeviyesi += (_, s) => SesSeviyesi?.Invoke(this, s);
            _vosk.Hata += (_, m) => Hata?.Invoke(this, m);
            _vosk.DosyaBitti += (_, _) => Task.Run(DinlemeyiDurdur);
        }
        _vosk.Kapi.Esik = _gurultuEsigi;

        if (dosya is null)
            _vosk.MikrofondanBaslat();
        else
            _vosk.DosyadanBaslat(dosya);
        TaniyiciDili = "tr-TR";
        DurumuAyarla(dosya is null ? SesDurumu.Dinleniyor : SesDurumu.DosyaTaniniyor);
    }

    public void DinlemeyiDurdur()
    {
        bool calisiyordu = _durum is SesDurumu.Dinleniyor or SesDurumu.DosyaTaniniyor;
        _vosk?.Durdur();
        var taniyici = Interlocked.Exchange(ref _taniyici, null);
        if (taniyici is not null)
        {
            try { taniyici.RecognizeAsyncCancel(); } catch (InvalidOperationException) { }
            taniyici.Dispose();
        }
        if (taniyici is null && !calisiyordu)
            return;
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
        _oneCore?.Dispose();
        _vosk?.Dispose();
    }
}
