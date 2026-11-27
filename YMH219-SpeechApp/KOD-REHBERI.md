# Kod Rehberi (Sözlü Kontrol İçin)

## KonusmaServisi — `src/SpeechApp/KonusmaServisi.cs`

- **Sarmalayıcı (wrapper) sınıf**: Form `System.Speech` türlerini doğrudan kullanmaz; yalnızca `Oku`, `DuraklatDevam`,
  `OkumayiDurdur`, `DinlemeyiBaslat`, `DinlemeyiDurdur` metotlarını ve olayları kullanır.
- **TTS:** `SpeechSynthesizer.SpeakAsync(metin)` — asenkron okur, form donmaz.
  `SpeakStarted` / `SpeakCompleted` → durum değişir; `SpeakProgress` → okunan kelimenin konumu (vurgulama).
  Duraklatılmış konuşma doğrudan iptal edilemediği için `OkumayiDurdur` önce `Resume()` çağırır.
- **STT:** `new SpeechRecognitionEngine(kultur)` → `LoadGrammar(new DictationGrammar())` →
  `SetInputToDefaultAudioDevice()` (mikrofon) veya `SetInputToWaveFile(yol)` → `RecognizeAsync(RecognizeMode.Multiple)`.
  - `SpeechRecognized` → kesin sonuç (`Result.Text`, `Result.Confidence`)
  - `SpeechHypothesized` → konuşma sürerken ara tahmin
  - `AudioLevelUpdated` → mikrofon seviyesi
- Mikrofon yoksa `SetInputToDefaultAudioDevice` `InvalidOperationException` fırlatır; servis bunu anlaşılır bir mesajla yeniden fırlatır, form `MessageBox` ile gösterir.
- **IDisposable:** sentezleyici ve tanıma motoru yönetilmeyen ses kaynakları tutar; form kapanırken `Dispose()` çağrılır.

## Core sınıfları

- `SesDurumu` enum + `Aciklama()` **extension method**: arayüzdeki renkli şerit buna göre güncellenir.
- `TanimaGecmisi`: `Ekle(metin, guven)` → boş veya `MinimumGuven` altındaki sonuçları reddeder. `TxtOlarakKaydet` → `StreamWriter`.
- `DilSecici.EnUygun(...)`: tam eşleşme → aynı dil → en-US → ilk kurulu (testlerle doğrulanmış).

## İş parçacıkları

Tanıma olayları arka plan iş parçacığından gelir. Formda `UIdaCalistir(() => ...)` → `BeginInvoke` ile arayüz güncellenir.

## Yeni eklenen motorlar (kod düzeyinde)

- **OneCore TTS — `OneCoreSeslendirici.cs`:** proje hedefi `net8.0-windows10.0.19041.0` olduğu için WinRT API'leri doğrudan
  kullanılır. `SpeechSynthesizer.SynthesizeTextToStreamAsync` (Options: `IncludeWordBoundaryMetadata`, `SpeakingRate`,
  `AudioVolume`) → `MediaPlaybackItem` → `MediaPlayer`. Kelime sınırları `TimedMetadataTrack` ("SpeechWord") izlerinde
  `SpeechCue` olarak gelir; `CueEntered` ile vurgulanır. Eski okumanın geç olayları `_surum` sayacıyla yok sayılır.
- **Vosk — `VoskTaniyici.cs`:** `Model` statik olarak bir kez yüklenir. Mikrofon: NAudio `WaveInEvent` (16 kHz, 16 bit, mono),
  her 100 ms'lik blok `AcceptWaveform`'a verilir; `true` dönerse `Result()` (tam cümle), değilse `PartialResult()`.
  Dosya: `AudioFileReader` → `StereoToMono` → `WdlResampling(16000)` → `SampleToWaveProvider16`; dosya görevi kendi
  `VoskRecognizer`'ını kullanır (Durdur ile aynı nesneye iki iş parçacığından erişilmesin).
- **`VoskSonucu` (Core):** JSON'dan metin ve kelime `conf` ortalamasını çıkarır; PCM tamponundan 0-100 seviye hesaplar.
- **`GurultuKapisi` (Core):** seviye < eşik olan blok `Array.Clear` ile sessizliğe çevrilir; eşik aşılınca 4 blok açık kalır.
  `OnerilenEsik` en yüksek %10'u atıp ortalama·1,5 + 3 hesaplar.

## Olası küçük değişiklik soruları

- *"Varsayılan hız 2 olsun"*: `MainForm_Load` içinde `_servis.Hiz = 2; trkHiz.Value = 2;`
- *"Güven eşiğini %50 yap"*: `new TanimaGecmisi(minimumGuven: 0.5f)`
- *"Sadece belirli komutları tanı"*: `DictationGrammar` yerine `new Grammar(new GrammarBuilder(new Choices("başlat", "durdur")))`
