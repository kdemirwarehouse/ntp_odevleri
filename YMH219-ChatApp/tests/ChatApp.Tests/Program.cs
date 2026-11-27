using System.Collections.Concurrent;
using System.Net;
using ChatApp.Core;

// Gerçek TCP bağlantılarıyla (127.0.0.1) çalışan konsol test çalıştırıcısı.
// Çalıştırma: dotnet run --project tests/ChatApp.Tests

int basarili = 0, basarisiz = 0;

async Task Test(string ad, Func<Task> govde)
{
    try
    {
        var gorev = govde();
        if (await Task.WhenAny(gorev, Task.Delay(TimeSpan.FromSeconds(15))) != gorev)
            throw new TimeoutException("Test 15 saniyede bitmedi.");
        await gorev;
        basarili++;
        Console.WriteLine($"  [OK]   {ad}");
    }
    catch (Exception ex)
    {
        basarisiz++;
        Console.WriteLine($"  [HATA] {ad}: {ex.GetType().Name}: {ex.Message}");
    }
}

void Dogrula(bool kosul, string mesaj)
{
    if (!kosul) throw new Exception(mesaj);
}

// Gelen mesajları biriktirip belirli bir mesajı bekleyen yardımcı
static async Task<ChatMesaji> Bekle(BlockingCollection<ChatMesaji> kutu, Func<ChatMesaji, bool> kosul, int ms = 3000)
{
    var sure = DateTime.Now.AddMilliseconds(ms);
    while (DateTime.Now < sure)
    {
        if (kutu.TryTake(out var m, 50) && kosul(m))
            return m;
        await Task.Yield();
    }
    throw new TimeoutException("Beklenen mesaj gelmedi.");
}

static (ChatIstemci, BlockingCollection<ChatMesaji>) YeniIstemci()
{
    var istemci = new ChatIstemci();
    var kutu = new BlockingCollection<ChatMesaji>();
    istemci.MesajAlindi += (_, m) => kutu.Add(m);
    return (istemci, kutu);
}

ChatSunucu YeniSunucu()
{
    var s = new ChatSunucu();
    s.Baslat(0, IPAddress.Loopback); // port 0 => boş port
    return s;
}

Console.WriteLine("ChatApp testleri");

await Test("Mesaj JSON satırına çevrilip geri okunur (Türkçe karakterlerle)", () =>
{
    var m = new ChatMesaji { Tur = MesajTuru.Ozel, Gonderen = "Ayşe", Alici = "Çağrı", Metin = "Günaydın! \"tırnak\"" };
    string satir = m.JsonSatiri();
    Dogrula(!satir.Contains('\n'), "Tek satır olmalı");
    var geri = ChatMesaji.JsondanOku(satir);
    Dogrula(geri.Tur == MesajTuru.Ozel && geri.Alici == "Çağrı" && geri.Metin == m.Metin, "Alanlar korunmalı");
    return Task.CompletedTask;
});

await Test("Kullanıcı adı doğrulaması", () =>
{
    Dogrula(ChatSunucu.KatilmaHatasi(new ChatMesaji { Tur = MesajTuru.Katil, Gonderen = "Ali" }) is null, "Ali geçerli");
    Dogrula(ChatSunucu.KatilmaHatasi(new ChatMesaji { Tur = MesajTuru.Katil, Gonderen = "  " }) is not null, "Boş ad geçersiz");
    Dogrula(ChatSunucu.KatilmaHatasi(new ChatMesaji { Tur = MesajTuru.Katil, Gonderen = new string('x', 30) }) is not null, "Uzun ad geçersiz");
    Dogrula(ChatSunucu.KatilmaHatasi(new ChatMesaji { Tur = MesajTuru.Sohbet, Gonderen = "Ali" }) is not null, "İlk mesaj Katil olmalı");
    return Task.CompletedTask;
});

