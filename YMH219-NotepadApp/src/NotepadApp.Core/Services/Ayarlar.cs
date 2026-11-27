using System.Text.Json;

namespace NotepadApp.Core.Services;

/// <summary>
/// Uygulama tercihleri (tema, otomatik kaydetme). Not verisinden ayrı, küçük bir JSON dosyasında tutulur:
/// %APPDATA%\YMH219-NotepadApp\ayarlar.json
/// </summary>
public class Ayarlar
{
    public bool KaranlikTema { get; set; }
    public bool OtomatikKaydet { get; set; } = true;

    public static string VarsayilanYol() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YMH219-NotepadApp", "ayarlar.json");

    /// <summary>Ayar dosyasını okur; yoksa veya bozuksa varsayılan ayarları döndürür (uygulama açılmaya devam eder).</summary>
    public static Ayarlar Yukle(string yol)
    {
        try
        {
            if (File.Exists(yol))
                return JsonSerializer.Deserialize<Ayarlar>(File.ReadAllText(yol)) ?? new Ayarlar();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException) { }
        return new Ayarlar();
    }

    public void Kaydet(string yol)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(yol)!);
            File.WriteAllText(yol, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* tercih kaydedilemese de sorun değil */ }
    }
}
