using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using NotepadApp.Core.Models;

namespace NotepadApp.Core.Services;

/// <summary>Dosyadan yükleme işleminin sonucu: notlar ve varsa kullanıcıya gösterilecek uyarı.</summary>
public record YuklemeSonucu(List<Not> Notlar, string? Uyari);

/// <summary>
/// Kaydetme sonucu: diske yazılan ve silinen not dosyası sayısı ile çakışan notların diskteki sürümleri.
/// </summary>
public record KayitSonucu(int Yazilan, int Silinen)
{
    /// <summary>Başka bir pencere/uygulama tarafından değiştirildiği için yazılmayan notların diskteki hâli.</summary>
    public List<Not> Cakismalar { get; init; } = new();
}

/// <summary>
/// Notları JSON olarak saklar (File I/O + Serialization).
/// <para>
/// Her not, veri klasöründe <c>&lt;Id&gt;.json</c> adlı <b>ayrı bir dosyada</b> tutulur
/// (ör. <c>notlar.json</c> verilirse klasör <c>notlar\</c> olur). Kaydetmede yalnızca içeriği değişen notların
/// dosyası yeniden yazılır, silinen notların dosyası kaldırılır; böylece not sayısı arttıkça her kayıtta
/// tüm verinin yeniden yazılması önlenir.
/// </para>
/// <para>
/// Eski sürümün tek dosyalı <c>notlar.json</c> biçimi ilk açılışta otomatik olarak klasör biçimine taşınır
/// ve eski dosya <c>notlar.json.eski</c> adıyla saklanır.
/// </para>
/// </summary>
public class NotDeposu
{
    private static readonly JsonSerializerOptions JsonAyarlari = new()
    {
        WriteIndented = true,
        // Türkçe karakterler kaçış dizisine çevrilmesin, dosya okunabilir kalsın.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Son yükleme/kaydetmede diskte olduğu bilinen notların JSON içeriği (değişiklik tespiti için).</summary>
    private readonly Dictionary<Guid, string> _diskIcerigi = new();

    /// <summary>Son yükleme/kaydetmede diskte görülen sürüm numaraları (çakışma tespiti için).</summary>
    private readonly Dictionary<Guid, int> _bilinenSurum = new();

    /// <summary>Eski tek dosyalı biçimin yolu (geriye dönük uyumluluk için).</summary>
    public string DosyaYolu { get; }

    /// <summary>Notların ayrı dosyalar hâlinde tutulduğu klasör.</summary>
    public string KlasorYolu { get; }

    public NotDeposu(string dosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(dosyaYolu))
            throw new ArgumentException("Dosya yolu boş olamaz.", nameof(dosyaYolu));
        DosyaYolu = Path.GetFullPath(dosyaYolu);
        KlasorYolu = Path.Combine(Path.GetDirectoryName(DosyaYolu)!, Path.GetFileNameWithoutExtension(DosyaYolu));
    }

