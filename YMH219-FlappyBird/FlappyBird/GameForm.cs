using FlappyBird.Game;

namespace FlappyBird
{
    /// <summary>
    /// Ana oyun penceresi.
    /// Görevleri: klavye olaylarını almak, Timer ile oyun döngüsünü çalıştırmak,
    /// oyunu çizmek ve durum bilgilerini (Label'lar) güncellemek.
    /// Oyun kuralları GameManager sınıfındadır.
    /// </summary>
    public partial class GameForm : Form
    {
        private const int GroundHeight = 60;

        // Oyun her zaman bu mantıksal çözünürlükte hesaplanır ve çizilir;
        // pencere büyüyünce/küçülünce görüntü en-boy oranı korunarak ölçeklenir.
        public const int LogicalWidth = 480;
        public const int LogicalHeight = 680;

        private float viewScale = 1f;
        private PointF viewOffset;
        private FormWindowState windowStateBeforeFullScreen;
        private bool fullScreen;

        // Sabit adımlı döngü: oyun mantığı her zaman saniyede 60 adım ilerler.
        // Timer geç tetiklense bile geçen gerçek süre kadar adım çalıştırılır.
        private const double StepSeconds = 1.0 / 60.0;
        private const int MaxStepsPerTick = 5;   // Uzun donmalarda "ışınlanmayı" sınırlar

        private GameManager game = null!;
        private readonly SoundEffects sounds = new SoundEffects();
        private float groundOffset;   // Zeminin kayma animasyonu için
        private Background background = null!;
        private readonly System.Diagnostics.Stopwatch clock = new();
        private double lastTime;
        private double accumulator;

        private readonly Font titleFont = new Font("Segoe UI", 28F, FontStyle.Bold);
        private readonly Font infoFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        private readonly Font bigScoreFont = new Font("Segoe UI", 36F, FontStyle.Bold);

        private static readonly Color SkyColor = Color.FromArgb(112, 197, 206);

        public GameForm()
        {
            InitializeComponent();
            gamePanel.Resize += GamePanel_Resize;
            UpdateViewTransform();
        }

        // ---------------------------------------------------------------
        // Form olayları
        // ---------------------------------------------------------------

