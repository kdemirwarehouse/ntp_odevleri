using NotepadApp.Core.Models;
using NotepadApp.Core.Services;

// Harici test çatısı gerektirmeyen basit konsol test çalıştırıcısı.
// Çalıştırma: dotnet run --project tests/NotepadApp.Tests

int basarili = 0, basarisiz = 0;
string geciciKlasor = Path.Combine(Path.GetTempPath(), "NotepadAppTests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(geciciKlasor);

void Test(string ad, Action govde)
{
    try
    {
        govde();
        basarili++;
        Console.WriteLine($"  [OK]   {ad}");
    }
    catch (Exception ex)
    {
        basarisiz++;
        Console.WriteLine($"  [HATA] {ad}: {ex.Message}");
    }
}

void Dogrula(bool kosul, string mesaj)
{
    if (!kosul) throw new Exception(mesaj);
}

Console.WriteLine("NotepadApp testleri");

Test("Not oluşturulunca tarihler atanır", () =>
{
    var not = new Not("Alışveriş", "süt, ekmek");
    Dogrula(not.OlusturmaTarihi != default, "Oluşturma tarihi atanmadı");
    Dogrula(not.OlusturmaTarihi == not.DuzenlemeTarihi, "İlk düzenleme tarihi oluşturma tarihine eşit olmalı");
    Dogrula(not.Kategori == "Genel", "Varsayılan kategori Genel olmalı");
});

Test("Boş başlık reddedilir", () =>
{
    bool hataAtildi = false;
    try { _ = new Not("   ", "içerik"); } catch (ArgumentException) { hataAtildi = true; }
    Dogrula(hataAtildi, "Boş başlık için ArgumentException beklenirdi");
});

Test("Güncelleme düzenleme tarihini ileri alır, oluşturma tarihini korur", () =>
{
    var not = new Not("Ödev", "ilk");
    var olusturma = not.OlusturmaTarihi;
    Thread.Sleep(15);
    not.Guncelle("Ödev 2", "ikinci", "Okul");
    Dogrula(not.OlusturmaTarihi == olusturma, "Oluşturma tarihi değişmemeli");
    Dogrula(not.DuzenlemeTarihi > olusturma, "Düzenleme tarihi ilerlemeli");
    Dogrula(not.Baslik == "Ödev 2" && not.Kategori == "Okul", "Alanlar güncellenmedi");
});

Test("NotDefteri ekle / sil / bul", () =>
{
    var defter = new NotDefteri();
    var a = defter.Ekle("A", "1");
    defter.Ekle("B", "2");
    Dogrula(defter.Sayi == 2, "İki not olmalı");
    Dogrula(defter.Sil(a.Id), "Silme başarılı olmalı");
    Dogrula(defter.Bul(a.Id) is null, "Silinen not bulunmamalı");
    Dogrula(!defter.Sil(Guid.NewGuid()), "Olmayan not silinememeli");
});

Test("Her değişiklikte Degisti olayı tetiklenir", () =>
{
    var defter = new NotDefteri();
    int sayac = 0;
    defter.Degisti += (_, _) => sayac++;
    var n = defter.Ekle("A", "");
    defter.Guncelle(n.Id, "A2", "x", null);
    defter.Sil(n.Id);
    Dogrula(sayac == 3, $"3 olay beklenirdi, {sayac} geldi");
});

Test("Arama başlık ve içerikte, büyük/küçük harf duyarsız çalışır", () =>
{
    var defter = new NotDefteri();
    defter.Ekle("Market listesi", "Süt ve yumurta");
    defter.Ekle("Proje", "GitHub README yaz");
    Dogrula(defter.Filtrele("market", null).Count == 1, "Başlıkta arama çalışmadı");
    Dogrula(defter.Filtrele("readme", null).Count == 1, "İçerikte arama çalışmadı");
    Dogrula(defter.Filtrele("", null).Count == 2, "Boş arama tüm notları döndürmeli");
});

Test("Kategoriye göre filtreleme", () =>
{
    var defter = new NotDefteri();
    defter.Ekle("A", "", "Okul");
    defter.Ekle("B", "", "Ev");
    defter.Ekle("C", "", "okul");
    Dogrula(defter.Filtrele(null, "Okul").Count == 2, "Okul kategorisinde 2 not olmalı");
    Dogrula(defter.Filtrele(null, NotDefteri.TumKategoriler).Count == 3, "(Tümü) tüm notları döndürmeli");
    Dogrula(defter.Kategoriler().Count == 2, "Kategoriler büyük/küçük harf duyarsız tekilleştirilmeli");
});

Test("JSON kaydet ve geri yükle (Türkçe karakterlerle)", () =>
{
    string yol = Path.Combine(geciciKlasor, "notlar.json");
    var depo = new NotDeposu(yol);
    var defter = new NotDefteri();
    defter.Ekle("Çalışma planı", "İş, güç, şeker, ğ ü ö ç ı", "Okul");
    depo.Kaydet(defter.Notlar);

    Dogrula(File.ReadAllText(depo.NotDosyasi(defter.Notlar[0].Id)).Contains("Çalışma planı"), "Türkçe karakterler dosyada okunabilir olmalı");

    var sonuc = depo.Yukle();
    Dogrula(sonuc.Uyari is null, "Uyarı olmamalı");
    Dogrula(sonuc.Notlar.Count == 1, "Bir not yüklenmeli");
    var yuklenen = sonuc.Notlar[0];
    var asil = defter.Notlar[0];
    Dogrula(yuklenen.Id == asil.Id && yuklenen.Icerik == asil.Icerik, "Alanlar korunmalı");
    Dogrula(yuklenen.OlusturmaTarihi == asil.OlusturmaTarihi, "Tarih korunmalı");
});

Test("Dosya yoksa boş liste, uyarısız döner", () =>
{
    var depo = new NotDeposu(Path.Combine(geciciKlasor, "yok", "notlar.json"));
    var sonuc = depo.Yukle();
    Dogrula(sonuc.Notlar.Count == 0 && sonuc.Uyari is null, "Boş liste beklenirdi");
});

Test("Bozuk dosya yedeklenir ve uyarı üretilir", () =>
{
    string yol = Path.Combine(geciciKlasor, "bozuk.json");
    File.WriteAllText(yol, "{ bu json değil ");
    var sonuc = new NotDeposu(yol).Yukle();
    Dogrula(sonuc.Notlar.Count == 0, "Boş liste beklenirdi");
    Dogrula(sonuc.Uyari is not null, "Uyarı beklenirdi");
    Dogrula(Directory.GetFiles(geciciKlasor, "bozuk.json.*.bak").Length == 1, "Yedek dosyası oluşmalı");
});

Test("Boş dosya uyarı ile boş liste döner", () =>
{
    string yol = Path.Combine(geciciKlasor, "bos.json");
    File.WriteAllText(yol, "");
    var sonuc = new NotDeposu(yol).Yukle();
    Dogrula(sonuc.Notlar.Count == 0 && sonuc.Uyari is not null, "Boş dosya için uyarı beklenirdi");
});

Test("Yalnızca değişen not yazılır, silinen notun dosyası kaldırılır", () =>
{
    string yol = Path.Combine(geciciKlasor, "artimli", "notlar.json");
    var depo = new NotDeposu(yol);
    var defter = new NotDefteri();
    var a = defter.Ekle("A", "1");
    var b = defter.Ekle("B", "2");
    var c = defter.Ekle("C", "3");
    var k1 = depo.Kaydet(defter.Notlar);
    Dogrula(k1.Yazilan == 3 && k1.Silinen == 0, "İlk kayıtta 3 dosya yazılmalı");
    var k2 = depo.Kaydet(defter.Notlar);
    Dogrula(k2.Yazilan == 0 && k2.Silinen == 0, "Değişiklik yokken hiçbir dosya yazılmamalı");
    defter.Guncelle(b.Id, "B2", "yeni", null);
    defter.Sil(c.Id);
    var k3 = depo.Kaydet(defter.Notlar);
    Dogrula(k3.Yazilan == 1 && k3.Silinen == 1, "Yalnızca B yazılmalı, C silinmeli");
    Dogrula(!File.Exists(depo.NotDosyasi(c.Id)) && File.Exists(depo.NotDosyasi(a.Id)), "C dosyası silinmiş, A duruyor olmalı");
    var yeni = new NotDeposu(yol).Yukle();
    Dogrula(yeni.Notlar.Count == 2 && yeni.Notlar.Any(n => n.Baslik == "B2"), "Yeniden yüklemede 2 not ve güncel B olmalı");
});

Test("İki pencere aynı notu değiştirirse ikinci kayıt çakışma bildirir", () =>
{
    string yol = Path.Combine(geciciKlasor, "cakisma", "notlar.json");
    var ilk = new NotDeposu(yol);
    var defter0 = new NotDefteri();
    var not = defter0.Ekle("Ortak not", "ilk");
    ilk.Kaydet(defter0.Notlar);

    // İki pencere aynı anda açılır
    var depoA = new NotDeposu(yol); var defterA = new NotDefteri(); defterA.Yukle(depoA.Yukle().Notlar);
    var depoB = new NotDeposu(yol); var defterB = new NotDefteri(); defterB.Yukle(depoB.Yukle().Notlar);

    defterA.Guncelle(not.Id, "Ortak not", "A yazdı", null);
    Dogrula(depoA.Kaydet(defterA.Notlar).Cakismalar.Count == 0, "İlk kaydeden çakışma görmemeli");

    defterB.Guncelle(not.Id, "Ortak not", "B yazdı", null);
    var sonuc = depoB.Kaydet(defterB.Notlar);
    Dogrula(sonuc.Cakismalar.Count == 1 && sonuc.Cakismalar[0].Icerik == "A yazdı", "B çakışma almalı ve diskteki A sürümünü görmeli");
    Dogrula(new NotDeposu(yol).Yukle().Notlar[0].Icerik == "A yazdı", "Diskteki A sürümü ezilmemeli");

    depoB.BeniminkiniYaz(defterB.Bul(not.Id)!, sonuc.Cakismalar[0]);
    var son = new NotDeposu(yol).Yukle().Notlar[0];
    Dogrula(son.Icerik == "B yazdı" && son.Surum > sonuc.Cakismalar[0].Surum, "Benimkini yaz B'yi daha yüksek sürümle kaydetmeli");
    Dogrula(depoB.Kaydet(defterB.Notlar).Cakismalar.Count == 0, "Çözümden sonra çakışma kalmamalı");
});

Test("Başka pencerenin eklediği not kaydetmede silinmez; kopya oluşturma", () =>
{
    string yol = Path.Combine(geciciKlasor, "ekleme", "notlar.json");
    var depoA = new NotDeposu(yol); var defterA = new NotDefteri(); defterA.Yukle(depoA.Yukle().Notlar);
    var depoB = new NotDeposu(yol); var defterB = new NotDefteri(); defterB.Yukle(depoB.Yukle().Notlar);
    defterB.Ekle("B'nin notu", "x"); depoB.Kaydet(defterB.Notlar);
    defterA.Ekle("A'nın notu", "y"); depoA.Kaydet(defterA.Notlar);
    Dogrula(new NotDeposu(yol).Yukle().Notlar.Count == 2, "İki pencerenin notu da diskte kalmalı");
    var kopya = defterA.KopyasiniEkle(defterA.Notlar[0]);
    Dogrula(kopya.Baslik.EndsWith("(kopya)") && kopya.Id != defterA.Notlar[0].Id, "Kopya yeni Id ve (kopya) ekiyle oluşmalı");
});

Test("Eski tek dosyalı notlar.json klasör biçimine taşınır", () =>
{
    string klasor = Path.Combine(geciciKlasor, "tasima");
    Directory.CreateDirectory(klasor);
    string yol = Path.Combine(klasor, "notlar.json");
    var eskiNotlar = new List<Not> { new("Eski 1", "x"), new("Eski 2", "y") };
    File.WriteAllText(yol, System.Text.Json.JsonSerializer.Serialize(eskiNotlar));
    var depo = new NotDeposu(yol);
    var sonuc = depo.Yukle();
    Dogrula(sonuc.Notlar.Count == 2, "İki eski not yüklenmeli");
    Dogrula(Directory.GetFiles(depo.KlasorYolu, "*.json").Length == 2, "Her not ayrı dosyaya taşınmalı");
    Dogrula(!File.Exists(yol) && File.Exists(yol + ".eski"), "Eski dosya .eski olarak saklanmalı");
});

Test("Klasördeki bozuk not dosyası yedeklenir, diğerleri yüklenir", () =>
{
    string yol = Path.Combine(geciciKlasor, "kismi", "notlar.json");
    var depo = new NotDeposu(yol);
    var defter = new NotDefteri();
    defter.Ekle("Sağlam", "ok");
    depo.Kaydet(defter.Notlar);
    File.WriteAllText(Path.Combine(depo.KlasorYolu, Guid.NewGuid().ToString("N") + ".json"), "{ bozuk");
    var sonuc = new NotDeposu(yol).Yukle();
    Dogrula(sonuc.Notlar.Count == 1 && sonuc.Uyari is not null, "Sağlam not yüklenmeli ve uyarı verilmeli");
    Dogrula(Directory.GetFiles(depo.KlasorYolu, "*.bak").Length == 1, "Bozuk dosya .bak yapılmalı");
});

Test("RTF'den HTML: kalın, italik, altı çizili ve Türkçe karakterler korunur", () =>
{
    string rtf = @"{\rtf1\ansi\ansicpg1254\deff0{\fonttbl{\f0\fnil\fcharset162 Segoe UI;}}" +
                 @"\f0\fs22 Normal \b kal\'fdn\b0  ve \i italik \i0 ve \ul alt\ul0 .\par " +
                 @"\u350?eker <etiket>\par}";
    string html = BicimliDisaAktarici.RtfdenHtml(rtf);
    Dogrula(html.Contains("<b>kalın</b>"), "Kalın ve \\'fd (ı) korunmalı: " + html);
    Dogrula(html.Contains("<i>italik </i>") && html.Contains("<u>alt</u>"), "İtalik ve altı çizili korunmalı: " + html);
    Dogrula(html.Contains("Şeker") && html.Contains("&lt;etiket&gt;"), "Unicode ve HTML kaçışları doğru olmalı: " + html);
    Dogrula(!html.Contains("Segoe UI"), "Font tablosu metne karışmamalı");
});

Test("RTF ve HTML dışa aktarma dosyaları oluşturulur", () =>
{
    var not = new Not("Biçimli not", "düz içerik İğ");
    string rtfYolu = Path.Combine(geciciKlasor, "not.rtf"), htmlYolu = Path.Combine(geciciKlasor, "not.html");
    BicimliDisaAktarici.RtfOlarakAktar(not, rtfYolu);
    BicimliDisaAktarici.HtmlOlarakAktar(not, htmlYolu);
    Dogrula(File.ReadAllText(rtfYolu).StartsWith(@"{\rtf1"), "RTF başlığı olmalı");
    Dogrula(File.ReadAllText(rtfYolu).Contains(@"\u304?"), "İ harfi \\u304 olarak kaçışlanmalı");
    string html = File.ReadAllText(htmlYolu);
    Dogrula(html.Contains("<h1>Biçimli not</h1>") && html.Contains("düz içerik İğ"), "HTML başlık ve içerik içermeli");
});

Test("Geri al / yinele: çok seviyeli, yeni kayıt yineleyi temizler", () =>
{
    var g = new GeriAlGecmisi<string>(kapasite: 10);
    g.Sifirla("");
    g.Kaydet("a"); g.Kaydet("ab"); g.Kaydet("abc");
    Dogrula(!g.Kaydet("abc"), "Aynı durum ikinci kez kaydedilmemeli");
    Dogrula(g.GeriAl() == "ab" && g.GeriAl() == "a", "İki adım geri alınmalı");
    Dogrula(g.Yinele() == "ab" && g.YineleAdimSayisi == 1, "Bir adım yinelenmeli");
    g.Kaydet("abX");
    Dogrula(!g.Yinelenebilir, "Yeni kayıt yinele yığınını temizlemeli");
    Dogrula(g.GeriAl() == "ab" && g.GeriAl() == "a" && g.GeriAl() == "" && g.GeriAl() is null, "Başlangıca kadar geri alınmalı");
});

Test("Geri al geçmişi kapasiteyi aşınca en eski adımı düşürür", () =>
{
    var g = new GeriAlGecmisi<int>(kapasite: 3);
    g.Sifirla(0);
    for (int i = 1; i <= 5; i++) g.Kaydet(i);
    Dogrula(g.GeriAlAdimSayisi == 2, $"Kapasite 3 iken 2 geri alma adımı kalmalı, {g.GeriAlAdimSayisi}");
    Dogrula(g.GeriAl() == 4 && g.GeriAl() == 3 && !g.GeriAlinabilir, "En eski adımlar düşmüş olmalı");
});

Test("Sabitlenen not listenin en üstünde kalır", () =>
{
    var defter = new NotDefteri();
    var eski = defter.Ekle("Eski not", "");
    Thread.Sleep(15);
    defter.Ekle("Yeni not", "");
    Dogrula(defter.Filtrele(null, null)[0].Baslik == "Yeni not", "Sabitleme yokken en yeni üstte olmalı");
    Dogrula(defter.SabitlemeDegistir(eski.Id), "Sabitleme true döndürmeli");
    Dogrula(defter.Filtrele(null, null)[0].Baslik == "Eski not", "Sabit not en üstte olmalı");
    Dogrula(eski.ToString().StartsWith("📌"), "Listede raptiye simgesi görünmeli");
    Dogrula(!defter.SabitlemeDegistir(eski.Id) && defter.Filtrele(null, null)[0].Baslik == "Yeni not", "Kaldırınca sıra normale dönmeli");
});

Test("Etiketler: ayrıştırma, tekilleştirme, filtreleme ve aramada kullanım", () =>
{
    var etiketler = Not.EtiketleriAyristir(" sınav, #Proje; proje ,, acil ");
    Dogrula(etiketler.SequenceEqual(new[] { "sınav", "Proje", "acil" }), "Etiketler kırpılmalı ve tekilleştirilmeli: " + string.Join("|", etiketler));
    var defter = new NotDefteri();
    defter.Ekle("Matematik", "türev", "Okul", etiketler: new[] { "sınav", "acil" });
    defter.Ekle("Rapor", "", "Okul", etiketler: new[] { "proje" });
    defter.Ekle("Market", "", "Ev");
    Dogrula(defter.Filtrele(null, null, "SINAV").Count == 1, "Etiket filtresi büyük/küçük harf duyarsız olmalı");
    Dogrula(defter.Filtrele(null, "Okul", NotDefteri.TumEtiketler).Count == 2, "(Tüm etiketler) filtre uygulamamalı");
    Dogrula(defter.Filtrele("#proj", null).Count == 1, "Arama etiketlerde de yapılmalı");
    Dogrula(defter.Etiketler().SequenceEqual(new[] { "acil", "proje", "sınav" }), "Etiket listesi tekil ve sıralı olmalı");
    Dogrula(defter.Notlar[0].ToString().Contains("#sınav #acil"), "Listede etiketler görünmeli");
    bool hata = false;
    try { Not.EtiketleriAyristir(string.Join(",", Enumerable.Range(1, 11))); } catch (ArgumentException) { hata = true; }
    Dogrula(hata, "11 etiket reddedilmeli");
});

Test("XML arşivi: dışa aktar, geri oku ve birleştir", () =>
{
    var defter = new NotDefteri();
    var a = defter.Ekle("Ödev <1>", "İçerik & \"tırnak\"", "Okul", etiketler: new[] { "sınav" });
    defter.SabitlemeDegistir(a.Id);
    string yol = Path.Combine(geciciKlasor, "arsiv.xml");
    XmlNotAktarici.DisaAktar(defter.Notlar, yol);
    string xml = File.ReadAllText(yol);
    Dogrula(xml.Contains("<NotArsivi") && xml.Contains("Ödev &lt;1&gt;"), "XML kök elemanı ve kaçışlar doğru olmalı");

    var okunan = XmlNotAktarici.IceAktar(yol);
    Dogrula(okunan.Count == 1 && okunan[0].Id == a.Id && okunan[0].Sabitlenmis && okunan[0].Etiketler.SequenceEqual(new[] { "sınav" }),
            "Id, sabitleme ve etiketler korunmalı");

    var hedef = new NotDefteri();
    hedef.Ekle("Başka not", "");
    var s1 = hedef.IceAktar(okunan);
    Dogrula(s1 == (1, 0, 0), "İlk içe aktarmada 1 yeni not");
    var s2 = hedef.IceAktar(XmlNotAktarici.IceAktar(yol));
    Dogrula(s2 == (0, 0, 1) && hedef.Sayi == 2, "Aynı arşiv tekrar alınınca atlanmalı");

    File.WriteAllText(Path.Combine(geciciKlasor, "bozuk.xml"), "<NotArsivi><Not>");
    bool hata = false;
    try { XmlNotAktarici.IceAktar(Path.Combine(geciciKlasor, "bozuk.xml")); } catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException) { hata = true; }
    Dogrula(hata, "Bozuk XML anlaşılır hata vermeli");
});

