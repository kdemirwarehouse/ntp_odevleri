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

    Dogrula(File.ReadAllText(yol).Contains("Çalışma planı"), "Türkçe karakterler dosyada okunabilir olmalı");

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
