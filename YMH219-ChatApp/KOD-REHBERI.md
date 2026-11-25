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

## Olası küçük değişiklik soruları

- *"Varsayılan portu 6000 yap"*: `SunucuForm` ve `IstemciForm` içinde `nudPort` `Value = 6000`.
- *"Mesaja uzunluk sınırı koy"*: `IstemciForm` → `txtMesaj.MaxLength`; sunucuda `MesajiIsleAsync` içinde `gelen.Metin.Length` kontrolü.
- *"Sunucu kendi mesajını yayınlasın"*: `ChatSunucu` içine `public Task DuyuruAsync(string metin) => HerkeseGonderAsync(ChatMesaji.Sistem(metin));`
