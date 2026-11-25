using FaceRecognition.Core;

namespace FaceRecognition;

internal static class Program
{
    /// <summary>
    /// Giriş noktası. İsteğe bağlı parametreler:
    ///   --ornekler &lt;klasör&gt;   örneklerin saklanacağı klasör (varsayılan %LOCALAPPDATA%\YMH219-FaceRecognition\ornekler)
    ///   --resim &lt;dosya&gt;       açılışta bir görüntü dosyasını işler
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        string ornekler = ArgumanDegeri(args, "--ornekler") ?? OrnekDeposu.VarsayilanKlasor();
        var form = new MainForm(ornekler);

        if (ArgumanDegeri(args, "--resim") is string resim)
            form.Shown += (_, _) => form.GoruntuDosyasiAc(resim);

        Application.Run(form);
    }

    private static string? ArgumanDegeri(string[] args, string ad)
    {
        int i = Array.IndexOf(args, ad);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
