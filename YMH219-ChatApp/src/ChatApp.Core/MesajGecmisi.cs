using System.Text;
using System.Text.Json;

namespace ChatApp.Core;

/// <summary>
/// Sunucunun herkese açık mesaj geçmişi. Son <see cref="Kapasite"/> mesaj bellekte tutulur;
/// dosya yolu verilirse her mesaj JSON Lines (satır başına bir JSON) biçiminde dosyaya eklenir
/// ve sunucu yeniden açıldığında geçmiş dosyadan yüklenir.
/// <para>Gizlilik gereği özel mesajlar geçmişe yazılmaz.</para>
/// </summary>
public sealed class MesajGecmisi
{
    private readonly LinkedList<ChatMesaji> _mesajlar = new();
    private readonly object _kilit = new();
    private readonly string? _dosya;

    public MesajGecmisi(string? dosyaYolu = null, int kapasite = 100)
    {
        if (kapasite <= 0)
            throw new ArgumentOutOfRangeException(nameof(kapasite));
        _dosya = dosyaYolu;
        Kapasite = kapasite;
        Yukle();
    }

    public int Kapasite { get; }
    public string? DosyaYolu => _dosya;

    /// <summary>Varsayılan geçmiş dosyası: %LOCALAPPDATA%\YMH219-ChatApp\sohbet-gecmisi.jsonl</summary>
    public static string VarsayilanDosya => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YMH219-ChatApp", "sohbet-gecmisi.jsonl");

    public int Sayi { get { lock (_kilit) return _mesajlar.Count; } }

    /// <summary>Geçmişe uygun mesajları (Sohbet ve Sistem) ekler.</summary>
    public void Ekle(ChatMesaji mesaj)
    {
        if (mesaj.Tur is not (MesajTuru.Sohbet or MesajTuru.Sistem))
            return;

        var kayit = mesaj with { Gecmis = null, Kullanicilar = null, Odalar = null, Oda = mesaj.Oda ?? ChatMesaji.VarsayilanOda };
        lock (_kilit)
        {
            _mesajlar.AddLast(kayit);
            while (_mesajlar.Count > Kapasite)
                _mesajlar.RemoveFirst();

            if (_dosya is null)
                return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_dosya)!);
                File.AppendAllText(_dosya, kayit.JsonSatiri() + "\n", new UTF8Encoding(false));
                // Dosya kapasitenin çok üstüne çıktıysa yalnızca son mesajlarla yeniden yaz
                if (new FileInfo(_dosya).Length > 512 * 1024)
                    File.WriteAllLines(_dosya, _mesajlar.Select(m => m.JsonSatiri()), new UTF8Encoding(false));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Disk hatası sohbeti durdurmamalı; geçmiş bellekte tutulmaya devam eder
            }
        }
    }

    /// <summary>Yeni katılan istemciye gönderilecek son mesajlar (eskiden yeniye). Oda verilirse yalnızca o odanınkiler.</summary>
    public List<ChatMesaji> SonMesajlar(int adet = 50, string? oda = null)
    {
        lock (_kilit)
        {
            var secili = _mesajlar.Where(m => oda is null ||
                string.Equals(m.Oda ?? ChatMesaji.VarsayilanOda, oda, StringComparison.OrdinalIgnoreCase)).ToList();
            return secili.Skip(Math.Max(0, secili.Count - adet)).Select(m => m with { Gecmis = true }).ToList();
        }
    }

    public void Temizle()
    {
        lock (_kilit)
        {
            _mesajlar.Clear();
            if (_dosya is not null)
            {
                try { File.Delete(_dosya); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    private void Yukle()
    {
        if (_dosya is null || !File.Exists(_dosya))
            return;
        try
        {
            foreach (string satir in File.ReadLines(_dosya))
            {
                if (string.IsNullOrWhiteSpace(satir)) continue;
                try
                {
                    _mesajlar.AddLast(ChatMesaji.JsondanOku(satir));
                    if (_mesajlar.Count > Kapasite) _mesajlar.RemoveFirst();
                }
                catch (JsonException) { /* bozuk satır atlanır */ }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
