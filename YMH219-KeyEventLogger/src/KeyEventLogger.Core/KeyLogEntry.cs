namespace KeyEventLogger.Core;

/// <summary>Yakalanan klavye olayının türü.</summary>
public enum OlayTuru
{
    KeyDown,
    KeyUp,
    KeyPress
}

/// <summary>
/// Tek bir klavye olayı kaydı. Değişmez (immutable) bir record olarak tanımlanmıştır;
/// oluşturulduktan sonra içeriği değiştirilemez.
/// </summary>
public sealed record KeyLogEntry(DateTime Zaman, OlayTuru Tur, string Tus, string Ayrinti)
{
    /// <summary>Dosyaya yazılan / listede gösterilen tek satırlık biçim.</summary>
    public string SatirMetni() =>
        $"{Zaman:dd.MM.yyyy HH:mm:ss.fff}\t{Tur,-8}\t{Tus}\t{Ayrinti}";

    /// <summary>
    /// KeyPress ile gelen karakteri okunabilir hâle getirir.
    /// Kontrol karakterleri (Enter, Tab, Backspace ...) satırı bozmasın diye adıyla yazılır.
    /// </summary>
    public static string KarakterAdi(char c) => c switch
    {
        '\r' => "[Enter]",
        '\n' => "[LF]",
        '\t' => "[Tab]",
        '\b' => "[Backspace]",
        ' ' => "[Boşluk]",
        (char)27 => "[Esc]",
        _ when char.IsControl(c) => $"[Ctrl+{(char)(c + 64)}]",
        _ => c.ToString()
    };
}