await Test("İstemci bağlanır, sunucu listesinde görünür", async () =>
{
    using var sunucu = YeniSunucu();
    var (ali, kutu) = YeniIstemci();
    using var _ = ali;
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali");
    Dogrula(ali.Bagli, "Bağlı olmalı");
    await Bekle(kutu, m => m.Tur == MesajTuru.KullaniciListesi && m.Kullanicilar!.Contains("Ali"));
    Dogrula(sunucu.Istemciler.Any(i => i.KullaniciAdi == "Ali" && i.UzakAdres.StartsWith("127.0.0.1")), "Sunucu Ali'yi listelemeli");
});

await Test("Sohbet mesajı tüm istemcilere zaman bilgisiyle ulaşır", async () =>
{
    using var sunucu = YeniSunucu();
    var (ali, aliKutu) = YeniIstemci();
    var (veli, veliKutu) = YeniIstemci();
    using var a = ali; using var v = veli;
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali");
    await veli.BaglanAsync("127.0.0.1", sunucu.Port, "Veli");
    await ali.GonderAsync("Merhaba Veli");
    var veliGelen = await Bekle(veliKutu, m => m.Tur == MesajTuru.Sohbet);
    var aliGelen = await Bekle(aliKutu, m => m.Tur == MesajTuru.Sohbet);
    Dogrula(veliGelen.Gonderen == "Ali" && veliGelen.Metin == "Merhaba Veli", "Veli mesajı almalı");
    Dogrula(aliGelen.Metin == "Merhaba Veli", "Gönderen de kendi mesajını görmeli");
    Dogrula((DateTime.Now - veliGelen.Zaman).TotalSeconds < 5, "Zaman bilgisi olmalı");
});

await Test("Özel mesaj yalnızca alıcıya (ve gönderene) gider", async () =>
{
    using var sunucu = YeniSunucu();
    var (ali, aliKutu) = YeniIstemci();
    var (veli, veliKutu) = YeniIstemci();
    var (can, canKutu) = YeniIstemci();
    using var a = ali; using var v = veli; using var c = can;
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali");
    await veli.BaglanAsync("127.0.0.1", sunucu.Port, "Veli");
    await can.BaglanAsync("127.0.0.1", sunucu.Port, "Can");
    await ali.OzelGonderAsync("Veli", "gizli");
    var gelen = await Bekle(veliKutu, m => m.Tur == MesajTuru.Ozel);
    Dogrula(gelen.Gonderen == "Ali" && gelen.Alici == "Veli", "Veli özel mesajı almalı");
    await Bekle(aliKutu, m => m.Tur == MesajTuru.Ozel);
    await Task.Delay(300);
    Dogrula(!canKutu.Any(m => m.Tur == MesajTuru.Ozel), "Can özel mesajı görmemeli");
});

await Test("Aynı kullanıcı adı reddedilir", async () =>
{
    using var sunucu = YeniSunucu();
    var (ali1, _) = YeniIstemci();
    var (ali2, _) = YeniIstemci();
    using var a1 = ali1; using var a2 = ali2;
    await ali1.BaglanAsync("127.0.0.1", sunucu.Port, "Ali");
    bool reddedildi = false;
    try { await ali2.BaglanAsync("127.0.0.1", sunucu.Port, "ali"); }
    catch (ChatBaglantiHatasi ex) { reddedildi = ex.Message.Contains("kullanılıyor"); }
    Dogrula(reddedildi, "İkinci 'ali' reddedilmeli");
    Dogrula(!ali2.Bagli, "Reddedilen istemci bağlı görünmemeli");
});

await Test("Bağlantıyı kesen istemci diğerlerine bildirilir", async () =>
{
    using var sunucu = YeniSunucu();
    var ayrilanlar = new ConcurrentBag<string>();
    sunucu.IstemciAyrildi += (_, i) => ayrilanlar.Add(i.KullaniciAdi);
    var (ali, aliKutu) = YeniIstemci();
    var (veli, _) = YeniIstemci();
    using var a = ali;
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali");
    await veli.BaglanAsync("127.0.0.1", sunucu.Port, "Veli");
    veli.BaglantiyiKes();
    await Bekle(aliKutu, m => m.Tur == MesajTuru.Sistem && m.Metin.Contains("Veli") && m.Metin.Contains("ayrıldı"));
    Dogrula(ayrilanlar.Contains("Veli"), "Sunucu ayrılma olayı tetiklemeli");
    Dogrula(!veli.Bagli, "Veli bağlı görünmemeli");
});

