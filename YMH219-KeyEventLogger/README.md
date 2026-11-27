# YMH219-KeyEventLogger — Key Event Logger (Eğitsel)

Windows Forms'ta klavye olaylarının (**KeyDown, KeyUp, KeyPress**) nasıl yakalandığını ve uygulama
içinde nasıl işlendiğini gösteren eğitsel bir uygulama. Yalnızca **kendi penceresi odaktayken**
basılan tuşları zaman bilgisiyle listeler ve kullanıcı isterse `.txt` dosyasına kaydeder.
Nesne Tabanlı Programlama (YMH 219) dersi ek uygulama ödevidir.

| | |
|---|---|
| **Öğrenci** | Abdulkadir Demir |
| **Öğrenci numarası** | 240541007 |
| **Ders** | Nesne Tabanlı Programlama (YMH 219) |

### ▶ Tarayıcıda dene: **https://kdemirwarehouse.github.io/YMH219-KeyEventLogger/**

Aynı mantığın tarayıcı sürümü (`web/index.html`): `keydown` / `keypress` / `keyup` olaylarını yalnızca **o sayfa
odaktayken** listeler; tekrar algılama (`event.repeat`), istatistik, zaman filtresi ve `.txt` indirme içerir.
Hiçbir veri sunucuya gönderilmez. GitHub Actions ile GitHub Pages'e yayınlanır.

## Ekran Görüntüleri

| Kayıt açık — tekrarlar (turuncu), ok/Tab/F1 ve KeyDown↔KeyPress eşleşmesi | Kayıt durduruldu — zaman filtresi aktif, kaydedilmemiş olay sayacı |
|---|---|
| ![Kayıt açık](docs/screenshots/01-kayit-acik.png) | ![Kayıt kapalı ve filtre](docs/screenshots/02-kayit-durduruldu-filtre.png) |

## Kapsam ve Güvenlik Sınırı

Bu uygulama gerçek bir sistem keylogger'ı **değildir**:

- Olaylar yalnızca formun kendi `KeyDown` / `KeyUp` / `KeyPress` olaylarından (`KeyPreview = true`) alınır.
- **Global klavye hook'u yok**, gizli / arka plan çalışma yok, başlangıçta otomatik çalışma yok, ağ iletişimi yok.
- Pencere odağı kaybedilince (`Deactivate`) kayıt **otomatik durur**; devam etmek için tekrar *Başlat*'a basılmalıdır.
- Odaktaki kontrol bir parola alanıysa (`PasswordChar` / `UseSystemPasswordChar`) olay kaydedilmez.
- Kayıtlar diske kendiliğinden yazılmaz; yalnızca kullanıcı *.txt Olarak Kaydet* ile dosya seçerse yazılır.
  Varsayılan dosya adı (`klavye-olaylari-*.txt`) `.gitignore` ile depodan dışlanmıştır.

## Kullanılan Teknolojiler

- C# 12, .NET 8 (`net8.0-windows`), Windows Forms
- `KeyPreview`, `KeyDown`, `KeyUp`, `KeyPress`, `Deactivate` olayları
- `StreamWriter` (UTF-8) ile dosya yazımı, `DateTime` ile milisaniyeli zaman damgası
- LINQ (`GroupBy`, `OrderByDescending`) ile istatistik
- **Harici NuGet paketi yoktur.**

## Kurulum ve Çalıştırma

