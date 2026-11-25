using OpenCvSharp;

namespace FaceRecognition.Services;

/// <summary>
/// Kameradan arka plan görevinde kare okur ve her kareyi <see cref="KareGeldi"/> olayıyla verir.
/// Olay arka plan iş parçacığından tetiklenir; Mat olay işleyicisi döndükten sonra serbest bırakılır.
/// </summary>
public sealed class KameraServisi : IDisposable
{
    private CancellationTokenSource? _iptal;
    private Task? _gorev;

    public event EventHandler<Mat>? KareGeldi;
    public event EventHandler<string>? Hata;

    public bool Calisiyor => _gorev is { IsCompleted: false };

    /// <summary>Kamerayı açar. Kamera yoksa veya başka uygulama kullanıyorsa InvalidOperationException fırlatır.</summary>
    public void Baslat(int kameraNo = 0)
    {
        if (Calisiyor)
            return;

        var kamera = new VideoCapture(kameraNo, VideoCaptureAPIs.DSHOW);
        if (!kamera.IsOpened())
        {
            kamera.Dispose();
            throw new InvalidOperationException(
                $"Kamera ({kameraNo}) açılamadı. Bir kameranın bağlı olduğundan, başka bir uygulama tarafından " +
                "kullanılmadığından ve Windows Gizlilik ayarlarında kamera erişiminin açık olduğundan emin olun.");
        }
        kamera.Set(VideoCaptureProperties.FrameWidth, 640);
        kamera.Set(VideoCaptureProperties.FrameHeight, 480);

        _iptal = new CancellationTokenSource();
        var iptal = _iptal.Token;
        _gorev = Task.Run(() => Dongu(kamera, iptal));
    }

    private void Dongu(VideoCapture kamera, CancellationToken iptal)
    {
        using (kamera)
        using (var kare = new Mat())
        {
            int bosKare = 0;
            while (!iptal.IsCancellationRequested)
            {
                if (!kamera.Read(kare) || kare.Empty())
                {
                    if (++bosKare > 50)
                    {
                        Hata?.Invoke(this, "Kameradan görüntü alınamıyor; kamera bağlantısı kesilmiş olabilir.");
                        return;
                    }
                    Thread.Sleep(20);
                    continue;
                }
                bosKare = 0;
                KareGeldi?.Invoke(this, kare);
            }
        }
    }

    public void Durdur()
    {
        _iptal?.Cancel();
        try { _gorev?.Wait(2000); } catch (AggregateException) { }
        _gorev = null;
    }

    public void Dispose() => Durdur();
}