await Test("Çalışmayan sunucuya bağlanma anlaşılır hata verir", async () =>
{
    int bosPort;
    using (var gecici = new ChatSunucu()) { gecici.Baslat(0, IPAddress.Loopback); bosPort = gecici.Port; }
    var (ali, _) = YeniIstemci();
    bool hata = false;
    try { await ali.BaglanAsync("127.0.0.1", bosPort, "Ali"); }
    catch (ChatBaglantiHatasi) { hata = true; }
    Dogrula(hata, "ChatBaglantiHatasi beklenirdi");
});

await Test("Sunucu durunca istemciye BaglantiKoptu bildirilir", async () =>
{
    var sunucu = YeniSunucu();
    var (ali, _) = YeniIstemci();
    var koptu = new TaskCompletionSource<string>();
    ali.BaglantiKoptu += (_, sebep) => koptu.TrySetResult(sebep);
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali");
    sunucu.Durdur();
    var sonuc = await Task.WhenAny(koptu.Task, Task.Delay(3000));
    Dogrula(sonuc == koptu.Task, "BaglantiKoptu olayı gelmeli");
    Dogrula(!ali.Bagli, "İstemci bağlı görünmemeli");
});

await Test("Sunucu yeniden başlayınca istemci otomatik yeniden bağlanır", async () =>
{
    var sunucu = YeniSunucu();
    int port = sunucu.Port;
    var (ali, _) = YeniIstemci();
    ali.OtomatikYenidenBaglan = true;
    ali.IlkBekleme = TimeSpan.FromMilliseconds(200);
    var denendi = new TaskCompletionSource();
    var baglandi = new TaskCompletionSource();
    ali.YenidenBaglaniyor += (_, _) => denendi.TrySetResult();
    ali.YenidenBaglandi += (_, _) => baglandi.TrySetResult();
    await ali.BaglanAsync("127.0.0.1", port, "Ali");

    sunucu.Durdur();
    Dogrula(await Task.WhenAny(denendi.Task, Task.Delay(3000)) == denendi.Task, "Yeniden bağlanma denemesi başlamalı");
    var yeni = new ChatSunucu();
    yeni.Baslat(port, IPAddress.Loopback);
    Dogrula(await Task.WhenAny(baglandi.Task, Task.Delay(8000)) == baglandi.Task, "Yeniden bağlanılmalı");
    Dogrula(ali.Bagli && yeni.Istemciler.Any(i => i.KullaniciAdi == "Ali"), "Ali yeni sunucuda görünmeli");
    ali.Dispose();
    yeni.Durdur();
});

await Test("Bağlantıyı Kes, devam eden yeniden bağlanmayı durdurur", async () =>
{
    var sunucu = YeniSunucu();
    var (ali, _) = YeniIstemci();
    ali.OtomatikYenidenBaglan = true;
    ali.IlkBekleme = TimeSpan.FromMilliseconds(300);
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali");
    sunucu.Durdur();
    await Task.Delay(150);
    Dogrula(ali.YenidenBaglaniyorMu, "Yeniden bağlanma sürüyor olmalı");
    ali.BaglantiyiKes();
    await Task.Delay(100);
    Dogrula(!ali.YenidenBaglaniyorMu && !ali.Bagli, "Döngü durmalı");
});

await Test("Yeni katılan son mesajları geçmiş olarak alır; özel mesajlar geçmişe girmez", async () =>
{
    var sunucu = YeniSunucu();
    var (ali, aliKutu) = YeniIstemci();
    var (ayse, _) = YeniIstemci();
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali");
    await ayse.BaglanAsync("127.0.0.1", sunucu.Port, "Ayse");
    await ali.GonderAsync("ilk mesaj");
    await ali.OzelGonderAsync("Ayse", "gizli");
    await Bekle(aliKutu, m => m.Metin == "gizli");

    var (veli, veliKutu) = YeniIstemci();
    await veli.BaglanAsync("127.0.0.1", sunucu.Port, "Veli");
    var eski = await Bekle(veliKutu, m => m.Gecmis == true && m.Metin == "ilk mesaj");
    Dogrula(eski.Gonderen == "Ali", "Geçmiş mesajın göndereni korunmalı");
    Dogrula(sunucu.Gecmis.SonMesajlar().All(m => m.Tur != MesajTuru.Ozel), "Özel mesaj geçmişte olmamalı");
    sunucu.Durdur();
});

