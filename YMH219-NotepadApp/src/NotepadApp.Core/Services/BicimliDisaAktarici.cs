using System.Text;
using NotepadApp.Core.Models;

namespace NotepadApp.Core.Services;

/// <summary>
/// Notu biçimlendirmesini (kalın / italik / altı çizili) koruyarak dışa aktarır:
/// <list type="bullet">
/// <item><b>.rtf</b> — RichTextBox'ın RTF içeriği aynen yazılır; Word / WordPad ile açılır.</item>
/// <item><b>.html</b> — RTF, küçük bir ayrıştırıcıyla &lt;b&gt;, &lt;i&gt;, &lt;u&gt; etiketlerine çevrilir; tarayıcıda açılır.</item>
/// </list>
/// </summary>
public static class BicimliDisaAktarici
{
    public static void RtfOlarakAktar(Not not, string dosyaYolu)
    {
        string rtf = string.IsNullOrWhiteSpace(not.RtfIcerik) ? DuzMetindenRtf(not.Icerik) : not.RtfIcerik!;
        // RTF 7-bit ASCII'dir; Türkçe karakterler zaten \'xx / \uN kaçışlarıyla içindedir.
        File.WriteAllText(dosyaYolu, rtf, Encoding.ASCII);
    }

    public static void HtmlOlarakAktar(Not not, string dosyaYolu)
    {
        string govde = string.IsNullOrWhiteSpace(not.RtfIcerik)
            ? Kacis(not.Icerik).Replace("\n", "<br>\n")
            : RtfdenHtml(not.RtfIcerik!);
        string baslik = Kacis(not.Baslik);
        string html = $"""
            <!doctype html>
            <html lang="tr">
            <head>
            <meta charset="utf-8">
            <title>{baslik}</title>
            <style>
              body {"{"} font-family: "Segoe UI", system-ui, sans-serif; max-width: 760px; margin: 40px auto; line-height: 1.55; color: #1f2328; {"}"}
              .bilgi {"{"} color: #656d76; font-size: 13px; border-bottom: 1px solid #d0d7de; padding-bottom: 8px; margin-bottom: 16px; {"}"}
            </style>
            </head>
            <body>
            <h1>{baslik}</h1>
            <div class="bilgi">Kategori: {Kacis(not.Kategori)} · Oluşturulma: {not.OlusturmaTarihi:dd.MM.yyyy HH:mm} · Son düzenleme: {not.DuzenlemeTarihi:dd.MM.yyyy HH:mm}</div>
            <div>{govde}</div>
            </body>
            </html>
            """;
        File.WriteAllText(dosyaYolu, html, new UTF8Encoding(false));
    }

    /// <summary>Yalnızca HTML'de özel anlamı olan karakterleri kaçışlar (Türkçe harfler okunur kalır).</summary>
    public static string Kacis(string metin) =>
        metin.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>Düz metni en basit RTF belgesine çevirir (Türkçe karakterler \uN ile).</summary>
    public static string DuzMetindenRtf(string metin)
    {
        var sb = new StringBuilder(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\f0\fs22 ");
        foreach (char c in metin)
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case '{': sb.Append(@"\{"); break;
                case '}': sb.Append(@"\}"); break;
                case '\n': sb.Append(@"\par "); break;
                case '\r': break;
                default:
                    if (c > 127) sb.Append(@"\u").Append((short)c).Append('?');
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('}').ToString();
    }

    /// <summary>
    /// RTF'yi basit HTML'e çevirir. Desteklenen: \b \i \ul (ve kapatan 0 biçimleri), \par \line \tab,
    /// \'xx (Windows-1254 Türkçe kod sayfası), \uN Unicode, gruplar ve yok sayılan hedefler (\fonttbl, \colortbl, \*).
    /// </summary>
    public static string RtfdenHtml(string rtf)
    {
        Encoding kodSayfasi;
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); kodSayfasi = Encoding.GetEncoding(1254); }
        catch (Exception) { kodSayfasi = Encoding.Latin1; }

