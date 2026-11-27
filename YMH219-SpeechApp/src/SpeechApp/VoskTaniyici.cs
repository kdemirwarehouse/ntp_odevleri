using System.IO.Compression;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SpeechApp.Core;
using Vosk;

namespace SpeechApp;

/// <summary>
/// Vosk (Kaldi tabanlı, çevrimdışı) konuşma tanıyıcısı ile <b>Türkçe</b> dikte.
/// <para>
/// Windows'un klasik SAPI tanıyıcısı çoğu sistemde yalnızca İngilizcedir. Vosk, internete bağlanmadan
/// bilgisayarda çalışan küçük bir Türkçe model (~35 MB, ilk kullanımda bir kez indirilir) ile tanıma yapar.
/// Mikrofon sesi NAudio ile 16 kHz / 16 bit / mono olarak alınır; .wav/.mp3 dosyaları aynı biçime dönüştürülür.
/// </para>
/// </summary>
public sealed class VoskTaniyici : IDisposable
{
    public const string ModelAdi = "vosk-model-small-tr-0.3";
    public const string ModelAdresi = "https://alphacephei.com/vosk/models/" + ModelAdi + ".zip";
    private const int OrnekleHizi = 16000;

    private static Model? _model;                 // Model yüklemesi yavaştır; bir kez yüklenip paylaşılır
    private static readonly object _modelKilidi = new();

    private VoskRecognizer? _taniyici;
    private WaveInEvent? _mikrofon;
    private CancellationTokenSource? _dosyaIptal;

    /// <summary>Mikrofon için gürültü kapısı (eşik 0 ise devre dışı).</summary>
    public GurultuKapisi Kapi { get; } = new();

    public event EventHandler<(string Metin, float Guven)>? CumleTanindi;
    public event EventHandler<string>? TahminGuncellendi;
    public event EventHandler<int>? SesSeviyesi;
    public event EventHandler? DosyaBitti;
    public event EventHandler<string>? Hata;

    static VoskTaniyici() => Vosk.Vosk.SetLogLevel(-1);   // Kaldi günlüklerini kapat

    public static string ModelKlasoru => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YMH219-SpeechApp", ModelAdi);

    /// <summary>Model kurulu mu? (Eski modellerde dosyalar kök klasörde, yenilerde am/ ve conf/ altındadır.)</summary>
    public static bool ModelKurulu =>
        File.Exists(Path.Combine(ModelKlasoru, "final.mdl")) || File.Exists(Path.Combine(ModelKlasoru, "am", "final.mdl"));

    /// <summary>Türkçe modeli indirir ve açar. İlerleme 0-100 olarak bildirilir.</summary>
    public static async Task ModelIndirAsync(IProgress<int>? ilerleme, CancellationToken iptal = default)
    {
        string ust = Path.GetDirectoryName(ModelKlasoru)!;
        Directory.CreateDirectory(ust);
        string zip = Path.Combine(ust, ModelAdi + ".zip.indiriliyor");

        using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
        using (var cevap = await http.GetAsync(ModelAdresi, HttpCompletionOption.ResponseHeadersRead, iptal))
        {
            cevap.EnsureSuccessStatusCode();
            long toplam = cevap.Content.Headers.ContentLength ?? 0;
            await using var kaynak = await cevap.Content.ReadAsStreamAsync(iptal);
            await using var hedef = File.Create(zip);
            var tampon = new byte[81920];
            long okunan = 0;
            int n;
            while ((n = await kaynak.ReadAsync(tampon, iptal)) > 0)
            {
                await hedef.WriteAsync(tampon.AsMemory(0, n), iptal);
                okunan += n;
                if (toplam > 0) ilerleme?.Report((int)(okunan * 100 / toplam));
            }
        }

        // Zip içinde "vosk-model-small-tr-0.3/..." klasörü vardır
        if (Directory.Exists(ModelKlasoru))
            Directory.Delete(ModelKlasoru, recursive: true);
        ZipFile.ExtractToDirectory(zip, ust, overwriteFiles: true);
        File.Delete(zip);
        if (!ModelKurulu)
            throw new InvalidDataException("İndirilen model dosyası beklenen yapıda değil.");
    }

    private static Model ModeliYukle()
    {
        lock (_modelKilidi)
        {
            if (!ModelKurulu)
                throw new InvalidOperationException("Türkçe Vosk modeli kurulu değil. Önce modeli indirin.");
            return _model ??= new Model(ModelKlasoru);
        }
    }

    private VoskRecognizer YeniTaniyici()
    {
        var r = new VoskRecognizer(ModeliYukle(), OrnekleHizi);
        r.SetWords(true);   // kelime başına güven skoru için
        return r;
    }

    private void Besle(VoskRecognizer r, byte[] tampon, int uzunluk)
    {
        if (r.AcceptWaveform(tampon, uzunluk))
            SonucuBildir(r.Result());
        else
        {
            string ara = VoskSonucu.AraSonuc(r.PartialResult());
            if (ara.Length > 0) TahminGuncellendi?.Invoke(this, ara);
        }
    }

    private void SonucuBildir(string json)
    {
        var (metin, guven) = VoskSonucu.TamSonuc(json);
        if (metin.Length > 0)
            CumleTanindi?.Invoke(this, (metin, guven));
    }

