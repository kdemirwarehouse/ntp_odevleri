namespace ChatApp;

internal static class Program
{
    /// <summary>
    /// Giriş noktası.
    ///   ChatApp.exe            → başlangıç penceresi (sunucu / istemci seçimi)
    ///   ChatApp.exe --sunucu   → doğrudan sunucu penceresi
    ///   ChatApp.exe --istemci  → doğrudan istemci penceresi
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        Form anaForm = args.Contains("--sunucu") ? new SunucuForm()
                     : args.Contains("--istemci") ? new IstemciForm()
                     : new BaslangicForm();
        Application.Run(anaForm);
    }
}
