# Kod Rehberi (Sözlü Kontrol İçin)

## Akış

```
İstemci                          Sunucu
BaglanAsync ── TCP connect ────► AcceptTcpClientAsync (KabulDongusuAsync)
            ── {"Tur":"Katil"} ─► KatilmaHatasi? → Hata / Hoş geldin
            ◄── Sistem + KullaniciListesi ──
GonderAsync ── {"Tur":"Sohbet"} ► MesajiIsleAsync → HerkeseGonderAsync
OkumaDongusuAsync ◄──────────── herkese yayın
```

## Önemli noktalar

- **Neden satır tabanlı JSON?** TCP bir bayt akışıdır, mesaj sınırı yoktur. Her mesajı tek satır yazıp
  `ReadLineAsync` ile okuyarak mesajlar birbirinden ayrılır.
- **`JsonSatirBaglantisi`**: `NetworkStream` → `StreamReader` / `StreamWriter(AutoFlush = true)`.
  Yazma `SemaphoreSlim` ile kilitlenir; iki görev aynı anda yazarsa satırlar karışmaz.
- **Sunucu**: her istemci için `IstemciyiYonetAsync` ayrı bir görev olarak çalışır (`_ = ...Async()`).
  İstemciler `ConcurrentDictionary` içinde tutulur çünkü farklı iş parçacıklarından erişilir.
  `finally` bloğu, istemci nasıl ayrılırsa ayrılsın listeden çıkarılmasını ve diğerlerine bildirilmesini garanti eder.
- **İstemci**: `BaglanAsync` sunucunun ilk cevabını bekler; `Hata` gelirse `ChatBaglantiHatasi` fırlatır.
  Sonra `OkumaDongusuAsync` arka planda çalışır ve her mesajda `MesajAlindi` olayını tetikler.
- **Arayüz ve iş parçacıkları**: Core olayları arka plan iş parçacığından gelir. Windows Forms kontrollerine
  yalnızca oluşturuldukları (UI) iş parçacığından erişilebilir; bu yüzden formlarda `BeginInvoke` kullanılır (`UIdaCalistir`).
- **`async void` butonlar**: `BtnBaglan_Click` içindeki `await` sırasında form donmaz; hata `try/catch` ile yakalanıp `MessageBox` ile gösterilir.
- **Kapsülleme**: `BagliIstemci.Baglanti` `internal` → formdan doğrudan sokete erişilemez.

## Yeni eklenen özellikler (kod düzeyinde)

- **TLS — `TlsYardimcisi.cs`, `JsonSatirBaglantisi(tcp, akis)`:** sunucu `CertificateRequest` ile RSA-2048 kendinden imzalı
  sertifika üretir (PFX olarak saklanır). `ChatSunucu.Sertifika` atanmışsa her bağlantıda `SslStream.AuthenticateAsServerAsync`
  çalışır. İstemcide `RemoteCertificateValidationCallback` → `SunucuSertifikasiniDogrula`: zincir geçerliyse kabul; değilse
  SHA-256 parmak izi `BilinenSunucular` (TOFU) dosyasıyla karşılaştırılır — ilk kez görülüyorsa kaydedilir, farklıysa reddedilir.
- **Parola:** `Katil` mesajındaki `Parola`, `CryptographicOperations.FixedTimeEquals(SHA256(a), SHA256(b))` ile karşılaştırılır;
  yanlışsa 1 sn beklenip `Hata` gönderilir. Sunucu, ilettiği mesajlarda `Parola = null` yapar.
- **Odalar:** `BagliIstemci.Oda`; `OdayaGonderAsync(oda, mesaj)` yalnızca o odadakilere yazar. `OdaDegistir` mesajı:
  eski odaya "ayrıldı", yeni odaya "katıldı", istemciye o odanın geçmişi ve herkese güncel `KullaniciListesi` (+ `Odalar`).
- **Geçmiş — `MesajGecmisi.cs`:** `LinkedList` ile son 100 mesaj; her mesaj `File.AppendAllText` ile JSON Lines dosyasına eklenir.
  Yeni katılana `SonMesajlar(50, oda)` → `Gecmis = true` işaretli gönderilir; özel mesajlar geçmişe girmez.
- **Otomatik yeniden bağlanma — `ChatIstemci.YenidenBaglanAsync`:** beklenmedik kopuşta (bilerek kesilmediyse) 1→2→4→…10 sn
  beklemeli 10 deneme; `CancellationTokenSource` ile "Bağlantıyı Kes" döngüyü durdurur. Son bağlantı bilgileri (adres, ad,
  parola, TLS, oda) saklanır.

## Olası küçük değişiklik soruları

- *"Varsayılan portu 6000 yap"*: `SunucuForm` ve `IstemciForm` içinde `nudPort` `Value = 6000`.
- *"Mesaja uzunluk sınırı koy"*: `IstemciForm` → `txtMesaj.MaxLength`; sunucuda `MesajiIsleAsync` içinde `gelen.Metin.Length` kontrolü.
- *"Sunucu kendi mesajını yayınlasın"*: `ChatSunucu` içine `public Task DuyuruAsync(string metin) => HerkeseGonderAsync(ChatMesaji.Sistem(metin));`
