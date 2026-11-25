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

## Olası küçük değişiklik soruları

- *"Varsayılan hız 2 olsun"*: `MainForm_Load` içinde `_servis.Hiz = 2; trkHiz.Value = 2;`
- *"Güven eşiğini %50 yap"*: `new TanimaGecmisi(minimumGuven: 0.5f)`
- *"Sadece belirli komutları tanı"*: `DictationGrammar` yerine `new Grammar(new GrammarBuilder(new Choices("başlat", "durdur")))`
