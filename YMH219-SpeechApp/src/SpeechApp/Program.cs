namespace SpeechApp;

internal static class Program
{
    /// <summary>
    /// Giriş noktası. İsteğe bağlı: SpeechApp.exe --wav ornek.wav
    /// (açılışta verilen ses dosyasını metne çevirir).
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var form = new MainForm();

        int i = Array.IndexOf(args, "--wav");
        if (i >= 0 && i + 1 < args.Length)
        {
            string yol = args[i + 1];
            form.Shown += (_, _) => form.DosyadanTani(yol);
        }

        Application.Run(form);
    }
}