await Test("Geçmiş dosyaya yazılır ve yeni sunucuda yüklenir; kapasite aşılınca eskiler düşer", () =>
{
    string dosya = Path.Combine(Path.GetTempPath(), $"chat-gecmis-{Guid.NewGuid():N}.jsonl");
    var g1 = new MesajGecmisi(dosya, kapasite: 3);
    for (int i = 1; i <= 5; i++)
        g1.Ekle(new ChatMesaji { Tur = MesajTuru.Sohbet, Gonderen = "Ali", Metin = $"m{i}" });
    g1.Ekle(new ChatMesaji { Tur = MesajTuru.Ozel, Gonderen = "Ali", Alici = "Ayse", Metin = "gizli" });
    var g2 = new MesajGecmisi(dosya, kapasite: 3);
    var metinler = g2.SonMesajlar().Select(m => m.Metin).ToList();
    Dogrula(metinler.SequenceEqual(new[] { "m3", "m4", "m5" }), $"Son 3 mesaj beklenirdi: {string.Join(",", metinler)}");
    g2.Temizle();
    Dogrula(!File.Exists(dosya) && g2.Sayi == 0, "Temizle dosyayı ve belleği silmeli");
    return Task.CompletedTask;
});

await Test("Parolalı sunucu: doğru parola kabul, yanlış/eksik parola reddedilir", async () =>
{
    var sunucu = new ChatSunucu { Parola = "gizli123", YanlisParolaBeklemesi = TimeSpan.Zero };
    sunucu.Baslat(0, IPAddress.Loopback);
    var (ali, _) = YeniIstemci();
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali", parola: "gizli123");
    Dogrula(ali.Bagli, "Doğru parola ile bağlanılmalı");

    foreach (var yanlis in new[] { "yanlis", null })
    {
        var (veli, _) = YeniIstemci();
        string? hata = null;
        try { await veli.BaglanAsync("127.0.0.1", sunucu.Port, "Veli", parola: yanlis); }
        catch (ChatBaglantiHatasi ex) { hata = ex.Message; }
        Dogrula(hata == ChatSunucu.YanlisParolaMetni, $"Parola hatası beklenirdi, gelen: {hata}");
        Dogrula(!sunucu.Istemciler.Any(i => i.KullaniciAdi == "Veli"), "Veli listede olmamalı");
    }
    Dogrula(ChatSunucu.ParolalarEsit("abc", "abc") && !ChatSunucu.ParolalarEsit("abc", "abd"), "Sabit zamanlı karşılaştırma doğru çalışmalı");
    sunucu.Durdur();
});

await Test("Odalar: mesajlar yalnızca aynı odaya gider, oda değiştirme ve liste", async () =>
{
    var sunucu = YeniSunucu();
    var (ali, aliKutu) = YeniIstemci();
    var (ayse, ayseKutu) = YeniIstemci();
    var (veli, veliKutu) = YeniIstemci();
    await ali.BaglanAsync("127.0.0.1", sunucu.Port, "Ali", oda: "Oyun");
    await ayse.BaglanAsync("127.0.0.1", sunucu.Port, "Ayse", oda: "Oyun");
    await veli.BaglanAsync("127.0.0.1", sunucu.Port, "Veli");
    Dogrula(ali.Oda == "Oyun" && veli.Oda == "Genel", "Odalar doğru atanmalı");

    await ali.GonderAsync("oyun odası mesajı");
    await Bekle(ayseKutu, m => m.Metin == "oyun odası mesajı" && m.Oda == "Oyun");
    await veli.GonderAsync("genel mesaj");
    await Bekle(veliKutu, m => m.Metin == "genel mesaj");
    Dogrula(!veliKutu.Any(m => m.Metin == "oyun odası mesajı"), "Veli Oyun odasının mesajını almamalı");

    await veli.OdaDegistirAsync("Oyun");
    await Bekle(veliKutu, m => m.Gecmis == true && m.Metin == "oyun odası mesajı");   // önce odanın geçmişi gelir
    var liste = await Bekle(veliKutu, m => m.Tur == MesajTuru.KullaniciListesi && m.Oda == "Oyun");
    Dogrula(liste.Kullanicilar!.SequenceEqual(new[] { "Ali", "Ayse", "Veli" }), "Oyun odasında 3 kişi olmalı");
    Dogrula(ChatSunucu.OdaAdiHatasi("") is not null && ChatSunucu.OdaAdiHatasi("a/b") is not null, "Geçersiz oda adı reddedilmeli");
    sunucu.Durdur();
});