Gereksinim: Windows 10/11 ve [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
(veya .NET masaüstü geliştirme iş yükü kurulu Visual Studio 2022).

**Visual Studio ile:** `KeyEventLogger.sln` dosyasını açın, `KeyEventLogger` başlangıç projesiyle **F5**.

**Komut satırı ile**

```powershell
git clone https://github.com/kdemirwarehouse/YMH219-KeyEventLogger.git
cd YMH219-KeyEventLogger
dotnet run --project src/KeyEventLogger
```

**Testler:** `dotnet run --project tests/KeyEventLogger.Tests` (13 test)

## Temel Özellikler

1. **Başlat / Durdur** — kayıt durumunu kontrol eder. Üstteki gösterge kayıt açıkken kırmızı
   **● KAYIT AÇIK**, kapalıyken gri **■ KAYIT KAPALI** olur; pencere başlığı da durumu gösterir.
2. **Olay listesi** — her olay için tarih-saat (milisaniye dahil), olay türü, tuş adı ve ayrıntı
   (KeyCode, değiştirici tuşlar, karakter kodu). Olay türleri renklerle ayrılır.
3. **Temizle** — ekrandaki kayıtları siler; kayıt durumu değişmez.
4. **.txt Olarak Kaydet** — kaydı durdurur, `SaveFileDialog` ile seçilen dosyaya UTF-8 olarak yazar.
   Yazma hatasında kayıtlar bellekte korunur ve kullanıcıya mesaj gösterilir.
5. **Olay türü seçimi** — KeyDown / KeyUp / KeyPress onay kutularıyla ayrı ayrı açılıp kapatılabilir.
6. **İsteğe bağlı geliştirmeler**
   - **İstatistik:** toplam tuş basımı (KeyDown sayısı), kayıtlı olay sayısı ve en sık kullanılan 5 tuş
   - **Tarih/saat aralığına göre filtreleme**; filtre aktifken kaydetme yalnızca filtrelenmiş olayları yazar
7. Bellek koruması: 5000 olayda kayıt otomatik durur.
8. **Gezinme tuşları her durumda yakalanır:** kayıt açıkken ok tuşları ve Tab, tüm kontrollerin `PreviewKeyDown`
   olayında `IsInputKey = true` yapılarak "girdi tuşu" sayılır; odak bir butonda olsa bile KeyDown/KeyUp oluşur.
   Kayıt kapalıyken normal Tab/ok ile gezinme çalışır.
9. **Basılı tutma tekrarlarının algılanması:** KeyUp gelmeden aynı tuşa ikinci KeyDown gelirse olay `[tekrar]`
   etiketiyle (turuncu) listelenir. İstatistikteki tuş basımı ve en sık tuşlar yalnızca fiziksel basımları sayar.
   **"Basılı tutma tekrarlarını yok say"** seçeneği açıkken tekrar KeyDown/KeyPress olayları hiç kaydedilmez.
10. **Kapatırken kaydetme onayı:** dosyaya yazılmamış olay varken pencere kapatılırsa *Evet* (kaydet ve kapat),
    *Hayır* (kaydetmeden kapat), *İptal* (açık kal) seçenekleri sunulur. İstatistik panelinde kaydedilmemiş
    olay sayısı görünür. Güvenlik ilkesi korunur: kayıt diske yine yalnızca kullanıcı onayıyla yazılır.
11. **KeyDown ↔ KeyPress ilişkisi gösterilir:** karakter üretmeyen tuşların (Shift, Ctrl, F1, oklar…) KeyDown
    satırında *"karakter üretmez → KeyPress olmaz"* notu yer alır (Win32 `MapVirtualKey` ile belirlenir);
    her KeyPress satırında da onu üreten KeyDown tuşu (`← KeyDown: A`) yazılır.

### Windows klavye olay modeli (özet)

| Olay | Ne zaman oluşur? | Bilgi |
|---|---|---|
| `KeyDown` | Her fiziksel tuş basımında (basılı tutulursa tekrar eder) | `KeyCode`, `Modifiers` |
| `KeyPress` | Yalnızca tuş bir **karakter** ürettiğinde (`WM_CHAR`) | `KeyChar` |
| `KeyUp` | Tuş bırakıldığında | `KeyCode` |

Bu yüzden Shift, Ctrl, F1 gibi tuşlarda KeyPress görülmemesi bir eksiklik değil, Windows'un olay modelidir;
uygulama bunu her satırda açıkça gösterir.

## Proje Yapısı

```
YMH219-KeyEventLogger/
├── KeyEventLogger.sln
├── src/
│   ├── KeyEventLogger.Core/        Arayüzden bağımsız mantık
│   │   ├── KeyLogEntry.cs          Olay kaydı (record) + OlayTuru enum
│   │   ├── KayitOturumu.cs         Başlat/Durdur/Temizle, sınır, istatistik, filtre
│   │   ├── KayitDosyasi.cs         .txt dosyasına yazma (StreamWriter)
│   │   └── TekrarAlgilayici.cs     Basılı tutma (auto-repeat) olaylarını ayırt etme
│   └── KeyEventLogger/             Windows Forms
│       ├── MainForm.cs             Klavye olayları, butonlar, gösterge
│       └── MainForm.Designer.cs
├── tests/KeyEventLogger.Tests/     Konsol test çalıştırıcısı
├── web/index.html                 Tarayıcı sürümü (GitHub Pages)
├── .github/workflows/pages.yml    web/ klasörünü GitHub Pages'e deploy eden iş akışı
└── docs/screenshots/
```

Ayrıntılı açıklama: [KOD-REHBERI.md](KOD-REHBERI.md)

## Bilinen Sınırlamalar / Geliştirme Notları

- Masaüstü sürümü yalnızca Windows'ta çalışır (Windows Forms). Diğer platformlar için tarayıcı sürümü kullanılabilir.

