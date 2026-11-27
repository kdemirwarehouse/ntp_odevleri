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
        public const float Gravity = 0.55f;       // Her karede hıza eklenen yer çekimi
        public const float FlapStrength = -8.5f;  // Zıplama anında verilen yukarı hız
        public const float MaxFallSpeed = 12f;    // Düşüş hızının üst sınırı

        private readonly float startX;
        private readonly float startY;

        // Animasyon durumu
        private float wingPhase;     // Kanat çırpma döngüsünün açısı (radyan)
        private float hoverPhase;    // Hazır ekranında süzülme hareketi

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Velocity { get; private set; }
        public int Width { get; } = 38;
        public int Height { get; } = 28;

        /// <summary>
        /// Kuşu kaba olarak çevreleyen dikdörtgen (ön kontrol için).
        /// </summary>
        public Rectangle Bounds =>
            new Rectangle((int)X, (int)Y, Width, Height);

        /// <summary>Kuşun gövdesini temsil eden elipsin merkezi ve yarıçapları.</summary>
        public float CenterX => X + Width / 2f;
        public float CenterY => Y + Height / 2f;
        public float RadiusX => Width / 2f - 1f;
        public float RadiusY => Height / 2f - 1f;

        /// <summary>
        /// Kuşun elips şeklindeki gövdesi verilen dikdörtgene değiyor mu?
        /// Dikdörtgen (AABB) yerine elips kullanıldığı için köşelerdeki boşluk
        /// artık çarpışma sayılmaz. Hesap: koordinatlar yarıçaplara bölünerek elips
        /// birim çembere dönüştürülür, sonra çember-dikdörtgen testi yapılır.
        /// </summary>
        public bool Intersects(RectangleF rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return false;

            // Dikdörtgen üzerinde elips merkezine en yakın nokta
            float nearestX = Math.Clamp(CenterX, rect.Left, rect.Right);
            float nearestY = Math.Clamp(CenterY, rect.Top, rect.Bottom);

            float dx = (nearestX - CenterX) / RadiusX;
            float dy = (nearestY - CenterY) / RadiusY;
            return dx * dx + dy * dy <= 1f;
        }

        /// <summary>Test ve özel durumlar için kuşu doğrudan konumlandırır.</summary>
        public void SetPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

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
            wingPhase = 0;
            hoverPhase = 0;
        }

        /// <summary>
        /// Her karede çağrılan animasyon adımı. Kanat, kuş yükselirken hızlı,
        /// düşerken yavaş çırpar. <paramref name="hovering"/> true ise (Hazır ekranı)
        /// kuş başlangıç yüksekliğinde hafifçe aşağı yukarı süzülür.
        /// </summary>
        public void Animate(bool hovering)
        {
            wingPhase += Velocity < 0 || hovering ? 0.45f : 0.18f;

            if (hovering)
            {
                hoverPhase += 0.08f;
                Y = startY + MathF.Sin(hoverPhase) * 6f;
            }
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

            // Kanat: sinüs dalgasıyla yukarı-aşağı çırpar (3 kareli sprite yerine sürekli animasyon)
            float flap = MathF.Sin(wingPhase);
            float wingHeight = 6 + 6 * MathF.Abs(flap);
            float wingY = Height / 2f - 2 - flap * 7f;
            using (var wing = new SolidBrush(Color.Orange))
                g.FillEllipse(wing, 3, wingY, 18, wingHeight);
            using (var wingEdge = new Pen(Color.DarkOrange, 1.5f))
                g.DrawEllipse(wingEdge, 3, wingY, 18, wingHeight);

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
