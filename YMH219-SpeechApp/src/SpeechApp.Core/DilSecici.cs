using System.Globalization;

namespace SpeechApp.Core;

/// <summary>
/// Kurulu konuşma tanıyıcıları / sesler arasından en uygun dili seçer.
/// Sıra: tam eşleşme (tr-TR) → aynı dil (tr) → en-US → ilk kurulu olan.
/// Arayüz ve System.Speech'ten bağımsız olduğu için test edilebilir.
/// </summary>
public static class DilSecici
{
    public static string? EnUygun(IEnumerable<string> kuruluKulturler, string tercih)
    {
        var liste = kuruluKulturler.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
        if (liste.Count == 0)
            return null;

        string? tam = liste.FirstOrDefault(k => k.Equals(tercih, StringComparison.OrdinalIgnoreCase));
        if (tam is not null)
            return tam;

        string dil = tercih.Split('-')[0];
        string? ayniDil = liste.FirstOrDefault(k => k.Split('-')[0].Equals(dil, StringComparison.OrdinalIgnoreCase));
        if (ayniDil is not null)
            return ayniDil;

        return liste.FirstOrDefault(k => k.Equals("en-US", StringComparison.OrdinalIgnoreCase)) ?? liste[0];
    }

    /// <summary>"en-US" → "English (United States)" gibi okunabilir ad.</summary>
    public static string GorunenAd(string kultur)
    {
        try { return $"{CultureInfo.GetCultureInfo(kultur).NativeName} ({kultur})"; }
        catch (CultureNotFoundException) { return kultur; }
    }
}
