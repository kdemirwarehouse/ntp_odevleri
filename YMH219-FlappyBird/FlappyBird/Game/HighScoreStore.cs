namespace FlappyBird.Game
{
    /// <summary>
    /// En yüksek skoru bir metin dosyasında saklar ve okur.
    /// Dosya: %LOCALAPPDATA%\YMH219-FlappyBird\highscore.txt
    /// </summary>
    public class HighScoreStore
    {
        private readonly string filePath;

        public HighScoreStore()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YMH219-FlappyBird");
            filePath = Path.Combine(folder, "highscore.txt");
        }

        /// <summary>Kayıtlı en yüksek skoru döndürür; dosya yoksa veya bozuksa 0.</summary>
        public int Load()
        {
            try
            {
                if (!File.Exists(filePath))
                    return 0;

                string text = File.ReadAllText(filePath).Trim();
                return int.TryParse(text, out int value) && value >= 0 ? value : 0;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Okunamazsa oyun yine de çalışmaya devam etmeli
                return 0;
            }
        }

        /// <summary>Skoru dosyaya yazar. Başarılıysa true döner.</summary>
        public bool Save(int score)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllText(filePath, score.ToString());
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