        private void GameForm_Load(object? sender, EventArgs e)
        {
            try
            {
                game = new GameManager(LogicalWidth, LogicalHeight - GroundHeight);
                background = new Background(LogicalWidth, LogicalHeight - GroundHeight);
                game.StatusChanged += Game_StatusChanged;
                game.Flapped += (_, _) => sounds.PlayFlap();
                game.Scored += (_, _) => sounds.PlayPoint();
                game.Crashed += (_, _) => sounds.PlayHit();

                UpdateStatusLabels();
                UpdateRestartButton();
                clock.Start();
                gameTimer.Start();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("Oyun başlatılamadı: " + ex.Message, "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        /// <summary>
        /// Oyun döngüsü. Timer'ın ne kadar düzenli tetiklendiğinden bağımsız olarak,
        /// geçen gerçek süreye göre sabit 1/60 sn'lik adımlar çalıştırılır
        /// (fixed timestep + accumulator). Böylece kare hızı dalgalansa da oyun hızı değişmez.
        /// </summary>
        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            double now = clock.Elapsed.TotalSeconds;
            accumulator += now - lastTime;
            lastTime = now;

            int steps = 0;
            while (accumulator >= StepSeconds && steps < MaxStepsPerTick)
            {
                game.Update();
                if (game.State == GameState.Playing || game.State == GameState.Ready)
                {
                    groundOffset = (groundOffset + game.Speed) % 40;
                    background.Update(game.Speed);
                    game.Bird.Animate(hovering: game.State == GameState.Ready);
                }

                accumulator -= StepSeconds;
                steps++;
            }

            if (steps == MaxStepsPerTick)
                accumulator = 0;   // Çok geride kaldıysak biriken süreyi at

            gamePanel.Invalidate();   // Paint olayını tetikler
        }

        private void GameForm_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                case Keys.Up:
                case Keys.W:
                    game.Flap();
                    break;

                case Keys.Enter:
                case Keys.R:
                    if (game.State == GameState.GameOver || game.State == GameState.Ready)
                        game.StartNewGame();
                    break;

                case Keys.P:
                case Keys.Escape:
                    game.TogglePause();
                    break;

                case Keys.D1:
                case Keys.NumPad1:
                    game.SetDifficulty(Difficulty.Easy);
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    game.SetDifficulty(Difficulty.Normal);
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    game.SetDifficulty(Difficulty.Hard);
                    break;

                case Keys.D:
                    game.AutoPilot = !game.AutoPilot;
                    if (game.AutoPilot && game.State == GameState.Ready)
                        game.Flap();
                    else if (game.AutoPilot && game.State == GameState.GameOver)
                        game.StartNewGame();
                    break;

                case Keys.F11:
                    ToggleFullScreen();
                    break;

                case Keys.M:
                    sounds.Muted = !sounds.Muted;
                    UpdateStatusLabels();
                    break;

                default:
                    return;
            }

            // Tuşu biz işledik; başka kontrolün (ör. butonun) almasını engelle
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void GameForm_KeyUp(object? sender, KeyEventArgs e)
        {
            // Boşluk tuşunun KeyUp'ı odaktaki butonu "tıklamasın"
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                e.Handled = true;
        }

        private void BtnRestart_Click(object? sender, EventArgs e)
        {
            game.StartNewGame();
            ActiveControl = null;   // Odağı butondan al
        }

        private void Game_StatusChanged(object? sender, EventArgs e)
        {
            UpdateStatusLabels();
            UpdateRestartButton();
        }

        // ---------------------------------------------------------------
        // Arayüz güncelleme
        // ---------------------------------------------------------------

        private void UpdateStatusLabels()
        {
            lblScore.Text = $"Skor: {game.Score}";
            lblHighScore.Text = $"En Yüksek: {game.HighScore}";
            lblLevel.Text = $"{game.Difficulty.Name} • Sv {game.Level}";
            lblSpeed.Text = $"Hız: {game.Speed:0.0}" + (sounds.Muted ? " (sessiz)" : "");
            Text = game.AutoPilot ? "Flappy Bird - YMH219  [DEMO]" : "Flappy Bird - YMH219";
        }

        private void UpdateRestartButton()
        {
            bool visible = game.State == GameState.GameOver;
            btnRestart.Visible = visible;
            if (visible)
            {
                btnRestart.Text = "Yeniden Başlat";
                PositionRestartButton();
            }
        }

        // ---------------------------------------------------------------
        // Ölçekleme ve tam ekran
        // ---------------------------------------------------------------

        /// <summary>Paneldeki mantıksal oyun alanının ölçeğini ve ortalama kaymasını hesaplar.</summary>
        private void UpdateViewTransform()
        {
            Size client = gamePanel.ClientSize;
            if (client.Width <= 0 || client.Height <= 0) return;

            viewScale = Math.Min((float)client.Width / LogicalWidth, (float)client.Height / LogicalHeight);
            viewOffset = new PointF((client.Width - LogicalWidth * viewScale) / 2f,
                                    (client.Height - LogicalHeight * viewScale) / 2f);
        }

        /// <summary>Yeniden Başlat butonunu ölçeklenmiş oyun alanında doğru yere taşır.</summary>
        private void PositionRestartButton()
        {
            const int logicalTop = 400;
            btnRestart.Left = (int)(viewOffset.X + LogicalWidth * viewScale / 2f - btnRestart.Width / 2f);
            btnRestart.Top = (int)(viewOffset.Y + logicalTop * viewScale);
        }

        private void GamePanel_Resize(object? sender, EventArgs e)
        {
            UpdateViewTransform();
            PositionRestartButton();
            gamePanel.Invalidate();
        }

        /// <summary>F11: kenarlıksız tam ekran ile normal pencere arasında geçiş yapar.</summary>
        private void ToggleFullScreen()
        {
            fullScreen = !fullScreen;
            if (fullScreen)
            {
                windowStateBeforeFullScreen = WindowState;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;   // Görev çubuğunun da kapanması için önce Normal
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = windowStateBeforeFullScreen;
            }
        }

        // ---------------------------------------------------------------
        // Çizim
        // ---------------------------------------------------------------

        private void GamePanel_Paint(object? sender, PaintEventArgs e)
        {
            if (game == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Mantıksal 480x680 alanı panele ölçekle; kalan kenarlar koyu renkte kalır
            g.TranslateTransform(viewOffset.X, viewOffset.Y);
            g.ScaleTransform(viewScale, viewScale);
            g.SetClip(new Rectangle(0, 0, LogicalWidth, LogicalHeight));

            using (var sky = new SolidBrush(SkyColor))
                g.FillRectangle(sky, 0, 0, LogicalWidth, LogicalHeight);
            background.Draw(g);

            foreach (Pipe pipe in game.Pipes)
                pipe.Draw(g, game.PlayAreaHeight);

            DrawGround(g);
            game.Bird.Draw(g);

            switch (game.State)
            {
                case GameState.Ready:
                    DrawCenteredText(g, "FLAPPY BIRD", titleFont, Color.White, 110);
                    DrawCenteredText(g, "Başlamak için BOŞLUK / ↑", infoFont, Color.White, 180);
                    DrawCenteredText(g, "Duraklat: P   •   Yeniden: Enter", infoFont, Color.White, 210);
                    DrawCenteredText(g, "Ses: M   •   Tam ekran: F11", infoFont, Color.White, 240);
                    DrawCenteredText(g, "Demo (otomatik pilot): D", infoFont, Color.White, 270);
                    DrawDifficultyChooser(g, 380);
                    break;

                case GameState.Playing:
                    DrawCenteredText(g, game.Score.ToString(), bigScoreFont, Color.White, 40);
                    if (game.AutoPilot)
                        DrawCenteredText(g, "DEMO • kapatmak için D", infoFont, Color.Gold, 100);
                    break;

                case GameState.Paused:
                    DrawCenteredText(g, game.Score.ToString(), bigScoreFont, Color.White, 40);
                    DrawCenteredText(g, "DURAKLATILDI", titleFont, Color.White, 260);
                    DrawCenteredText(g, "Devam için P", infoFont, Color.White, 315);
                    break;

                case GameState.GameOver:
                    DrawGameOver(g);
                    break;
            }
        }

        private void DrawGround(Graphics g)
        {
            int top = game.PlayAreaHeight;
            int width = LogicalWidth;

            using var dirt = new SolidBrush(Color.FromArgb(222, 216, 149));
            using var grass = new SolidBrush(Color.FromArgb(115, 191, 46));
            using var stripe = new SolidBrush(Color.FromArgb(200, 190, 120));

            g.FillRectangle(dirt, 0, top, width, GroundHeight);

            // Kayan çizgiler: zeminin hareket ettiği hissini verir
            for (float x = -groundOffset; x < width; x += 40)
                g.FillRectangle(stripe, x, top + 22, 20, 8);

            g.FillRectangle(grass, 0, top, width, 12);
        }

        private void DrawGameOver(Graphics g)
        {
            int width = LogicalWidth;
            var box = new Rectangle(width / 2 - 160, 170, 320, 210);

            using (var shade = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
                g.FillRectangle(shade, box);

            DrawCenteredText(g, "GAME OVER", titleFont, Color.OrangeRed, 190);
            DrawCenteredText(g, $"Skor: {game.Score}", infoFont, Color.White, 260);
            DrawCenteredText(g, $"En Yüksek: {game.HighScore}", infoFont, Color.Gold, 290);

            if (game.IsNewHighScore)
                DrawCenteredText(g, "Yeni rekor!", infoFont, Color.LightGreen, 325);
            else if (game.AutoPilotUsed)
                DrawCenteredText(g, "Demo oyunu rekora sayılmaz", infoFont, Color.LightGray, 325);

            DrawCenteredText(g, "Enter / R ile de yeniden başlayabilirsin", infoFont,
                             Color.White, 470);
            DrawDifficultyChooser(g, 510);
        }

        /// <summary>1 / 2 / 3 tuşlarıyla seçilen zorluğu vurgulayarak gösterir.</summary>
        private void DrawDifficultyChooser(Graphics g, int y)
        {
            DrawCenteredText(g, "Zorluk (1 / 2 / 3):", infoFont, Color.White, y);

            int boxWidth = 90, gap = 10;
            int total = Difficulty.All.Count * boxWidth + (Difficulty.All.Count - 1) * gap;
            int x = (LogicalWidth - total) / 2;

            using var normal = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
            using var selected = new SolidBrush(Color.FromArgb(230, 255, 140, 0));
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            for (int i = 0; i < Difficulty.All.Count; i++)
            {
                Difficulty d = Difficulty.All[i];
                var box = new Rectangle(x + i * (boxWidth + gap), y + 30, boxWidth, 32);
                g.FillRectangle(d == game.Difficulty ? selected : normal, box);
                g.DrawString($"{i + 1}  {d.Name}", infoFont, Brushes.White, box, format);
            }
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Color color, int y)
        {
            SizeF size = g.MeasureString(text, font);
            float x = (LogicalWidth - size.Width) / 2;

            // Okunabilirlik için hafif gölge
            using (var shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                g.DrawString(text, font, shadow, x + 2, y + 2);
            using (var brush = new SolidBrush(color))
                g.DrawString(text, font, brush, x, y);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            gameTimer.Stop();
            titleFont.Dispose();
            infoFont.Dispose();
            bigScoreFont.Dispose();
            sounds.Dispose();
            base.OnFormClosed(e);
        }
    }
}
