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

- **Kaydet:** `JsonSerializer.Serialize(list)` → `StreamWriter` ile `.tmp` dosyasına yaz → `File.Move` ile asıl dosyanın yerine koy.
- **Yükle:** `StreamReader.ReadToEnd()` → `JsonSerializer.Deserialize<List<Not>>`.
- Hata durumları:
  | Durum | Davranış |
  |---|---|
  | Dosya yok | Boş liste, uyarı yok (ilk çalıştırma) |
  | Dosya boş | Boş liste + uyarı |
  | Geçersiz JSON (`JsonException`) | Dosya `.bak` olarak yedeklenir, boş liste + uyarı |
  | İzin / IO hatası | Form `try/catch` ile yakalar, `MessageBox` ile bildirir |

## 4. Form — `src/NotepadApp/MainForm.cs`

- `_seciliId` düzenleyicide açık notu gösterir; `null` ise yeni not yazılıyordur.
- `_duzenleyiciKirli` düzenleyicide kaydedilmemiş değişiklik olduğunu, `_dosyaKirli` bellekteki notların dosyaya yazılmadığını gösterir.
- `_arayuzGuncelleniyor` bayrağı, kod içinden kontrol değerleri değiştirilirken `TextChanged` / `SelectedIndexChanged` olaylarının yanlışlıkla iş yapmasını engeller.
- `Defter_Degisti` → otomatik kaydetme açıksa `DosyayaKaydet()` çağrılır.
- `MainForm_FormClosing` → kaydedilmemiş değişiklik varsa sorar, "İptal"de `e.Cancel = true`.

## 5. Olası küçük değişiklik soruları

- *"XML'e çevir"*: `NotDeposu` içinde `JsonSerializer` yerine `XmlSerializer(typeof(List<Not>))` kullanılır; formda değişiklik gerekmez.
- *"Yeni bir alan ekle (ör. Önemli)"*: `Not` sınıfına `public bool Onemli { get; set; }` eklenir, JSON otomatik olarak yeni alanı yazar/okur.
- *"Notları başlığa göre sırala"*: `NotDefteri.Filtrele` içindeki `OrderByDescending(n => n.DuzenlemeTarihi)` → `OrderBy(n => n.Baslik)`.
