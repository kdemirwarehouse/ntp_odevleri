namespace SpeechApp;

internal static class Program
{
    /// <summary>
    /// Giriş noktası. İsteğe bağlı:
    ///   SpeechApp.exe --wav ornek.wav     (açılışta verilen ses dosyasını metne çevirir)
    ///   SpeechApp.exe --motor vosk        (Türkçe Vosk tanıyıcısıyla başlar)
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var form = new MainForm();

        int m = Array.IndexOf(args, "--motor");
        if (m >= 0 && m + 1 < args.Length && Enum.TryParse<SpeechApp.Core.TanimaMotoru>(args[m + 1], true, out var motor))
            form.Shown += (_, _) => form.MotorSec(motor);

        int i = Array.IndexOf(args, "--wav");
        if (i >= 0 && i + 1 < args.Length)
        {
            string yol = args[i + 1];
            form.Shown += (_, _) => form.DosyadanTani(yol);
        }

        Application.Run(form);
    }
}
