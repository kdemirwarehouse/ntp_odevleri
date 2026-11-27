namespace SpeechApp.Core;

/// <summary>Uygulamada o an etkin olan işlem. Arayüzdeki gösterge bu değere göre çizilir.</summary>
public enum SesDurumu
{
    Hazir,
    Okunuyor,
    Duraklatildi,
    Dinleniyor,
    DosyaTaniniyor
}

/// <summary>Metin okuma motoru: klasik masaüstü SAPI (System.Speech) veya Windows 10+ OneCore (WinRT).</summary>
public enum SesMotoru
{
    Sapi,
    OneCore
}

/// <summary>Konuşma tanıma motoru: Windows SAPI (kurulu dil, genelde en-US) veya Vosk (çevrimdışı Türkçe model).</summary>
public enum TanimaMotoru
{
    Sapi,
    Vosk
}

public static class SesDurumuMetni
{
    public static string Aciklama(this SesDurumu durum) => durum switch
    {
        SesDurumu.Okunuyor => "🔊 Metin okunuyor (Text-to-Speech)",
        SesDurumu.Duraklatildi => "⏸ Okuma duraklatıldı",
        SesDurumu.Dinleniyor => "🎤 Mikrofon dinleniyor (Speech-to-Text)",
        SesDurumu.DosyaTaniniyor => "📄 Ses dosyası metne çevriliyor",
        _ => "Hazır"
    };
}
