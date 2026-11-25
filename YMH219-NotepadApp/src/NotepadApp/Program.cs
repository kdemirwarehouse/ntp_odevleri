using NotepadApp.Core.Services;

namespace NotepadApp;

internal static class Program
{
    /// <summary>
    /// Uygulamanın giriş noktası.
    /// İsteğe bağlı olarak farklı bir not dosyası verilebilir:
    ///   NotepadApp.exe --veri C:\yol\notlar.json
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        string veriDosyasi = NotDeposu.VarsayilanYol();
        int indeks = Array.IndexOf(args, "--veri");
        if (indeks >= 0 && indeks + 1 < args.Length)
            veriDosyasi = args[indeks + 1];

        Application.Run(new MainForm(veriDosyasi));
    }
}