    public void MikrofondanBaslat()
    {
        Durdur();
        _taniyici = YeniTaniyici();
        if (WaveInEvent.DeviceCount == 0)
        {
            Durdur();
            throw new InvalidOperationException("Mikrofon bulunamadı. Bir mikrofon bağlayın ve Windows Gizlilik ayarlarında erişime izin verin.");
        }

        var mikrofon = new WaveInEvent { WaveFormat = new WaveFormat(OrnekleHizi, 16, 1), BufferMilliseconds = 100 };
        mikrofon.DataAvailable += (_, e) =>
        {
            int seviye = VoskSonucu.SesSeviyesi(e.Buffer, e.BytesRecorded);
            SesSeviyesi?.Invoke(this, seviye);
            Kapi.Uygula(e.Buffer, e.BytesRecorded, seviye);   // eşik altı blok sessizliğe çevrilir
            lock (_modelKilidi)   // Durdur ile aynı anda tanıyıcıya erişilmesin
                if (_taniyici is { } r) Besle(r, e.Buffer, e.BytesRecorded);
        };
        mikrofon.RecordingStopped += (_, e) =>
        {
            if (e.Exception is not null)
                Hata?.Invoke(this, "Mikrofon kaydı durdu: " + e.Exception.Message);
        };
        try
        {
            mikrofon.StartRecording();
        }
        catch (NAudio.MmException ex)
        {
            mikrofon.Dispose();
            Durdur();
            throw new InvalidOperationException("Mikrofona erişilemedi (izin kapalı veya aygıt meşgul olabilir).\n\nAyrıntı: " + ex.Message, ex);
        }
        _mikrofon = mikrofon;
    }

    /// <summary>.wav (veya Windows'un açabildiği .mp3) dosyasını 16 kHz mono'ya dönüştürüp tanır.</summary>
    public void DosyadanBaslat(string yol)
    {
        Durdur();
        AudioFileReader okuyucu;
        try
        {
            okuyucu = new AudioFileReader(yol);
        }
        catch (Exception ex) when (ex is IOException or FormatException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            throw new InvalidOperationException("Ses dosyası açılamadı.\n\nAyrıntı: " + ex.Message, ex);
        }

        var r = YeniTaniyici();   // dosya görevi kendi tanıyıcısını kullanır ve sonunda kendisi kapatır
        var iptal = _dosyaIptal = new CancellationTokenSource();
        Task.Run(() =>
        {
            try
            {
                ISampleProvider kaynak = okuyucu;
                if (kaynak.WaveFormat.Channels == 2)
                    kaynak = new StereoToMonoSampleProvider(kaynak);
                if (kaynak.WaveFormat.SampleRate != OrnekleHizi)
                    kaynak = new WdlResamplingSampleProvider(kaynak, OrnekleHizi);
                var pcm = new SampleToWaveProvider16(kaynak);

                var tampon = new byte[8000];   // 0.25 sn
                int n;
                while (!iptal.IsCancellationRequested && (n = pcm.Read(tampon, 0, tampon.Length)) > 0)
                {
                    SesSeviyesi?.Invoke(this, VoskSonucu.SesSeviyesi(tampon, n));
                    Besle(r, tampon, n);
                }
                if (!iptal.IsCancellationRequested)
                    SonucuBildir(r.FinalResult());
            }
            catch (Exception ex)
            {
                Hata?.Invoke(this, "Dosya tanıma hatası: " + ex.Message);
            }
            finally
            {
                okuyucu.Dispose();
                r.Dispose();
                if (!iptal.IsCancellationRequested)
                    DosyaBitti?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    /// <summary>
    /// Mikrofonu kısa süre dinleyip (kullanıcı susarken) ortam gürültüsünün 100 ms'lik blok seviyelerini döndürür.
    /// </summary>
    public static async Task<List<int>> OrtamGurultusunuOlcAsync(TimeSpan sure)
    {
        if (WaveInEvent.DeviceCount == 0)
            throw new InvalidOperationException("Mikrofon bulunamadı.");
        var seviyeler = new List<int>();
        using var mikrofon = new WaveInEvent { WaveFormat = new WaveFormat(OrnekleHizi, 16, 1), BufferMilliseconds = 100 };
        mikrofon.DataAvailable += (_, e) => { lock (seviyeler) seviyeler.Add(VoskSonucu.SesSeviyesi(e.Buffer, e.BytesRecorded)); };
        try { mikrofon.StartRecording(); }
        catch (NAudio.MmException ex) { throw new InvalidOperationException("Mikrofona erişilemedi: " + ex.Message, ex); }
        await Task.Delay(sure);
        mikrofon.StopRecording();
        lock (seviyeler) return seviyeler.ToList();
    }

    public void Durdur()
    {
        _dosyaIptal?.Cancel();
        _dosyaIptal = null;
        var mikrofon = Interlocked.Exchange(ref _mikrofon, null);
        if (mikrofon is not null)
        {
            mikrofon.StopRecording();
            mikrofon.Dispose();
        }
        VoskRecognizer? eski;
        lock (_modelKilidi)
            eski = Interlocked.Exchange(ref _taniyici, null);
        if (eski is not null)
        {
            if (mikrofon is not null)
                SonucuBildir(eski.FinalResult());   // son cümleyi kaçırma
            eski.Dispose();
        }
        SesSeviyesi?.Invoke(this, 0);
    }

    public void Dispose() => Durdur();
}
