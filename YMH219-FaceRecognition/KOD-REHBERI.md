# Kod Rehberi (Sözlü Kontrol İçin)

## İşlem hattı (her kare için)

```
Mat (BGR) ─► GriyeCevir + EqualizeHist ─► CascadeClassifier.DetectMultiScale ─► Rect[] (yüzler)
                                                    │
         her yüz için: YuzuHazirla (kırp, gri, 100x100, EqualizeHist)
                                                    │
                     LBPHFaceRecognizer.Predict ─► (etiket Id, mesafe)
                                                    │
                TanimaKarari.Olustur(etiket, mesafe, eşik) ─► Ciz ─► Bitmap ─► PictureBox
```

## Sınıflar

- **`GoruntuServisi`** (IDisposable): `CascadeClassifier` ve `LBPHFaceRecognizer` nesnelerini tutar.
  - Yapıcı model dosyası yoksa `FileNotFoundException`, bozuksa `InvalidDataException` fırlatır → form `MessageBox` gösterir.
  - `Egit(depo)`: depodaki tüm örnekleri gri okur, `Train(goruntuler, idler)`. Etiketler sayıya çevrilir (`EtiketBilgisi.Id`), ad ↔ Id eşlemesi sözlükte tutulur.
  - Kamera görevi ile "Modeli Eğit" aynı anda modele erişebileceği için `lock (_modelKilidi)` kullanılır.
  - `Varyantlar(...)`: `yield return` ile veri artırma (ayna, parlaklık, döndürme).
- **`KameraServisi`**: `VideoCapture` → `Task.Run` içinde döngü → `KareGeldi` olayı. `CancellationToken` ile durdurulur.
- **`OrnekDeposu`** (Core): klasör = etiket. `YeniOrnekYolu` sıradaki `NNN.png` adını üretir. OpenCV kullanmadığı için birim test edilebilir.
- **`TanimaKarari`** (Core, `record struct`): LBPH'de **küçük mesafe = daha benzer**. `mesafe <= eşik` ise tanındı.

## Arayüz ve iş parçacıkları

- Kamera kareleri arka plan iş parçacığında işlenir; sonuç `Bitmap` `BeginInvoke` ile UI'a aktarılır.
  `Invoke` (senkron) kullanılmaz, çünkü "Kamera Durdur" UI iş parçacığında görevi beklerken kilitlenme (deadlock) olurdu.
- `_kareIsleniyor` bayrağı: arayüz önceki kareyi göstermeden yeni kare işlenmez (kare atlama).
- Eski `pbGoruntu.Image` her yeni karede `Dispose` edilir (bellek sızıntısı önlenir).

## Olası küçük değişiklik soruları

- *"Eşiği 100 yap"*: Designer'da `nudEsik.Value = 100` veya `TanimaKarari.VarsayilanEsik`.
- *"Daha küçük yüzleri de algıla"*: `YuzleriBul` içinde `minKenar` alt sınırını 40'tan 25'e indir.
- *"Veri artırmada ayna kopyasını kapat"*: `Varyantlar` içindeki `Cv2.Flip` bloğunu kaldır.
- *"Bilinmeyen yüzleri kırmızı yerine sarı çiz"*: `Ciz` içinde `new Scalar(40, 40, 230)` → `new Scalar(0, 220, 255)` (BGR sırası!).
