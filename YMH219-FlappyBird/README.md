# YMH219-FlappyBird

Windows Forms ile geliştirilmiş, Flappy Bird benzeri bir masaüstü oyunu.
YMH219 dersi kapsamında; zamanlayıcı (Timer), klavye olayları, çarpışma kontrolü
ve oyun durumunun nesne tabanlı bir yapıyla yönetilmesini göstermek amacıyla hazırlanmıştır.

| | |
|---|---|
| **Öğrenci** | Abdulkadir Demir |
| **Öğrenci numarası** | 240541007 |
| **Ders** | Nesne Tabanlı Programlama (YMH 219) |

## Ekran Görüntüleri

| Oyun sırasında | Game Over |
|---|---|
| ![Oyun sırasında](docs/screenshots/oyun.png) | ![Game Over ekranı](docs/screenshots/gameover.png) |

## Projenin Amacı

- Görsel bileşenleri çalışma zamanında hareket ettirmek
- Timer ile bir oyun döngüsü kurmak
- Klavye olaylarıyla (KeyDown / KeyUp) oyuncuyu kontrol etmek
- Çarpışma kontrolü yapmak ve oyun durumunu (Hazır, Oynanıyor, Duraklatıldı, Game Over) sınıflarla yönetmek

## Özellikler

- Kuş klavye ile yukarı zıplar, yer çekimiyle aşağı iner
- Üç boru çifti sağdan sola akar; ekran dışına çıkan boru rastgele bir yükseklikle sağda yeniden konumlanır
- Boruya, zemine çarpınca oyun durur ve **Game Over** ekranı gösterilir
- Her geçilen boru skoru 1 artırır
- **Yeniden Başlat** butonu (ayrıca Enter / R tuşu)
- Üst bilgi çubuğunda skor, en yüksek skor, seviye ve hız
- **İsteğe bağlı geliştirmeler:**
  - En yüksek skor dosyada saklanır (`%LOCALAPPDATA%\YMH219-FlappyBird\highscore.txt`)
  - Her 5 puanda seviye atlanır ve oyun hızlanır
  - Oyun duraklatılabilir (P / Esc)

## Kontroller

| Tuş | İşlev |
|---|---|
| Boşluk / ↑ / W | Zıpla (Hazır ekranında oyunu başlatır) |
| P / Esc | Duraklat / Devam et |
| Enter / R | Game Over sonrası yeniden başlat |

## Kullanılan Teknolojiler

- C# 12
- .NET 8 (`net8.0-windows`)
- Windows Forms
- `System.Windows.Forms.Timer` (oyun döngüsü, ~60 FPS)
- GDI+ (`Graphics`) ile çizim, çift tamponlama (double buffering)

## Sınıf Yapısı

```
FlappyBird/
├── Program.cs            Uygulamanın giriş noktası
├── GameForm.cs           Form: Timer, klavye olayları, çizim, arayüz güncelleme
├── GameForm.Designer.cs  Form kontrolleri (Label, Button, Panel, Timer)
├── GamePanel.cs          Titremesiz çizim için çift tamponlu panel
└── Game/
    ├── GameManager.cs    Oyun kuralları: skor, seviye, hız, çarpışma, durum geçişleri
    ├── Bird.cs           Kuş: konum, hız, yer çekimi, zıplama
    ├── Pipe.cs           Boru çifti: hareket, yeniden konumlanma, çarpışma
    ├── GameState.cs      Oyun durumları (enum)
    └── HighScoreStore.cs En yüksek skoru dosyaya yazma / okuma
```

- **Form ile oyun mantığı ayrıdır.** `GameForm` yalnızca olayları alır ve çizer; kuralların hepsi `GameManager` içindedir.
- `GameManager`, skor ya da durum değiştiğinde `StatusChanged` olayını tetikler; form bu olayla etiketleri günceller.
- Oyun döngüsü: `Timer.Tick` → `GameManager.Update()` → `Panel.Invalidate()` → `Paint` olayında çizim.

## Çalıştırma Adımları

### Visual Studio ile
1. Repoyu klonlayın:
   ```bash
   git clone https://github.com/kdemirwarehouse/YMH219-FlappyBird.git
   ```
2. `YMH219-FlappyBird.sln` dosyasını Visual Studio 2022 ile açın.
3. **F5** ile çalıştırın.

### Komut satırı ile
```bash
cd YMH219-FlappyBird
dotnet run --project FlappyBird
```

**Gereksinim:** Windows ve [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

## Bilinen Sınırlamalar / Geliştirme Notları

- Oyun döngüsü `System.Windows.Forms.Timer` (~16 ms) ile çalışır; bu zamanlayıcı yüksek hassasiyetli değildir,
  yoğun sistem yükünde kare hızı hafifçe dalgalanabilir.
- Çarpışma kontrolü dikdörtgen (AABB) tabanlıdır; kuşun köşelerinde birkaç piksellik tolerans olabilir.
- Ses efekti yoktur; grafikler GDI+ ile çizilen basit şekillerden oluşur (harici görsel dosya kullanılmaz).
- Pencere boyutu sabittir; yeniden boyutlandırma / tam ekran desteği yoktur.
- Olası geliştirmeler: ses efektleri, zorluk seçimi (kolay/orta/zor), sprite animasyonları.
