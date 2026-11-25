# Kod Rehberi (Sözlü Kontrol İçin)

## Olayların yakalanması — `MainForm.cs`

- `KeyPreview = true` → odakta hangi kontrol olursa olsun klavye olayları **önce forma** gelir.
- Olay sırası bir harf için: `KeyDown` → `KeyPress` → `KeyUp`.
  - `KeyDown/KeyUp` → `KeyEventArgs`: `KeyCode`, `Modifiers` (Ctrl/Shift/Alt)
  - `KeyPress` → `KeyPressEventArgs`: üretilen karakter (`KeyChar`), ör. Shift+a → `A`
- Her olay işleyicisi önce ilgili onay kutusuna ve `ParolaAlanindaMi()` kontrolüne bakar, sonra `_oturum.Ekle(...)` çağırır.
- `MainForm_Deactivate`: pencere odağı kaybedince kayıt durdurulur (güvenlik sınırı).

## Kayıt oturumu — `KayitOturumu.cs`

- `KayitAcik` özelliğinin `set`'i `private` → durum yalnızca `Baslat()` / `Durdur()` ile değişir (kapsülleme).
- `Ekle(...)` kayıt kapalıysa `false` döner, olayı yok sayar.
- İki **event** vardır: `DurumDegisti` (gösterge güncellenir) ve `KayitEklendi` (listeye satır eklenir).
  Form bu olaylara abone olur; Core katmanı Windows Forms'u tanımaz.
- `EnSikTuslar(n)`: `Where(KeyDown) → GroupBy(Tus) → OrderByDescending(Adet) → Take(n)`.
- `Filtrele(baslangic, bitis)`: zaman aralığındaki kayıtlar; bitiş < başlangıç ise `ArgumentException`.
- Yapıcıya `Func<DateTime>` verilebilir; testlerde sahte saat kullanmak için (bağımlılık enjeksiyonu).

## Dosyaya yazma — `KayitDosyasi.cs`

- `using var yazici = new StreamWriter(yol, false, new UTF8Encoding(true))` → Türkçe karakterler bozulmaz, `using` dosyayı kapatır.
- Formda `IOException` / `UnauthorizedAccessException` yakalanır, kayıtlar bellekte korunur.

## Olası küçük değişiklik soruları

- *"Sadece harfleri kaydet"*: `MainForm_KeyDown` içinde `if (e.KeyCode < Keys.A || e.KeyCode > Keys.Z) return;`
- *"Sınırı 1000 yap"*: `new KayitOturumu(sinir: 1000)`
- *"En sık 10 tuşu göster"*: `IstatistikleriGuncelle` içinde `EnSikTuslar(10)`
