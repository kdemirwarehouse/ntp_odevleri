# Kod Rehberi (Sözlü Kontrol İçin)

Bu dosya, değerlendirme sırasında kodun hangi bölümünün ne yaptığını hızlıca açıklayabilmek için hazırlanmıştır.

## 1. Not sınıfı — `src/NotepadApp.Core/Models/Not.cs`

- Özellikler: `Id` (Guid), `Baslik`, `Icerik`, `Kategori`, `RtfIcerik`, `OlusturmaTarihi`, `DuzenlemeTarihi`.
- Parametresiz yapıcı **JSON serileştirme** için gereklidir (`JsonSerializer` nesneyi önce boş oluşturur, sonra özellikleri doldurur).
- Parametreli yapıcı başlığı doğrular (`BaslikDogrula`) ve iki tarihi de `DateTime.Now` yapar.
- `Guncelle(...)` oluşturma tarihine dokunmaz, yalnızca `DuzenlemeTarihi`ni günceller.
- `ToString()` ezilmiştir (override); `ListBox` nesneyi bu metinle gösterir.

## 2. Koleksiyon yönetimi — `NotDefteri.cs`

- Notlar `private readonly List<Not> _notlar` içinde tutulur; dışarıya yalnızca `IReadOnlyList<Not>` verilir (kapsülleme).
- `Ekle`, `Guncelle`, `Sil` her başarılı işlemden sonra `Degisti` **olayını** (event) tetikler.
- `Filtrele(aranan, kategori)` LINQ ile `Where` + `OrderByDescending` kullanır.

## 3. Dosya işlemleri ve serileştirme — `NotDeposu.cs`

- **Klasör düzeni:** `notlar.json` yolu verilirse notlar `notlar\<Id>.json` dosyalarında tutulur (her not ayrı dosya).
- **Kaydet:** her not serileştirilir; son yazılan JSON ile aynıysa atlanır, değilse `.tmp` + `File.Move` ile atomik yazılır.
  Silinen notun dosyası kaldırılır. Dönen `KayitSonucu(Yazilan, Silinen)` durum çubuğunda gösterilir.
- **Yükle:** klasördeki `*.json` dosyaları tek tek okunur; klasör yoksa eski tek dosyalı `notlar.json` okunup klasöre taşınır.
- Hata durumları:
  | Durum | Davranış |
  |---|---|
  | Dosya yok | Boş liste, uyarı yok (ilk çalıştırma) |
  | Dosya boş | Boş liste + uyarı |
  | Geçersiz JSON (`JsonException`) | Yalnızca o not dosyası `.bak` olarak yedeklenir, diğerleri yüklenir + uyarı |
  | İzin / IO hatası | Form `try/catch` ile yakalar, `MessageBox` ile bildirir |

## 4. Form — `src/NotepadApp/MainForm.cs`

- `_seciliId` düzenleyicide açık notu gösterir; `null` ise yeni not yazılıyordur.
- `_duzenleyiciKirli` düzenleyicide kaydedilmemiş değişiklik olduğunu, `_dosyaKirli` bellekteki notların dosyaya yazılmadığını gösterir.
- `_arayuzGuncelleniyor` bayrağı, kod içinden kontrol değerleri değiştirilirken `TextChanged` / `SelectedIndexChanged` olaylarının yanlışlıkla iş yapmasını engeller.
- `Defter_Degisti` → otomatik kaydetme açıksa `DosyayaKaydet()` çağrılır.
- `MainForm_FormClosing` → kaydedilmemiş değişiklik varsa sorar, "İptal"de `e.Cancel = true`.

## 5. Yeni eklenen özellikler (kod düzeyinde)

- **Çakışma — `Not.Surum` + `NotDeposu.DisaridanDegisti`:** depo her notun son görülen sürümünü ve JSON içeriğini sözlükte tutar.
  Kaydetmeden önce diskteki dosya okunur; sürüm/içerik bilinenden farklıysa yazılmaz, `KayitSonucu.Cakismalar`'a eklenir.
  Form kullanıcıya sorar: `BeniminkiniYaz` (sürüm = max + 1), `DisktekiniKabulEt` veya `KopyasiniEkle`. `FileSystemWatcher`
  (`SynchronizingObject = this`) dış değişikliklerde 600 ms gecikmeli yeniden yükleme yapar.
- **Geri al — `GeriAlGecmisi<T>`:** `LinkedList` (geri al, kapasite 200) + `Stack` (yinele). Form RTF + imleç anlık görüntüsünü
  yazma 0,6 sn durunca kaydeder; Ctrl+Z/Ctrl+Y menü kısayolları RichTextBox'tan önce işlendiği için kendi geçmişimiz kullanılır.
- **Dışa aktarma — `BicimliDisaAktarici`:** RTF aynen yazılır; HTML için küçük bir RTF ayrıştırıcısı `{ }` gruplarını yığında
  tutar, `\b \i \ul` → `<b> <i> <u>`, `\'xx` → Windows-1254, `\uN` → Unicode, `\fonttbl` vb. atlanır.
- **Etiket / sabitleme:** `Not.Etiketler` (`List<string>`), `Not.EtiketleriAyristir`; `Filtrele(aranan, kategori, etiket)`
  sabitleri önce sıralar. **XML:** `XmlNotAktarici` + `[XmlRoot("NotArsivi")]`; `NotDefteri.IceAktar` aynı Id'de yeni tarihli olanı tutar.
- **Tema — `Tema.cs`:** kontroller özyinelemeli boyanır, menüler `ProfessionalColorTable`, başlık çubuğu `DwmSetWindowAttribute(20)`.
  RichTextBox "otomatik" rengi sistem rengiyle çizdiği için metin yüklenince tema rengine boyanır, kaydederken geçici bir
  kopyada siyaha çevrilerek temadan bağımsız RTF saklanır.

## 6. Olası küçük değişiklik soruları

- *"XML'e çevir"*: `NotDeposu` içinde `JsonSerializer` yerine `XmlSerializer(typeof(List<Not>))` kullanılır; formda değişiklik gerekmez.
- *"Yeni bir alan ekle (ör. Önemli)"*: `Not` sınıfına `public bool Onemli { get; set; }` eklenir, JSON otomatik olarak yeni alanı yazar/okur.
- *"Notları başlığa göre sırala"*: `NotDefteri.Filtrele` içindeki `OrderByDescending(n => n.DuzenlemeTarihi)` → `OrderBy(n => n.Baslik)`.
