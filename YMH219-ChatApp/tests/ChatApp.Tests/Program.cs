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
