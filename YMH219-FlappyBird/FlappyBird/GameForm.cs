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

        private GameManager game = null!;
        private float groundOffset;   // Zeminin kayma animasyonu için

        private readonly Font titleFont = new Font("Segoe UI", 28F, FontStyle.Bold);
        private readonly Font infoFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        private readonly Font bigScoreFont = new Font("Segoe UI", 36F, FontStyle.Bold);

        public GameForm()
        {
            InitializeComponent();
        }

        // ---------------------------------------------------------------
        // Form olayları
        // ---------------------------------------------------------------

        private void GameForm_Load(object? sender, EventArgs e)
        {
            try
            {
                int playAreaHeight = gamePanel.ClientSize.Height - GroundHeight;
                game = new GameManager(gamePanel.ClientSize.Width, playAreaHeight);
                game.StatusChanged += Game_StatusChanged;

                UpdateStatusLabels();
                UpdateRestartButton();
                gameTimer.Start();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("Oyun başlatılamadı: " + ex.Message, "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        /// <summary>Oyun döngüsü: her tikte oyunu günceller ve yeniden çizer.</summary>
        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            game.Update();

            if (game.State == GameState.Playing || game.State == GameState.Ready)
                groundOffset = (groundOffset + game.Speed) % 40;

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
            lblLevel.Text = $"Seviye: {game.Level}";
            lblSpeed.Text = $"Hız: {game.Speed:0.0}";
        }

        private void UpdateRestartButton()
        {
            bool visible = game.State == GameState.GameOver;
            btnRestart.Visible = visible;
            if (visible)
            {
                btnRestart.Text = "Yeniden Başlat";
                btnRestart.Left = (gamePanel.ClientSize.Width - btnRestart.Width) / 2;
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

            foreach (Pipe pipe in game.Pipes)
                pipe.Draw(g, game.PlayAreaHeight);

            DrawGround(g);
            game.Bird.Draw(g);

            switch (game.State)
            {
                case GameState.Ready:
                    DrawCenteredText(g, "FLAPPY BIRD", titleFont, Color.White, 170);
                    DrawCenteredText(g, "Başlamak için BOŞLUK / ↑", infoFont, Color.White, 240);
                    DrawCenteredText(g, "Duraklat: P   •   Yeniden: Enter", infoFont, Color.White, 270);
                    break;

                case GameState.Playing:
                    DrawCenteredText(g, game.Score.ToString(), bigScoreFont, Color.White, 40);
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
            int width = gamePanel.ClientSize.Width;

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
            int width = gamePanel.ClientSize.Width;
            var box = new Rectangle(width / 2 - 160, 170, 320, 210);

            using (var shade = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
                g.FillRectangle(shade, box);

            DrawCenteredText(g, "GAME OVER", titleFont, Color.OrangeRed, 190);
            DrawCenteredText(g, $"Skor: {game.Score}", infoFont, Color.White, 260);
            DrawCenteredText(g, $"En Yüksek: {game.HighScore}", infoFont, Color.Gold, 290);

            if (game.IsNewHighScore)
                DrawCenteredText(g, "Yeni rekor!", infoFont, Color.LightGreen, 325);

            DrawCenteredText(g, "Enter / R ile de yeniden başlayabilirsin", infoFont,
                             Color.White, 470);
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Color color, int y)
        {
            SizeF size = g.MeasureString(text, font);
            float x = (gamePanel.ClientSize.Width - size.Width) / 2;

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
            base.OnFormClosed(e);
        }
    }
}
