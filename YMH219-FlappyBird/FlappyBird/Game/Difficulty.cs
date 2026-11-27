namespace FlappyBird.Game
{
    /// <summary>
    /// Zorluk seviyesi ve ona bağlı oyun ayarları.
    /// Kolayda boşluk geniş ve oyun yavaş, zorda boşluk dar ve borular sık gelir.
    /// </summary>
    public sealed class Difficulty
    {
        public string Name { get; }
        public int GapHeight { get; }        // Üst ve alt boru arasındaki boşluk
        public float BaseSpeed { get; }      // Başlangıç hızı
        public float SpeedStep { get; }      // Her seviyede eklenen hız
        public float MaxSpeed { get; }
        public int PipeSpacing { get; }      // Borular arası yatay mesafe

        private Difficulty(string name, int gapHeight, float baseSpeed, float speedStep, float maxSpeed, int pipeSpacing)
        {
            Name = name;
            GapHeight = gapHeight;
            BaseSpeed = baseSpeed;
            SpeedStep = speedStep;
            MaxSpeed = maxSpeed;
            PipeSpacing = pipeSpacing;
        }

        public static readonly Difficulty Easy = new("Kolay", 190, 3.2f, 0.4f, 6f, 270);
        public static readonly Difficulty Normal = new("Orta", 160, 4f, 0.6f, 7.5f, 250);
        public static readonly Difficulty Hard = new("Zor", 135, 4.8f, 0.7f, 8.5f, 225);

        public static IReadOnlyList<Difficulty> All { get; } = new[] { Easy, Normal, Hard };

        /// <summary>Ada göre zorluğu bulur (büyük/küçük harf duyarsız); bulunamazsa null.</summary>
        public static Difficulty? FromName(string name) =>
            All.FirstOrDefault(d => string.Equals(d.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

        public override string ToString() => Name;
    }
}
