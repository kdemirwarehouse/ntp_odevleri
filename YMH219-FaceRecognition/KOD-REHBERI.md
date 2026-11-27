# Kod Rehberi (Sözlü Kontrol İçin)

## İşlem hattı (her kare için)

```
Mat (BGR) ─► YuNet (FaceDetectorYN) veya Haar ─► YuzBolgesi[] (kutu + göz noktaları)
                                                    │
         her yüz için: YuzuHazirla (göz hizalama veya kırpma, gri, 100x100, CLAHE)
                                                    │
                     LBPHFaceRecognizer.Predict ─► (etiket Id, mesafe)
                                                    │
     TanimaKarari.Olustur(etiket, mesafe, eşik, kalibrasyon) ─► Ciz (çerçeve) ─► Bitmap ─► EtiketleriCiz (GDI+) ─► PictureBox
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

## Yeni eklenen özellikler (kod düzeyinde)

- **YuNet — `GoruntuServisi.YuNetIleBul`:** `FaceDetectorYN.Create(onnx, "", boyut, 0.8, 0.3, 5000)`; görüntü en fazla 640 px'e
  küçültülür, algılayıcı boyuta göre önbelleklenir. Çıktı `Mat` satırı: x, y, w, h, sağ göz, sol göz, burun, ağız (x, y) ve skor.
  Model yoksa veya `OpenCVException` olursa Haar'a düşülür. Sonuç `YuzBolgesi(Bolge, SolGoz, SagGoz, Skor)`.
- **Hizalama — `YuzHizalama.Hesapla` (Core):** göz çizgisinin açısı `atan2(dy, dx)`, ölçek = 100·0,40 / göz mesafesi;
  `GetRotationMatrix2D(gözOrtası, açı, ölçek)` matrisinin öteleme sütununa (hedef − merkez) eklenip tek `WarpAffine` ile
  100x100 hizalı yüz üretilir (gözler hep (30,36) ve (70,36)'da). Ardından `CLAHE(2.0, 8x8)`.
- **Kalibrasyon — `MesafeKalibrasyonu` (Core) + `GoruntuServisi.Kalibre`:** her kişi için örnekler 5 ardışık parçaya bölünür;
  kişinin diğer parçalarıyla eğitilen modelden *gerçek*, yalnızca diğer kişilerle eğitilen modelden *sahte* mesafeler toplanır.
  Eşik = FAR + FRR'yi en aza indiren değer; güven = 1 / (1 + e^((d − eşik)/eğim)).
- **Etiket çizimi — `EtiketleriCiz`:** OpenCV yalnızca çerçeveyi çizer; yazılar `Mat.ToBitmap()` sonrasında GDI+ ile yazılır.
- **Otomatik toplama:** `System.Windows.Forms.Timer` (400 ms) her tikte son kareyi alır; karede tek yüz varsa hizalanmış örneği kaydeder.

## Olası küçük değişiklik soruları

- *"Eşiği 100 yap"*: Designer'da `nudEsik.Value = 100` veya `TanimaKarari.VarsayilanEsik`.
- *"Daha küçük yüzleri de algıla"*: `YuzleriBul` içinde `minKenar` alt sınırını 40'tan 25'e indir.
- *"Veri artırmada ayna kopyasını kapat"*: `Varyantlar` içindeki `Cv2.Flip` bloğunu kaldır.
- *"Bilinmeyen yüzleri kırmızı yerine sarı çiz"*: `Ciz` içinde `new Scalar(40, 40, 230)` → `new Scalar(0, 220, 255)` (BGR sırası!).
