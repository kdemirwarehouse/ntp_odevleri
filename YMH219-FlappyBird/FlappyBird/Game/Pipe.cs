namespace FlappyBird.Game
{
    /// <summary>
    /// Ekranda sağdan sola hareket eden üst + alt boru çifti (engel).
    /// İki borunun arasındaki boşluktan (Gap) kuşun geçmesi gerekir.
    /// </summary>
    public class Pipe
    {
        public const int PipeWidth = 70;
        public const int GapHeight = 160;
        private const int CapHeight = 22;   // Borunun ağız kısmının yüksekliği
        private const int MinMargin = 60;   // Boşluğun tavan/zemine en az uzaklığı
        private const int MaxGapShift = 170; // Ardışık iki boşluk arasındaki en fazla dikey fark

        public float X { get; private set; }

        /// <summary>Boşluğun üst kenarının Y koordinatı.</summary>
        public int GapTop { get; private set; }

        /// <summary>Kuş bu borudan geçti mi? (Skoru bir kez artırmak için)</summary>
        public bool Passed { get; set; }

        public Pipe(float x, int playAreaHeight, Random random)
        {
            Reposition(x, playAreaHeight, random);
        }

        /// <summary>Üst borunun dikdörtgeni.</summary>
        public Rectangle TopRect => new Rectangle((int)X, 0, PipeWidth, GapTop);

        /// <summary>Alt borunun dikdörtgeni (zemine kadar uzanır).</summary>
        public Rectangle BottomRect(int playAreaHeight) =>
            new Rectangle((int)X, GapTop + GapHeight, PipeWidth,
                          playAreaHeight - (GapTop + GapHeight));

        /// <summary>Boru ekranın solundan tamamen çıktı mı?</summary>
        public bool IsOffScreen => X + PipeWidth < 0;

        /// <summary>Boruyu verilen hızda sola kaydırır.</summary>
        public void Update(float speed)
        {
            X -= speed;
        }

        /// <summary>
        /// Ekran dışına çıkan boruyu yeniden kullanmak için sağa taşır
        /// ve boşluğu rastgele bir yüksekliğe yerleştirir.
        /// </summary>
        /// <param name="previousGapTop">
        /// Önceki borunun boşluk konumu. Verilirse yeni boşluk ona çok uzak olmaz;
        /// böylece hız arttığında bile kuşun yetişemeyeceği bir engel çıkmaz.
        /// </param>
        public void Reposition(float x, int playAreaHeight, Random random, int? previousGapTop = null)
        {
            X = x;
            int minTop = MinMargin;
            int maxTop = Math.Max(minTop + 1, playAreaHeight - GapHeight - MinMargin);

            if (previousGapTop.HasValue)
            {
                minTop = Math.Max(minTop, previousGapTop.Value - MaxGapShift);
                maxTop = Math.Min(maxTop, previousGapTop.Value + MaxGapShift);
            }

            GapTop = random.Next(minTop, maxTop + 1);
            Passed = false;
        }

        /// <summary>Verilen dikdörtgen bu boru çiftinden birine çarpıyor mu?</summary>
        public bool CollidesWith(Rectangle target, int playAreaHeight)
        {
            return target.IntersectsWith(TopRect) ||
                   target.IntersectsWith(BottomRect(playAreaHeight));
        }

        public void Draw(Graphics g, int playAreaHeight)
        {
            DrawPipePart(g, TopRect, capAtBottom: true);
            DrawPipePart(g, BottomRect(playAreaHeight), capAtBottom: false);
        }

        private static void DrawPipePart(Graphics g, Rectangle rect, bool capAtBottom)
        {
            if (rect.Height <= 0) return;

            using var body = new SolidBrush(Color.FromArgb(92, 184, 72));
            using var dark = new SolidBrush(Color.FromArgb(60, 130, 45));
            using var border = new Pen(Color.FromArgb(40, 90, 30), 2);

            // Gövde ve gölge şeridi
            g.FillRectangle(body, rect);
            g.FillRectangle(dark, rect.Right - 14, rect.Y, 10, rect.Height);
            g.DrawRectangle(border, rect);

            // Ağız kısmı (biraz daha geniş)
            int capY = capAtBottom ? rect.Bottom - CapHeight : rect.Y;
            var cap = new Rectangle(rect.X - 5, capY, rect.Width + 10, CapHeight);
            g.FillRectangle(body, cap);
            g.DrawRectangle(border, cap);
        }
    }
}