    /// <summary>Varsayılan konum: %APPDATA%\YMH219-NotepadApp\notlar.json (klasör: notlar\)</summary>
    public static string VarsayilanYol() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "YMH219-NotepadApp", "notlar.json");

    public string NotDosyasi(Guid id) => Path.Combine(KlasorYolu, id.ToString("N") + ".json");

    public static string Serilestir(Not not) => JsonSerializer.Serialize(not, JsonAyarlari);

    /// <summary>
    /// Değişen notları yazar, silinen notların dosyalarını kaldırır.
    /// Bu depo tarafından hiç görülmemiş dosyalara (ör. başka bir pencerenin eklediği notlar) dokunulmaz.
    /// </summary>
    public KayitSonucu Kaydet(IEnumerable<Not> notlar)
    {
        Directory.CreateDirectory(KlasorYolu);
        var liste = notlar.ToList();
        int yazilan = 0, silinen = 0;
        var cakismalar = new List<Not>();

        foreach (var not in liste)
        {
            string json = Serilestir(not);
            if (_diskIcerigi.TryGetValue(not.Id, out var eski) && eski == json)
                continue;   // değişmemiş: dosyaya dokunma
            if (DisaridanDegisti(not.Id) is Not disk)
            {
                cakismalar.Add(disk);
                continue;
            }
            NotuYaz(not, json);
            yazilan++;
        }

        var mevcutIdler = liste.Select(n => n.Id).ToHashSet();
        foreach (var id in _diskIcerigi.Keys.Where(id => !mevcutIdler.Contains(id)).ToList())
        {
            if (DisaridanDegisti(id) is Not disk)
            {
                cakismalar.Add(disk);   // başka pencere bu notu değiştirmiş: silmeden önce kullanıcıya sor
                continue;
            }
            File.Delete(NotDosyasi(id));
            _diskIcerigi.Remove(id);
            _bilinenSurum.Remove(id);
            silinen++;
        }
        return new KayitSonucu(yazilan, silinen) { Cakismalar = cakismalar };
    }

    private void NotuYaz(Not not, string? json = null)
    {
        json ??= Serilestir(not);
        AtomikYaz(NotDosyasi(not.Id), json);
        _diskIcerigi[not.Id] = json;
        _bilinenSurum[not.Id] = not.Surum;
    }

    /// <summary>
    /// Not dosyası bu depo son gördüğünden beri başka biri tarafından değiştirildiyse diskteki hâlini döndürür.
    /// Dosya hiç görülmediyse ve şimdi varsa (iki pencerede aynı Id — pratikte olmaz) yine çakışma sayılır.
    /// </summary>
    public Not? DisaridanDegisti(Guid id)
    {
        string yol = NotDosyasi(id);
        if (!File.Exists(yol))
            return null;
        Not? disk;
        try { disk = JsonSerializer.Deserialize<Not>(File.ReadAllText(yol, Encoding.UTF8), JsonAyarlari); }
        catch (Exception ex) when (ex is JsonException or IOException) { return null; }
        if (disk is null)
            return null;
        bool biliniyor = _bilinenSurum.TryGetValue(id, out int bilinen);
        if (biliniyor && disk.Surum == bilinen && _diskIcerigi.TryGetValue(id, out var icerik) && icerik == Serilestir(disk))
            return null;
        return biliniyor || !_diskIcerigi.ContainsKey(id) ? disk : null;
    }

    /// <summary>Çakışmada "benim sürümüm kazansın": sürüm numarası diskteki sürümün üstüne çıkarılıp yazılır.</summary>
    public void BeniminkiniYaz(Not yerel, Not disk)
    {
        yerel.Surum = Math.Max(yerel.Surum, disk.Surum) + 1;
        NotuYaz(yerel);
    }

    /// <summary>Çakışmada "diskteki sürümü al" veya dış değişiklik yüklendiğinde: notu bilinen duruma işaretler.</summary>
    public void DisktekiniKabulEt(Not disk)
    {
        _diskIcerigi[disk.Id] = Serilestir(disk);
        _bilinenSurum[disk.Id] = disk.Surum;
    }

    /// <summary>Önce geçici dosyaya yazar, sonra asıl dosyanın yerine taşır (yarıda kalan yazma eski dosyayı bozmaz).</summary>
    private static void AtomikYaz(string yol, string icerik)
    {
        string gecici = yol + ".tmp";
        File.WriteAllText(gecici, icerik, new UTF8Encoding(false));
        File.Move(gecici, yol, overwrite: true);
    }

    public YuklemeSonucu Yukle()
    {
        _diskIcerigi.Clear();
        _bilinenSurum.Clear();
        string? tasimaUyarisi = null;

        if (!Directory.Exists(KlasorYolu))
        {
            if (!File.Exists(DosyaYolu))
                return new YuklemeSonucu(new List<Not>(), null);

            // Eski tek dosyalı biçim: oku, klasöre taşı
            var eski = EskiDosyayiOku();
            if (eski.Notlar.Count == 0)
                return eski;
            Kaydet(eski.Notlar);
            File.Move(DosyaYolu, DosyaYolu + ".eski", overwrite: true);
            tasimaUyarisi = eski.Uyari;
        }

        var notlar = new List<Not>();
        var bozuklar = new List<string>();
        foreach (string dosya in Directory.GetFiles(KlasorYolu, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(dosya, Encoding.UTF8);
                var not = JsonSerializer.Deserialize<Not>(json, JsonAyarlari);
                if (not is null || string.IsNullOrWhiteSpace(not.Baslik))
                    throw new JsonException("Başlık yok.");
                notlar.Add(not);
                _diskIcerigi[not.Id] = Serilestir(not);
                _bilinenSurum[not.Id] = not.Surum;
            }
            catch (JsonException)
            {
                File.Move(dosya, dosya + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak", overwrite: true);
                bozuklar.Add(Path.GetFileName(dosya));
            }
        }

        string? uyari = tasimaUyarisi;
        if (bozuklar.Count > 0)
            uyari = $"{bozuklar.Count} not dosyası bozuk olduğu için okunamadı ve .bak uzantısıyla yedeklendi:\n" +
                    string.Join("\n", bozuklar);
        return new YuklemeSonucu(notlar, uyari);
    }

    /// <summary>Eski sürümün tüm notları içeren tek JSON dosyasını okur.</summary>
    private YuklemeSonucu EskiDosyayiOku()
    {
        string json = File.ReadAllText(DosyaYolu, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(json))
            return new YuklemeSonucu(new List<Not>(), "Not dosyası boştu; yeni bir liste başlatıldı.");

        try
        {
            var notlar = JsonSerializer.Deserialize<List<Not>>(json, JsonAyarlari) ?? new List<Not>();
            notlar.RemoveAll(n => n is null || string.IsNullOrWhiteSpace(n.Baslik));
            return new YuklemeSonucu(notlar, null);
        }
        catch (JsonException ex)
        {
            string yedek = DosyaYolu + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak";
            File.Copy(DosyaYolu, yedek, overwrite: true);
            return new YuklemeSonucu(new List<Not>(),
                "Not dosyası bozuk olduğu için okunamadı.\n" +
                $"Ayrıntı: {ex.Message}\n\n" +
                $"Bozuk dosyanın yedeği alındı:\n{yedek}\n\nBoş bir liste ile devam ediliyor.");
        }
    }
}
