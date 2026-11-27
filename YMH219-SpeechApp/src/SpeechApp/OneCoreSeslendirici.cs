using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

namespace SpeechApp;

/// <summary>
/// Windows 10/11'in modern (OneCore) konuşma sentezleyicisi: <c>Windows.Media.SpeechSynthesis</c>.
/// <para>
/// Klasik SAPI (System.Speech) çoğu sistemde yalnızca İngilizce masaüstü seslerini görür; OneCore ise Windows
/// "Dil ve bölge" ayarlarından kurulan seslerin tamamını (ör. Türkçe <b>Microsoft Tolga</b>) kullanabilir.
/// Sentezlenen WAV akışı <see cref="MediaPlayer"/> ile çalınır; böylece duraklatma/devam ve kelime sınırı
/// (word boundary) zaman bilgisiyle okunan kelimenin vurgulanması desteklenir.
/// </para>
/// </summary>
public sealed class OneCoreSeslendirici : IDisposable
{
    private readonly SpeechSynthesizer _sentezleyici = new();
    private readonly MediaPlayer _oynatici = new() { AutoPlay = false };
    private int _surum;   // eski bir okumanın geç gelen olaylarını yok saymak için

    public event EventHandler? Basladi;
    public event EventHandler? Bitti;
    public event EventHandler<(int Baslangic, int Uzunluk)>? KelimeOkunuyor;
    public event EventHandler<string>? Hata;

    public OneCoreSeslendirici()
    {
        _sentezleyici.Options.IncludeWordBoundaryMetadata = true;
        _oynatici.MediaEnded += (_, _) => Bitti?.Invoke(this, EventArgs.Empty);
        _oynatici.MediaFailed += (_, e) => Hata?.Invoke(this, "Ses çalınamadı: " + e.ErrorMessage);
    }

    /// <summary>Kurulu OneCore sesleri (ad, kültür).</summary>
    public static IReadOnlyList<(string Ad, string Kultur)> Sesler()
    {
        try
        {
            return SpeechSynthesizer.AllVoices.Select(v => (v.DisplayName, v.Language)).ToList();
        }
        catch (Exception)
        {
            return Array.Empty<(string, string)>();   // Eski Windows / WinRT kullanılamıyor
        }
    }

    public void SesSec(string ad)
    {
        var ses = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.DisplayName == ad);
        if (ses is not null)
            _sentezleyici.Voice = ses;
    }

    /// <summary>-10…10 arası hızı OneCore'un 0.5…3.0 katsayısına çevirir (0 → 1.0).</summary>
    public static double HizKatsayisi(int hiz) => hiz <= 0 ? 1.0 + hiz * 0.05 : 1.0 + hiz * 0.2;

    public int Hiz { get; set; }
    public int Duzey { get; set; } = 100;

    public bool Caliyor => _oynatici.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
    public bool Duraklatildi => _oynatici.PlaybackSession.PlaybackState == MediaPlaybackState.Paused && _oynatici.Source is not null;

    private async Task<SpeechSynthesisStream> SentezleAsync(string metin)
    {
        _sentezleyici.Options.SpeakingRate = HizKatsayisi(Hiz);
        _sentezleyici.Options.AudioVolume = Math.Clamp(Duzey, 0, 100) / 100.0;
        return await _sentezleyici.SynthesizeTextToStreamAsync(metin);
    }

    public async Task OkuAsync(string metin)
    {
        int surum = Interlocked.Increment(ref _surum);
        _oynatici.Pause();
        try
        {
            var akis = await SentezleAsync(metin);
            if (surum != _surum) return;   // bu sırada yeni okuma başlatıldı / durduruldu

            var oge = new MediaPlaybackItem(MediaSource.CreateFromStream(akis, akis.ContentType));
            var bagliIzler = new HashSet<TimedMetadataTrack>();
            void IzleriBagla()
            {
                for (int i = 0; i < oge.TimedMetadataTracks.Count; i++)
                {
                    var iz = oge.TimedMetadataTracks[i];
                    if (iz.Id != "SpeechWord" || !bagliIzler.Add(iz)) continue;
                    oge.TimedMetadataTracks.SetPresentationMode((uint)i, TimedMetadataTrackPresentationMode.ApplicationPresented);
                    iz.CueEntered += (_, e) =>
                    {
                        if (surum == _surum && e.Cue is SpeechCue c && c.StartPositionInInput is int bas && c.EndPositionInInput is int son)
                            KelimeOkunuyor?.Invoke(this, (bas, son - bas + 1));
                    };
                }
            }
            // İzler oluşturulurken hazır olabilir ya da sonradan eklenebilir: ikisini de ele al
            oge.TimedMetadataTracksChanged += (_, _) => IzleriBagla();
            IzleriBagla();
            _oynatici.Source = oge;
            _oynatici.Play();
            Basladi?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Hata?.Invoke(this, "OneCore sentezleyici okuyamadı: " + ex.Message);
            Bitti?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Duraklat() => _oynatici.Pause();
    public void Devam() => _oynatici.Play();

    public void Durdur()
    {
        Interlocked.Increment(ref _surum);
        _oynatici.Pause();
        _oynatici.Source = null;
    }

    /// <summary>Metni WAV dosyasına yazar (OneCore akışı zaten RIFF/WAV biçimindedir).</summary>
    public async Task WavOlarakKaydetAsync(string metin, string dosyaYolu)
    {
        using var akis = await SentezleAsync(metin);
        using var okuyucu = new DataReader(akis.GetInputStreamAt(0));
        uint boyut = (uint)akis.Size;
        await okuyucu.LoadAsync(boyut);
        var veri = new byte[boyut];
        okuyucu.ReadBytes(veri);
        await File.WriteAllBytesAsync(dosyaYolu, veri);
    }

    public void Dispose()
    {
        Durdur();
        _oynatici.Dispose();
        _sentezleyici.Dispose();
    }
}
