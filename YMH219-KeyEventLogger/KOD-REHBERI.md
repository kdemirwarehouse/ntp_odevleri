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

## Yeni eklenen özellikler (kod düzeyinde)

- **Ok tuşları / Tab:** `GezinmeTuslariniYakala` tüm kontrollerin `PreviewKeyDown` olayına bağlanır; kayıt açıkken bu tuşlar için
  `e.IsInputKey = true` yapılır. Böylece Windows onları "iletişim kutusu tuşu" olarak işleyip odağı taşımaz, KeyDown/KeyUp oluşur.
- **Tekrar algılama — `TekrarAlgilayici.cs` (Core):** basılı tuşlar `HashSet` içinde; KeyUp gelmeden aynı tuşa KeyDown gelirse tekrar.
  `Deactivate`'te küme temizlenir (odak dışında bırakılan tuşun KeyUp'ı gelmez). `KeyLogEntry.Tekrar` → `[tekrar]` etiketi;
  istatistikler `!Tekrar` olan KeyDown'ları sayar.
- **KeyPress ilişkisi:** `MapVirtualKey(vk, MAPVK_VK_TO_CHAR)` 0 dönerse tuş karakter üretmez → satıra not düşülür.
  Son KeyDown tuşu saklanıp KeyPress satırına `← KeyDown: X` yazılır.
- **Kapatma onayı:** `KayitOturumu.KaydedilmemisOlaySayisi` her `Ekle`'de artar, başarılı kayıt / temizlemede sıfırlanır.
  `FormClosing`'de > 0 ise Evet/Hayır/İptal; İptal veya başarısız kayıtta `e.Cancel = true`.

## Olası küçük değişiklik soruları

- *"Sadece harfleri kaydet"*: `MainForm_KeyDown` içinde `if (e.KeyCode < Keys.A || e.KeyCode > Keys.Z) return;`
- *"Sınırı 1000 yap"*: `new KayitOturumu(sinir: 1000)`
- *"En sık 10 tuşu göster"*: `IstatistikleriGuncelle` içinde `EnSikTuslar(10)`
