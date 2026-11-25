using KeyEventLogger.Core;

// Harici test çatısı gerektirmeyen konsol test çalıştırıcısı.
// Çalıştırma: dotnet run --project tests/KeyEventLogger.Tests

int basarili = 0, basarisiz = 0;

void Test(string ad, Action govde)
{
    try { govde(); basarili++; Console.WriteLine($"  [OK]   {ad}"); }
    catch (Exception ex) { basarisiz++; Console.WriteLine($"  [HATA] {ad}: {ex.Message}"); }
}

void Dogrula(bool kosul, string mesaj)
{
    if (!kosul) throw new Exception(mesaj);
}

// Testlerde zamanı kontrol edebilmek için sahte saat
DateTime simdi = new(2026, 11, 25, 10, 0, 0);
DateTime SahteSaat() { simdi = simdi.AddSeconds(1); return simdi; }

Console.WriteLine("KeyEventLogger testleri");

Test("Oturum kapalı başlar ve olay kabul etmez", () =>
{
    var o = new KayitOturumu();
    Dogrula(!o.KayitAcik, "Kayıt kapalı başlamalı");
    Dogrula(!o.Ekle(OlayTuru.KeyDown, "A"), "Kapalıyken olay eklenmemeli");
    Dogrula(o.Kayitlar.Count == 0, "Liste boş kalmalı");
});

Test("Başlat / Durdur durum değiştirir ve olay tetikler", () =>
{
    var o = new KayitOturumu();
    int olay = 0;
    o.DurumDegisti += (_, _) => olay++;
    o.Baslat();
    Dogrula(o.KayitAcik && o.BaslamaZamani is not null, "Kayıt açılmalı");
    o.Baslat(); // ikinci çağrı etkisiz
    o.Durdur();
    Dogrula(!o.KayitAcik, "Kayıt kapanmalı");
    Dogrula(olay == 2, $"2 durum olayı beklenirdi, {olay} geldi");
});

Test("Üç olay türü de kaydedilir", () =>
{
    var o = new KayitOturumu();
    o.Baslat();
    o.Ekle(OlayTuru.KeyDown, "A", "Shift");
    o.Ekle(OlayTuru.KeyPress, "A");
    o.Ekle(OlayTuru.KeyUp, "A");
    Dogrula(o.Kayitlar.Select(k => k.Tur).SequenceEqual(new[] { OlayTuru.KeyDown, OlayTuru.KeyPress, OlayTuru.KeyUp }),
        "Olay sırası korunmalı");
    Dogrula(o.ToplamTusBasimi == 1, "Tuş basımı yalnızca KeyDown sayılmalı");
});

Test("Temizle kayıtları siler ama kayıt durumunu korur", () =>
{
    var o = new KayitOturumu();
    o.Baslat();
    o.Ekle(OlayTuru.KeyDown, "B");
    o.Temizle();
    Dogrula(o.Kayitlar.Count == 0, "Liste boşalmalı");
    Dogrula(o.KayitAcik, "Kayıt açık kalmalı");
});

Test("Sınır dolunca kayıt otomatik durur", () =>
{
    var o = new KayitOturumu(sinir: 3);
    o.Baslat();
    for (int i = 0; i < 5; i++) o.Ekle(OlayTuru.KeyDown, "X");
    Dogrula(o.Kayitlar.Count == 3, "En fazla 3 kayıt olmalı");
    Dogrula(!o.KayitAcik, "Kayıt durmalı");
    bool hata = false;
    try { o.Baslat(); } catch (InvalidOperationException) { hata = true; }
    Dogrula(hata, "Sınır doluyken Başlat hata vermeli");
    o.Temizle();
    o.Baslat();
    Dogrula(o.KayitAcik, "Temizledikten sonra yeniden başlayabilmeli");
});

Test("En sık tuşlar doğru sıralanır", () =>
{
    var o = new KayitOturumu();
    o.Baslat();
    foreach (var t in new[] { "A", "B", "A", "C", "A", "B" })
        o.Ekle(OlayTuru.KeyDown, t);
    o.Ekle(OlayTuru.KeyUp, "C"); // KeyUp istatistiğe girmez
    var enSik = o.EnSikTuslar(2);
    Dogrula(enSik.Count == 2, "2 sonuç beklenirdi");
    Dogrula(enSik[0] == ("A", 3) && enSik[1] == ("B", 2), $"Beklenmeyen sıralama: {string.Join(",", enSik)}");
});

Test("Zaman aralığına göre filtreleme", () =>
{
    simdi = new DateTime(2026, 11, 25, 10, 0, 0);
    var o = new KayitOturumu(saat: SahteSaat);
    o.Baslat();                                   // 10:00:01
    o.Ekle(OlayTuru.KeyDown, "1");                // 10:00:02
    o.Ekle(OlayTuru.KeyDown, "2");                // 10:00:03
    o.Ekle(OlayTuru.KeyDown, "3");                // 10:00:04
    var sonuc = o.Filtrele(new DateTime(2026, 11, 25, 10, 0, 3), new DateTime(2026, 11, 25, 10, 0, 4));
    Dogrula(sonuc.Count == 2 && sonuc[0].Tus == "2", "Aralıktaki 2 kayıt beklenirdi");
});

Test("Kontrol karakterleri okunabilir ada çevrilir", () =>
{
    Dogrula(KeyLogEntry.KarakterAdi('\r') == "[Enter]", "Enter");
    Dogrula(KeyLogEntry.KarakterAdi('\b') == "[Backspace]", "Backspace");
    Dogrula(KeyLogEntry.KarakterAdi((char)3) == "[Ctrl+C]", "Ctrl+C");
    Dogrula(KeyLogEntry.KarakterAdi('ş') == "ş", "Türkçe karakter aynen kalmalı");
});

Test("TXT dosyasına UTF-8 olarak yazılır", () =>
{
    var o = new KayitOturumu();
    o.Baslat();
    o.Ekle(OlayTuru.KeyPress, "ğ");
    o.Ekle(OlayTuru.KeyDown, "Enter", "KeyCode=13");
    string yol = Path.Combine(Path.GetTempPath(), $"kel-test-{Guid.NewGuid():N}.txt");
    try
    {
        KayitDosyasi.Kaydet(o.Kayitlar, yol);
        var satirlar = File.ReadAllLines(yol);
        Dogrula(satirlar.Length == 4 + 2, $"4 başlık + 2 kayıt satırı beklenirdi, {satirlar.Length} geldi");
        Dogrula(satirlar[4].Contains("KeyPress") && satirlar[4].Contains("ğ"), "Türkçe karakter korunmalı");
    }
    finally { File.Delete(yol); }
});

Test("Geçersiz klasöre yazma hatası yakalanabilir", () =>
{
    bool hata = false;
    try { KayitDosyasi.Kaydet(Array.Empty<KeyLogEntry>(), Path.Combine("Z:\\olmayan\\klasor", "a.txt")); }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { hata = true; }
    Dogrula(hata, "IOException beklenirdi");
});

Console.WriteLine();
Console.WriteLine($"Sonuç: {basarili} başarılı, {basarisiz} başarısız");
return basarisiz == 0 ? 0 : 1;