await Test("TLS: şifreli bağlantı, TOFU parmak izi kaydı ve değişen sertifikanın reddi", async () =>
{
    string klasor = Path.Combine(Path.GetTempPath(), $"chat-tls-{Guid.NewGuid():N}");
    var bilinen = new BilinenSunucular(Path.Combine(klasor, "bilinen.txt"));
    var sunucu = new ChatSunucu { Sertifika = TlsYardimcisi.SertifikaYukleVeyaOlustur(Path.Combine(klasor, "a.pfx")) };
    sunucu.Baslat(0, IPAddress.Loopback);
    int port = sunucu.Port;

    var (ali, aliKutu) = YeniIstemci();
    ali.BilinenSunucular = bilinen;
    await ali.BaglanAsync("127.0.0.1", port, "Ali", tls: true);
    Dogrula(ali.Sifreli && ali.YeniSunucuKaydedildi, "İlk TLS bağlantısında parmak izi kaydedilmeli");
    await ali.GonderAsync("şifreli merhaba");
    await Bekle(aliKutu, m => m.Metin == "şifreli merhaba");
    ali.BaglantiyiKes();

    var (ali2, _) = YeniIstemci();
    ali2.BilinenSunucular = bilinen;
    await ali2.BaglanAsync("127.0.0.1", port, "Ali", tls: true);
    Dogrula(ali2.Sifreli && !ali2.YeniSunucuKaydedildi, "İkinci bağlantıda parmak izi doğrulanmalı");
    ali2.BaglantiyiKes();

    // TLS'siz istemci TLS sunucusuna bağlanamaz
    var (duz, _) = YeniIstemci();
    bool duzHata = false;
    try { await duz.BaglanAsync("127.0.0.1", port, "Duz", zamanAsimi: TimeSpan.FromSeconds(3)); } catch (ChatBaglantiHatasi) { duzHata = true; }
    Dogrula(duzHata, "Şifresiz istemci reddedilmeli");
    sunucu.Durdur();

    // Aynı adreste farklı sertifikalı sunucu: parmak izi uyuşmazlığı
    var sahte = new ChatSunucu { Sertifika = TlsYardimcisi.SertifikaYukleVeyaOlustur(Path.Combine(klasor, "b.pfx")) };
    sahte.Baslat(port, IPAddress.Loopback);
    var (ali3, _) = YeniIstemci();
    ali3.BilinenSunucular = bilinen;
    string? hata = null;
    try { await ali3.BaglanAsync("127.0.0.1", port, "Ali", tls: true); } catch (ChatBaglantiHatasi ex) { hata = ex.Message; }
    Dogrula(hata is not null && hata.Contains("GÜVENLİK UYARISI"), $"Parmak izi uyuşmazlığı beklenirdi: {hata}");
    sahte.Durdur();
    try { Directory.Delete(klasor, true); } catch (IOException) { }
});

await Test("Bağlı değilken mesaj göndermek hata verir", async () =>
{
    var (ali, _) = YeniIstemci();
    bool hata = false;
    try { await ali.GonderAsync("x"); } catch (ChatBaglantiHatasi) { hata = true; }
    Dogrula(hata, "ChatBaglantiHatasi beklenirdi");
});

Console.WriteLine();
Console.WriteLine($"Sonuç: {basarili} başarılı, {basarisiz} başarısız");
return basarisiz == 0 ? 0 : 1;
