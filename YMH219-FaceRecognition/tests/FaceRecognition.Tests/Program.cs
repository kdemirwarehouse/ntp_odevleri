using FaceRecognition.Core;

// Konsol test çalıştırıcısı. Çalıştırma: dotnet run --project tests/FaceRecognition.Tests

int basarili = 0, basarisiz = 0;
string kok = Path.Combine(Path.GetTempPath(), "FaceRecTests-" + Guid.NewGuid().ToString("N"));

void Test(string ad, Action govde)
{
    try { govde(); basarili++; Console.WriteLine($"  [OK]   {ad}"); }
    catch (Exception ex) { basarisiz++; Console.WriteLine($"  [HATA] {ad}: {ex.Message}"); }
}

void Dogrula(bool kosul, string mesaj)
{
    if (!kosul) throw new Exception(mesaj);
}

Console.WriteLine("FaceRecognition testleri");

Test("Geçerli etiketler kabul edilir", () =>
{
    Dogrula(EtiketKurallari.Hata("Ali") is null, "Ali");
    Dogrula(EtiketKurallari.Hata("Ayşe Çelik") is null, "Türkçe ve boşluk");
    Dogrula(EtiketKurallari.Normallestir("  Can  ") == "Can", "Kırpılmalı");
});

Test("Geçersiz etiketler reddedilir", () =>
{
    Dogrula(EtiketKurallari.Hata("") is not null, "boş");
    Dogrula(EtiketKurallari.Hata("a/b") is not null, "eğik çizgi");
    Dogrula(EtiketKurallari.Hata("..") is not null, "nokta");
    Dogrula(EtiketKurallari.Hata(new string('x', 31)) is not null, "uzun");
    Dogrula(EtiketKurallari.Hata("bilinmiyor") is not null, "ayrılmış ad");
});

Test("Eşik altındaki mesafe tanınır, güven hesaplanır", () =>
{
    var k = TanimaKarari.Olustur("Ali", 35, esik: 80);
    Dogrula(k.Taninan && k.Etiket == "Ali", "Ali tanınmalı");
    Dogrula(k.GuvenYuzde == 65, $"Güven 65 olmalı, {k.GuvenYuzde} geldi");
});

Test("Eşik üstündeki mesafe 'Bilinmiyor' olur", () =>
{
    var k = TanimaKarari.Olustur("Ali", 95, esik: 80);
    Dogrula(!k.Taninan && k.Etiket == TanimaKarari.Bilinmiyor, "Bilinmiyor beklenirdi");
    Dogrula(k.GuvenYuzde == 5, "Güven 5 olmalı");
});

Test("Güven 0-100 aralığında kalır, geçersiz mesafe bilinmiyor sayılır", () =>
{
    Dogrula(TanimaKarari.Olustur("Ali", 250).GuvenYuzde == 0, "0'a sınırlanmalı");
    Dogrula(TanimaKarari.Olustur("Ali", 0).GuvenYuzde == 100, "100 olmalı");
    Dogrula(!TanimaKarari.Olustur("Ali", double.NaN).Taninan, "NaN tanınmamalı");
    Dogrula(!TanimaKarari.Olustur(null, 10).Taninan, "Etiket yoksa tanınmamalı");
});

Test("Örnek deposu: boş klasörde etiket yok, eğitim hatası verir", () =>
{
    var depo = new OrnekDeposu(Path.Combine(kok, "bos"));
    Dogrula(depo.Etiketler().Count == 0, "Etiket olmamalı");
    Dogrula(depo.EgitimHatasi() is not null, "Eğitim hatası beklenirdi");
});

Test("Örnek deposu: dosya yolları sırayla üretilir", () =>
{
    var depo = new OrnekDeposu(Path.Combine(kok, "sira"));
    string y1 = depo.YeniOrnekYolu("Ali");
    File.WriteAllBytes(y1, new byte[] { 1 });
    string y2 = depo.YeniOrnekYolu("Ali");
    Dogrula(Path.GetFileName(y1) == "001.png" && Path.GetFileName(y2) == "002.png", $"{y1} / {y2}");
});

Test("Örnek deposu: etiketler alfabetik ve sayısal Id ile listelenir", () =>
{
    var depo = new OrnekDeposu(Path.Combine(kok, "liste"));
    foreach (var (ad, adet) in new[] { ("Zeynep", 2), ("Ali", 3) })
        for (int i = 0; i < adet; i++)
            File.WriteAllBytes(depo.YeniOrnekYolu(ad), new byte[] { 1 });
    File.WriteAllText(Path.Combine(kok, "liste", "Ali", "not.txt"), "resim değil");

    var etiketler = depo.Etiketler();
    Dogrula(etiketler.Count == 2, "2 etiket olmalı");
    Dogrula(etiketler[0].Etiket == "Ali" && etiketler[0].Id == 0 && etiketler[0].OrnekSayisi == 3, "Ali/0/3");
    Dogrula(etiketler[1].Etiket == "Zeynep" && etiketler[1].Id == 1 && etiketler[1].OrnekSayisi == 2, "Zeynep/1/2");
    Dogrula(depo.EgitimHatasi() is null, "Eğitim için yeterli olmalı");
});

Test("Örnek deposu: etiket silinir", () =>
{
    var depo = new OrnekDeposu(Path.Combine(kok, "sil"));
    File.WriteAllBytes(depo.YeniOrnekYolu("Can"), new byte[] { 1 });
    Dogrula(depo.EtiketSil("Can"), "Silinmeli");
    Dogrula(!depo.EtiketSil("Can"), "İkinci silme false dönmeli");
    Dogrula(depo.Etiketler().Count == 0, "Liste boş olmalı");
});

try { Directory.Delete(kok, true); } catch { /* önemsiz */ }

Console.WriteLine();
Console.WriteLine($"Sonuç: {basarili} başarılı, {basarisiz} başarısız");
return basarisiz == 0 ? 0 : 1;
