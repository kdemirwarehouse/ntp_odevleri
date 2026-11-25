using System.Drawing.Drawing2D;

namespace FlappyBird.Game
{
    /// <summary>
    /// Oyuncunun kontrol ettiği kuş.
    /// Yer çekimi ile aşağı iner, Flap() çağrıldığında yukarı sıçrar.
    /// </summary>
    public class Bird
    {
        // Fizik sabitleri
        private const float Gravity = 0.55f;       // Her karede hıza eklenen yer çekimi
        private const float FlapStrength = -8.5f;  // Zıplama anında verilen yukarı hız
        private const float MaxFallSpeed = 12f;    // Düşüş hızının üst sınırı

        private readonly float startX;
        private readonly float startY;

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Velocity { get; private set; }
        public int Width { get; } = 38;
        public int Height { get; } = 28;

        /// <summary>
        /// Çarpışma kontrolünde kullanılan dikdörtgen.
        /// Kenarlardan biraz küçültülerek oyuncuya karşı daha adil hale getirildi.
        /// </summary>
        public Rectangle Bounds =>
            new Rectangle((int)X + 4, (int)Y + 4, Width - 8, Height - 8);

        public Bird(float startX, float startY)
        {
            this.startX = startX;
            this.startY = startY;
            Reset();
        }

        /// <summary>Kuşu başlangıç konumuna ve hızına döndürür.</summary>
        public void Reset()
        {
            X = startX;
            Y = startY;
            Velocity = 0;
        }

        /// <summary>Kuşu yukarı doğru sıçratır.</summary>
        public void Flap()
        {
            Velocity = FlapStrength;
        }

        /// <summary>
        /// Her oyun karesinde çağrılır: yer çekimini uygular ve konumu günceller.
        /// </summary>
        public void Update()
        {
            Velocity = Math.Min(Velocity + Gravity, MaxFallSpeed);
            Y += Velocity;

            // Tavanın dışına çıkmasın
            if (Y < 0)
            {
                Y = 0;
                Velocity = 0;
            }
        }

        /// <summary>Kuşu, hızına göre hafifçe eğik olarak çizer.</summary>
        public void Draw(Graphics g)
        {
            float angle = Math.Clamp(Velocity * 4f, -25f, 70f);
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            GraphicsState state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(angle);
            g.TranslateTransform(-Width / 2f, -Height / 2f);

            // Gövde
            using (var body = new SolidBrush(Color.Gold))
                g.FillEllipse(body, 0, 0, Width, Height);
            using (var outline = new Pen(Color.DarkGoldenrod, 2))
                g.DrawEllipse(outline, 0, 0, Width, Height);

            // Kanat
            using (var wing = new SolidBrush(Color.Orange))
                g.FillEllipse(wing, 4, Height / 2f - 2, 16, 10);

            // Göz
            g.FillEllipse(Brushes.White, Width - 16, 4, 11, 11);
            g.FillEllipse(Brushes.Black, Width - 11, 7, 5, 5);

            // Gaga
            PointF[] beak =
            {
                new PointF(Width - 4, Height / 2f - 3),
                new PointF(Width + 8, Height / 2f + 1),
                new PointF(Width - 4, Height / 2f + 5)
            };
            using (var beakBrush = new SolidBrush(Color.OrangeRed))
                g.FillPolygon(beakBrush, beak);

            g.Restore(state);
        }
    }
}