Test("Ayarlar kaydedilir, geri okunur; bozuk ayar dosyası varsayılana döner", () =>
{
    string yol = Path.Combine(geciciKlasor, "ayar", "ayarlar.json");
    Dogrula(!Ayarlar.Yukle(yol).KaranlikTema && Ayarlar.Yukle(yol).OtomatikKaydet, "Varsayılan: açık tema, otomatik kaydet açık");
    new Ayarlar { KaranlikTema = true, OtomatikKaydet = false }.Kaydet(yol);
    var okunan = Ayarlar.Yukle(yol);
    Dogrula(okunan.KaranlikTema && !okunan.OtomatikKaydet, "Kaydedilen ayarlar okunmalı");
    File.WriteAllText(yol, "{ bozuk");
    Dogrula(!Ayarlar.Yukle(yol).KaranlikTema, "Bozuk dosyada varsayılan ayarlar dönmeli");
});

Test("TXT dışa aktarma başlık, tarih ve içerik yazar", () =>
{
    var not = new Not("Toplantı: 12/05", "Gündem maddeleri\n1) Bütçe");
    string yol = Path.Combine(geciciKlasor, TxtDisaAktarici.DosyaAdiOner(not));
    Dogrula(!Path.GetFileName(yol).Contains(':') && !Path.GetFileName(yol).Contains('/'), "Geçersiz karakterler temizlenmeli");
    TxtDisaAktarici.DisaAktar(not, yol);
    string metin = File.ReadAllText(yol);
    Dogrula(metin.StartsWith("Toplantı: 12/05"), "Başlık ilk satırda olmalı");
    Dogrula(metin.Contains("Oluşturulma") && metin.Contains("1) Bütçe"), "Tarih ve içerik yazılmalı");
});

try { Directory.Delete(geciciKlasor, true); } catch { /* geçici klasör silinemese de önemli değil */ }

Console.WriteLine();
Console.WriteLine($"Sonuç: {basarili} başarılı, {basarisiz} başarısız");
return basarisiz == 0 ? 0 : 1;
