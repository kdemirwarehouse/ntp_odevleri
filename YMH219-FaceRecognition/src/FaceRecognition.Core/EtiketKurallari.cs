namespace FaceRecognition.Core;

/// <summary>Kişi etiketlerinin (ör. "Ali Yılmaz") doğrulanması. Etiket aynı zamanda klasör adıdır.</summary>
public static class EtiketKurallari
{
    public const int MaksimumUzunluk = 30;

    /// <summary>Etiket geçersizse hata mesajı, geçerliyse null döner.</summary>
    public static string? Hata(string? etiket)
    {
        if (string.IsNullOrWhiteSpace(etiket))
            return "Etiket (kişi adı) boş olamaz.";
        etiket = etiket.Trim();
        if (etiket.Length > MaksimumUzunluk)
            return $"Etiket en fazla {MaksimumUzunluk} karakter olabilir.";
        if (etiket.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || etiket.Contains('.'))
            return "Etiket şu karakterleri içeremez: \\ / : * ? \" < > | .";
        if (etiket.Equals(TanimaKarari.Bilinmiyor, StringComparison.OrdinalIgnoreCase))
            return $"\"{TanimaKarari.Bilinmiyor}\" ayrılmış bir etikettir.";
        return null;
    }

    /// <summary>Geçerli etiketi kırpılmış hâliyle döner, geçersizse ArgumentException fırlatır.</summary>
    public static string Normallestir(string? etiket)
    {
        string? hata = Hata(etiket);
        if (hata is not null)
            throw new ArgumentException(hata, nameof(etiket));
        return etiket!.Trim();
    }
}
