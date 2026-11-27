using System.Net.Sockets;
using System.Text;

namespace ChatApp.Core;

/// <summary>
/// Bir TcpClient üzerinde satır tabanlı JSON mesajlaşması yapar.
/// NetworkStream, StreamReader ve StreamWriter ile sarılır.
/// Aynı anda iki yerden yazılırsa satırlar karışmasın diye yazma işlemi SemaphoreSlim ile kilitlenir.
/// </summary>
public sealed class JsonSatirBaglantisi : IDisposable
{
    /// <summary>Kötü niyetli / hatalı çok uzun satırlara karşı sınır.</summary>
    public const int MaksimumSatirUzunlugu = 16 * 1024;

    private readonly TcpClient _tcp;
    private readonly StreamReader _okuyucu;
    private readonly StreamWriter _yazici;
    private readonly SemaphoreSlim _yazmaKilidi = new(1, 1);
    private bool _kapandi;

    public JsonSatirBaglantisi(TcpClient tcp) : this(tcp, null) { }

    /// <param name="akis">TLS kullanılıyorsa SslStream; null ise doğrudan NetworkStream kullanılır.</param>
    public JsonSatirBaglantisi(TcpClient tcp, Stream? akis)
    {
        _tcp = tcp ?? throw new ArgumentNullException(nameof(tcp));
        akis ??= tcp.GetStream();
        Sifreli = akis is System.Net.Security.SslStream;
        var utf8 = new UTF8Encoding(false);
        _okuyucu = new StreamReader(akis, utf8);
        _yazici = new StreamWriter(akis, utf8) { AutoFlush = true, NewLine = "\n" };
        UzakAdres = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
    }

    public string UzakAdres { get; }

    /// <summary>Bağlantı TLS ile şifreli mi?</summary>
    public bool Sifreli { get; }

    public async Task GonderAsync(ChatMesaji mesaj, CancellationToken iptal = default)
    {
        await _yazmaKilidi.WaitAsync(iptal).ConfigureAwait(false);
        try
        {
            if (_kapandi)
                throw new IOException("Bağlantı kapalı.");
            await _yazici.WriteLineAsync(mesaj.JsonSatiri().AsMemory(), iptal).ConfigureAwait(false);
        }
        finally
        {
            _yazmaKilidi.Release();
        }
    }

    /// <summary>Bir sonraki mesajı okur. Karşı taraf bağlantıyı kapattıysa null döner.</summary>
    public async Task<ChatMesaji?> OkuAsync(CancellationToken iptal = default)
    {
        string? satir = await _okuyucu.ReadLineAsync(iptal).ConfigureAwait(false);
        if (satir is null)
            return null;
        if (satir.Length > MaksimumSatirUzunlugu)
            throw new InvalidDataException("Mesaj çok uzun.");
        return ChatMesaji.JsondanOku(satir);
    }

    public void Dispose()
    {
        if (_kapandi)
            return;
        _kapandi = true;
        try { _tcp.Client.Shutdown(SocketShutdown.Both); } catch { /* zaten kapanmış olabilir */ }
        _tcp.Close();
        _okuyucu.Dispose();
        try { _yazici.Dispose(); } catch (IOException) { /* kapanmış akış */ } catch (ObjectDisposedException) { }
    }
}
