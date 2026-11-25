using System;
using System.IO;
using Hafta5_Evim;

// Basit, paket gerektirmeyen davranış testleri. Çalıştırma: dotnet run --project tests/Hafta5_Evim.Tests
class Testler
{
    static int gecen = 0, kalan = 0;

    static void Dogrula(string ad, bool kosul)
    {
        if (kosul) { gecen++; Console.WriteLine($"[GEÇTİ] {ad}"); }
        else { kalan++; Console.WriteLine($"[KALDI] {ad}"); }
    }

    static string KonsolYakala(Action a)
    {
        var eski = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        try { a(); } finally { Console.SetOut(eski); }
        return sw.ToString();
    }

    static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        int yil = DateTime.Now.Year;

        // Property doğrulaması
        var k = new Kisi("Ali", 2000, "Doktor");
        Dogrula("Geçerli doğum yılı atanır", k.DogumYili == 2000);
        string cikti = KonsolYakala(() => k.DogumYili = 1800);
        Dogrula("1900 ve öncesi reddedilir", k.DogumYili == 2000);
        Dogrula("Geçersiz yılda uyarı yazılır", cikti.Contains("Geçersiz doğum yılı!"));
        KonsolYakala(() => k.DogumYili = yil + 1);
        Dogrula("Gelecek yıl reddedilir", k.DogumYili == 2000);
        k.DogumYili = yil;
        Dogrula("İçinde bulunulan yıl kabul edilir", k.DogumYili == yil);

        // Görev 1: Meslek doğrulaması
        Dogrula("Boş meslek -> Belirtilmedi", new Kisi("A", 1990, "").Meslek == "Belirtilmedi");
        Dogrula("Boşluklu meslek -> Belirtilmedi", new Kisi("B", 1990, "   ").Meslek == "Belirtilmedi");
        Dogrula("null meslek -> Belirtilmedi", new Kisi("C", 1990, null).Meslek == "Belirtilmedi");
        Dogrula("Meslek kırpılır", new Kisi("D", 1990, "  Avukat ").Meslek == "Avukat");

        // Yapıcı metot ve statik sayaç
        int once = Kisi.KisiSayisi;
        new Kisi("E", 1980, "Pilot");
        new Kisi("F", 1981, "Aşçı");
        Dogrula("Her new Kisi sayacı 1 artırır", Kisi.KisiSayisi == once + 2);
        var tek = new Kisi("Gül");
        Dogrula("Kisi(string ad) aşırı yüklemesi çalışır", tek.Ad == "Gül" && tek.Meslek == "Belirtilmedi");
        Dogrula("Aşırı yüklenmiş yapıcı da sayacı artırır", Kisi.KisiSayisi == once + 3);

        // YasHesapla / BilgiYaz
        var y = new Kisi("Ayşe", 1970, "Öğretmen");
        Dogrula("YasHesapla doğru", y.YasHesapla() == yil - 1970);
        string bilgi = KonsolYakala(y.BilgiYaz);
        Dogrula("BilgiYaz biçimi doğru", bilgi.Trim() == $"Ayşe - Öğretmen - {yil - 1970} yaşında");

        // Görev 3: static readonly adres
        Dogrula("EvAdresi boş değil", !string.IsNullOrWhiteSpace(Kisi.EvAdresi));
        var alan = typeof(Kisi).GetField("EvAdresi");
        Dogrula("EvAdresi static readonly", alan != null && alan.IsStatic && (alan.IsInitOnly || alan.IsLiteral));

        // Ev sınıfı
        var ev = new Ev();
        Dogrula("Boş evde ortalama 0", ev.OrtalamaYas() == 0);
        Dogrula("Boş evde EnYasli null", ev.EnYasli() == null);
        ev.UyeEkle(new Kisi("Anne", 1970, "Öğretmen"));
        ev.UyeEkle(new Kisi("Baba", 1968, "Mühendis"));
        ev.UyeEkle(new Kisi("Ben", 2004, "Öğrenci"));
        ev.UyeEkle(new Kisi("Dede", 1945, "Emekli"));
        Dogrula("Ev 4 üye tutar", ev.UyeSayisi == 4);
        double beklenen = ((yil - 1970) + (yil - 1968) + (yil - 2004) + (yil - 1945)) / 4.0;
        Dogrula("Ortalama yaş doğru", Math.Abs(ev.OrtalamaYas() - beklenen) < 1e-9);
        Dogrula("EnYasli Dede'yi döndürür", ev.EnYasli().Ad == "Dede");
        string liste = KonsolYakala(ev.TumUyeleriYaz);
        Dogrula("TumUyeleriYaz 4 satır yazar", liste.Trim().Split('\n').Length == 4);

        Console.WriteLine();
        Console.WriteLine($"Sonuç: {gecen}/{gecen + kalan} test geçti.");
        return kalan == 0 ? 0 : 1;
    }
}
