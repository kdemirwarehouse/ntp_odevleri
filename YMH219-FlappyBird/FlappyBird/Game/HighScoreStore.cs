namespace FlappyBird.Game
{
    /// <summary>
    /// En yüksek skorları zorluk seviyesine göre bir metin dosyasında saklar ve okur.
    /// Dosya: %LOCALAPPDATA%\YMH219-FlappyBird\highscore.txt
    /// Biçim: her satırda "Zorluk=Skor" (ör. "Orta=12").
    /// Eski sürümün yalnızca sayı içeren dosyası "Orta" zorluğunun rekoru olarak okunur.
    /// </summary>
    public class HighScoreStore
    {
        private readonly string filePath;

        public HighScoreStore() : this(null) { }

        /// <param name="filePath">Özel dosya yolu (testler için). null ise varsayılan konum kullanılır.</param>
        public HighScoreStore(string? filePath)
        {
            this.filePath = filePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YMH219-FlappyBird", "highscore.txt");
        }

        /// <summary>Orta zorluğun rekorunu döndürür (geriye dönük uyumluluk).</summary>
        public int Load() => Load(Difficulty.Normal);

        /// <summary>Verilen zorluğun kayıtlı rekorunu döndürür; dosya yoksa veya bozuksa 0.</summary>
        public int Load(Difficulty difficulty) =>
            ReadAll().TryGetValue(difficulty.Name, out int value) ? value : 0;

        /// <summary>Orta zorluk için skoru yazar (geriye dönük uyumluluk).</summary>
        public bool Save(int score) => Save(Difficulty.Normal, score);

        /// <summary>Verilen zorluğun rekorunu yazar; diğer zorlukların rekorları korunur.</summary>
        public bool Save(Difficulty difficulty, int score)
        {
            try
            {
                var scores = ReadAll();
                scores[difficulty.Name] = score;

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllLines(filePath, scores.Select(kv => $"{kv.Key}={kv.Value}"));
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private Dictionary<string, int> ReadAll()
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(filePath))
                    return result;

                foreach (string raw in File.ReadAllLines(filePath))
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;

                    string[] parts = line.Split('=', 2);
                    string name = parts.Length == 2 ? parts[0].Trim() : Difficulty.Normal.Name;
                    string number = parts.Length == 2 ? parts[1] : parts[0];

                    if (Difficulty.FromName(name) != null &&
                        int.TryParse(number.Trim(), out int value) && value >= 0)
                        result[name] = value;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Okunamazsa oyun yine de çalışmaya devam etmeli
            }
            return result;
        }
    }
}
