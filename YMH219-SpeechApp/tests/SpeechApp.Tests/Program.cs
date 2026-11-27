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

Test("Gürültü kapısı: eşik altı blok sessizleşir, konuşma sonrası kuyrukta açık kalır", () =>
{
    var kapi = new GurultuKapisi { Esik = 20, KuyrukBlokSayisi = 2 };
    byte[] Blok() => new byte[] { 1, 2, 3, 4 };
    var b1 = Blok();
    Dogrula(!kapi.Uygula(b1, 4, 5) && b1.All(x => x == 0), "Eşik altı blok sıfırlanmalı");
    Dogrula(kapi.Uygula(Blok(), 4, 30), "Eşik üstü blok geçmeli");
    Dogrula(kapi.Uygula(Blok(), 4, 5) && kapi.Uygula(Blok(), 4, 5), "Kuyruk süresince 2 blok geçmeli");
    Dogrula(!kapi.Uygula(Blok(), 4, 5), "Kuyruk bitince kapanmalı");
    Dogrula(new GurultuKapisi().Uygula(Blok(), 4, 0), "Eşik 0 iken kapı devre dışı olmalı");
});

Test("Önerilen gürültü eşiği: tepe değerler atılır, en fazla 60", () =>
{
    var olcum = Enumerable.Repeat(4, 18).Concat(new[] { 90, 95 }).ToList();   // 2 tık sesi
    Dogrula(GurultuKapisi.OnerilenEsik(olcum) == 9, $"4*1.5+3=9 beklenirdi, {GurultuKapisi.OnerilenEsik(olcum)}");
    Dogrula(GurultuKapisi.OnerilenEsik(Enumerable.Repeat(80, 10).ToList()) == 60, "Üst sınır 60 olmalı");
    Dogrula(GurultuKapisi.OnerilenEsik(Array.Empty<int>()) == 0, "Ölçüm yoksa 0");
});

Test("Minimum güven çalışırken değiştirilebilir", () =>
{
    var g = new TanimaGecmisi(0.2f);
    Dogrula(g.Ekle("az emin", 0.3f), "0.3 >= 0.2 eklenmeli");
    g.MinimumGuven = 0.5f;
    Dogrula(!g.Ekle("az emin", 0.3f), "Eşik 0.5 iken 0.3 eklenmemeli");
});

Test("Vosk tam sonucu: metin ve kelime güvenlerinin ortalaması", () =>
{
    var (metin, guven) = VoskSonucu.TamSonuc("{\"result\":[{\"conf\":1.0,\"word\":\"bugün\"},{\"conf\":0.5,\"word\":\"hava\"}],\"text\":\"bugün hava\"}");
    Dogrula(metin == "bugün hava", "Metin okunmalı");
    Dogrula(Math.Abs(guven - 0.75f) < 0.001f, $"Ortalama güven 0.75 olmalı, {guven}");
    var (bos, _) = VoskSonucu.TamSonuc("{\"text\":\"\"}");
    Dogrula(bos == "", "Boş sonuç boş metin vermeli");
    Dogrula(VoskSonucu.TamSonuc("bozuk json").Guven == 0f, "Bozuk JSON güvenli şekilde ele alınmalı");
});

Test("Vosk ara sonucu ve PCM ses seviyesi", () =>
{
    Dogrula(VoskSonucu.AraSonuc("{\"partial\":\"merhaba dü\"}") == "merhaba dü", "Ara sonuç okunmalı");
    var tampon = new byte[4];
    BitConverter.GetBytes((short)16384).CopyTo(tampon, 0);
    BitConverter.GetBytes((short)-32768).CopyTo(tampon, 2);
    Dogrula(VoskSonucu.SesSeviyesi(tampon, 4) == 100, "En yüksek genlik 100 olmalı");
    Dogrula(VoskSonucu.SesSeviyesi(tampon, 2) == 50, "Yarım genlik 50 olmalı");
});

Console.WriteLine();
Console.WriteLine($"Sonuç: {basarili} başarılı, {basarisiz} başarısız");
return basarisiz == 0 ? 0 : 1;
