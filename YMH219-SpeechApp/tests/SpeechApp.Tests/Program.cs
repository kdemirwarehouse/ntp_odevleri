using SpeechApp.Core;

// Konsol test çalıştırıcısı. Çalıştırma: dotnet run --project tests/SpeechApp.Tests

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

Console.WriteLine("SpeechApp testleri");

Test("Dil seçici: tam eşleşmeyi tercih eder", () =>
    Dogrula(DilSecici.EnUygun(new[] { "en-US", "tr-TR" }, "tr-TR") == "tr-TR", "tr-TR beklenirdi"));

Test("Dil seçici: aynı dilin farklı bölgesini seçer", () =>
    Dogrula(DilSecici.EnUygun(new[] { "en-US", "en-GB" }, "en-AU") == "en-US", "en-US beklenirdi"));

Test("Dil seçici: Türkçe yoksa en-US'e düşer", () =>
    Dogrula(DilSecici.EnUygun(new[] { "de-DE", "en-US" }, "tr-TR") == "en-US", "en-US beklenirdi"));

Test("Dil seçici: en-US de yoksa ilk kurulu olanı seçer", () =>
    Dogrula(DilSecici.EnUygun(new[] { "de-DE", "fr-FR" }, "tr-TR") == "de-DE", "de-DE beklenirdi"));

Test("Dil seçici: hiç tanıyıcı yoksa null", () =>
    Dogrula(DilSecici.EnUygun(Array.Empty<string>(), "tr-TR") is null, "null beklenirdi"));

Test("Görünen ad kültür kodunu içerir", () =>
    Dogrula(DilSecici.GorunenAd("tr-TR").Contains("tr-TR"), "Kod görünmeli"));

Test("Geçmiş: düşük güvenli ve boş sonuçlar eklenmez", () =>
{
    var g = new TanimaGecmisi(minimumGuven: 0.5f);
    Dogrula(g.Ekle("hello world", 0.9f), "Yüksek güven eklenmeli");
    Dogrula(!g.Ekle("noise", 0.2f), "Düşük güven eklenmemeli");
    Dogrula(!g.Ekle("   ", 0.99f), "Boş metin eklenmemeli");
    Dogrula(g.Kayitlar.Count == 1, "Bir kayıt olmalı");
});

Test("Geçmiş: birleşik metin ve ortalama güven", () =>
{
    var g = new TanimaGecmisi();
    g.Ekle("good morning", 0.8f);
    g.Ekle("how are you", 0.6f);
    Dogrula(g.BirlesikMetin() == "good morning how are you", "Cümleler birleşmeli");
    Dogrula(Math.Abs(g.OrtalamaGuven - 0.7) < 0.001, "Ortalama 0.7 olmalı");
});

Test("Güven yüzdesi 0-100 aralığına sınırlanır", () =>
{
    Dogrula(new TanimaKaydi(DateTime.Now, "a", 0.876f).GuvenYuzde == 88, "88 beklenirdi");
    Dogrula(new TanimaKaydi(DateTime.Now, "a", 1.5f).GuvenYuzde == 100, "100 beklenirdi");
});

Test("Geçmiş .txt dosyasına zaman damgasıyla yazılır", () =>
{
    var g = new TanimaGecmisi();
    g.Ekle("merhaba dünya", 0.9f, new DateTime(2026, 11, 25, 14, 30, 5));
    string yol = Path.Combine(Path.GetTempPath(), $"speech-test-{Guid.NewGuid():N}.txt");
    try
    {
        g.TxtOlarakKaydet(yol);
        string icerik = File.ReadAllText(yol);
        Dogrula(icerik.Contains("[14:30:05] (90%) merhaba dünya"), "Satır biçimi hatalı");
    }
    finally { File.Delete(yol); }
});

Test("Durum açıklamaları boş değildir", () =>
{
    foreach (SesDurumu d in Enum.GetValues<SesDurumu>())
        Dogrula(!string.IsNullOrWhiteSpace(d.Aciklama()), $"{d} için açıklama yok");
});

Console.WriteLine();
Console.WriteLine($"Sonuç: {basarili} başarılı, {basarisiz} başarısız");
return basarisiz == 0 ? 0 : 1;
