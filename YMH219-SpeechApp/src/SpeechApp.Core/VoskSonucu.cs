using System.Text.Json;

namespace SpeechApp.Core;

/// <summary>
/// Vosk tanıyıcısının JSON çıktısını ayrıştırır.
/// <para>Tam sonuç: <c>{"result":[{"conf":0.98,"word":"merhaba",...}], "text":"merhaba dünya"}</c></para>
/// <para>Ara sonuç: <c>{"partial":"merhaba dün"}</c></para>
/// Güven skoru, kelime güvenlerinin ortalamasıdır (kelime bilgisi yoksa 1).
/// </summary>
public static class VoskSonucu
{
    public static (string Metin, float Guven) TamSonuc(string json)
    {
        try
        {
            using var belge = JsonDocument.Parse(json);
            var kok = belge.RootElement;
            string metin = kok.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            float guven = 1f;
            if (kok.TryGetProperty("result", out var kelimeler) && kelimeler.ValueKind == JsonValueKind.Array && kelimeler.GetArrayLength() > 0)
                guven = (float)kelimeler.EnumerateArray()
                                       .Select(k => k.TryGetProperty("conf", out var c) ? c.GetDouble() : 1.0)
                                       .Average();
            return (metin.Trim(), Math.Clamp(guven, 0f, 1f));
        }
        catch (JsonException)
        {
            return ("", 0f);
        }
    }

    public static string AraSonuc(string json)
    {
        try
        {
            using var belge = JsonDocument.Parse(json);
            return belge.RootElement.TryGetProperty("partial", out var p) ? (p.GetString() ?? "").Trim() : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    /// <summary>16-bit PCM tampondaki en yüksek genliği 0-100 ölçeğine çevirir (mikrofon seviyesi çubuğu için).</summary>
    public static int SesSeviyesi(byte[] tampon, int uzunluk)
    {
        int enYuksek = 0;
        for (int i = 0; i + 1 < uzunluk; i += 2)
        {
            int ornek = Math.Abs((int)BitConverter.ToInt16(tampon, i));
            if (ornek > enYuksek) enYuksek = ornek;
        }
        return Math.Min(100, enYuksek * 100 / 32768);
    }
}
