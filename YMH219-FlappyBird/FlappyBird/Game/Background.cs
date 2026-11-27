namespace FlappyBird.Game
{
    /// <summary>
    /// Paralaks (katmanlı) arka plan: uzaktaki bulutlar yavaş, yakındaki tepeler ve şehir
    /// silüeti daha hızlı kayar. Böylece sahnede derinlik hissi oluşur.
    /// Tüm çizimler GDI+ ile yapılır, harici görsel dosya kullanılmaz.
    /// </summary>
    public class Background
    {
        private const float CloudFactor = 0.15f;   // Oyun hızına göre bulut katmanının hızı
        private const float CityFactor = 0.35f;
        private const float HillFactor = 0.6f;

        private readonly int width;
        private readonly int horizon;              // Zeminin başladığı Y
        private readonly (float x, float y, float scale)[] clouds;
        private readonly int[] buildingHeights;

        private float cloudOffset;
        private float cityOffset;
        private float hillOffset;

        public Background(int width, int horizon, int seed = 7)
        {
            this.width = width;
            this.horizon = horizon;

            var random = new Random(seed);
            clouds = Enumerable.Range(0, 5)
                .Select(i => (x: i * (width / 4f) + random.Next(0, 60),
                              y: 40f + random.Next(0, 160),
                              scale: 0.7f + (float)random.NextDouble() * 0.6f))
                .ToArray();
            buildingHeights = Enumerable.Range(0, 16).Select(_ => random.Next(40, 120)).ToArray();
        }

        /// <summary>Katmanları oyun hızıyla orantılı kaydırır.</summary>
        public void Update(float speed)
        {
            cloudOffset = (cloudOffset + speed * CloudFactor) % (width + 120);
            cityOffset = (cityOffset + speed * CityFactor) % (buildingHeights.Length * 40);
            hillOffset = (hillOffset + speed * HillFactor) % 240;
        }

        public void Draw(Graphics g)
        {
            DrawClouds(g);
            DrawCity(g);
            DrawHills(g);
        }

        private void DrawClouds(Graphics g)
        {
            using var brush = new SolidBrush(Color.FromArgb(235, 255, 255, 255));
            float span = width + 120;
            foreach (var (x, y, scale) in clouds)
            {
                float cx = ((x - cloudOffset) % span + span) % span - 60;
                float w = 70 * scale, h = 26 * scale;
                g.FillEllipse(brush, cx, y, w, h);
                g.FillEllipse(brush, cx + w * 0.25f, y - h * 0.45f, w * 0.55f, h * 1.1f);
                g.FillEllipse(brush, cx + w * 0.5f, y - h * 0.2f, w * 0.5f, h * 0.9f);
            }
        }

        private void DrawCity(Graphics g)
        {
            using var building = new SolidBrush(Color.FromArgb(150, 205, 215));
            using var window = new SolidBrush(Color.FromArgb(185, 225, 230));
            for (int i = 0; i < buildingHeights.Length * 2; i++)
            {
                float x = i * 40 - cityOffset;
                if (x > width) break;
                if (x < -40) continue;

                int h = buildingHeights[i % buildingHeights.Length];
                g.FillRectangle(building, x, horizon - 30 - h, 36, h + 30);
                for (int wy = horizon - 22 - h; wy < horizon - 30; wy += 16)
                {
                    g.FillRectangle(window, x + 6, wy, 8, 8);
                    g.FillRectangle(window, x + 22, wy, 8, 8);
                }
            }
        }

        private void DrawHills(Graphics g)
        {
            using var hill = new SolidBrush(Color.FromArgb(120, 200, 110));
            using var dark = new SolidBrush(Color.FromArgb(95, 175, 90));
            for (float x = -hillOffset - 120; x < width + 120; x += 240)
            {
                g.FillEllipse(dark, x + 110, horizon - 50, 200, 100);
                g.FillEllipse(hill, x, horizon - 40, 180, 80);
            }
        }
    }
}