        var cikti = new StringBuilder();
        var yigin = new Stack<(bool B, bool I, bool U, bool Atla)>();
        bool b = false, i = false, u = false, atla = false;
        bool acikB = false, acikI = false, acikU = false;
        int unicodeAtla = 0;

        void BicimiEsitle()
        {
            // Kapanış sırası: önce iç etiketler
            if (acikU && !u) { cikti.Append("</u>"); acikU = false; }
            if (acikI && !i) { cikti.Append("</i>"); acikI = false; }
            if (acikB && !b) { cikti.Append("</b>"); acikB = false; }
            if (b && !acikB) { cikti.Append("<b>"); acikB = true; }
            if (i && !acikI) { cikti.Append("<i>"); acikI = true; }
            if (u && !acikU) { cikti.Append("<u>"); acikU = true; }
        }
        void Yaz(string metin)
        {
            if (atla || metin.Length == 0) return;
            BicimiEsitle();
            cikti.Append(Kacis(metin));
        }

        for (int k = 0; k < rtf.Length; k++)
        {
            char c = rtf[k];
            if (c == '{') { yigin.Push((b, i, u, atla)); continue; }
            if (c == '}') { if (yigin.Count > 0) (b, i, u, atla) = yigin.Pop(); continue; }
            if (c == '\r' || c == '\n') continue;
            if (c != '\\') { if (unicodeAtla > 0) { unicodeAtla--; continue; } Yaz(c.ToString()); continue; }

            // Kontrol sözcüğü / sembolü
            if (k + 1 >= rtf.Length) break;
            char s = rtf[k + 1];
            if (s is '\\' or '{' or '}') { Yaz(s.ToString()); k++; continue; }
            if (s == '*') { atla = true; k++; continue; }
            if (s == '\'')
            {
                if (k + 3 < rtf.Length && int.TryParse(rtf.AsSpan(k + 2, 2), System.Globalization.NumberStyles.HexNumber, null, out int hex))
                {
                    if (unicodeAtla > 0) unicodeAtla--;
                    else Yaz(kodSayfasi.GetString(new[] { (byte)hex }));
                }
                k += 3;
                continue;
            }
            if (!char.IsLetter(s)) { k++; continue; }

            int bas = k + 1, son = bas;
            while (son < rtf.Length && char.IsLetter(rtf[son])) son++;
            string kelime = rtf[bas..son];
            int sayiBas = son;
            if (son < rtf.Length && (rtf[son] == '-' || char.IsDigit(rtf[son]))) { son++; while (son < rtf.Length && char.IsDigit(rtf[son])) son++; }
            int? sayi = son > sayiBas && int.TryParse(rtf.AsSpan(sayiBas, son - sayiBas), out int n) ? n : null;
            if (son < rtf.Length && rtf[son] == ' ') son++;   // ayırıcı boşluk
            k = son - 1;

            switch (kelime)
            {
                case "b": b = sayi != 0; break;
                case "i": i = sayi != 0; break;
                case "ul": u = sayi != 0; break;
                case "ulnone": u = false; break;
                case "plain": b = i = u = false; break;
                case "par": case "line": if (!atla) { BicimiEsitle(); cikti.Append("<br>\n"); } break;
                case "tab": Yaz("\t"); break;
                case "u":
                    if (sayi is int kod) { Yaz(((char)(kod < 0 ? kod + 65536 : kod)).ToString()); unicodeAtla = 1; }
                    break;
                case "fonttbl": case "colortbl": case "stylesheet": case "info": case "pict": case "generator":
                    atla = true; break;
            }
        }
        b = i = u = false;
        BicimiEsitle();
        string html = cikti.ToString();
        while (html.EndsWith("<br>\n")) html = html[..^5];   // RichTextBox'ın sondaki boş paragrafı
        return html;
    }
}
