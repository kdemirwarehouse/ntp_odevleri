namespace FlappyBird.Game
{
    /// <summary>
    /// Oyunun kurallarını ve durumunu yöneten sınıf.
    /// Kuşu, boruları, skoru, hızı ve oyun durumunu (GameState) bir arada tutar.
    /// Form sadece çizim ve klavye olaylarıyla ilgilenir; oyun mantığı buradadır.
    /// </summary>
    public class GameManager
    {
        // Hız ayarları
        private const float BaseSpeed = 4f;
        private const float SpeedStep = 0.6f;   // Her seviyede eklenen hız
        private const float MaxSpeed = 7.5f;
        private const int PointsPerLevel = 5;   // Kaç puanda bir seviye atlanır

        private const int PipeCount = 3;
        private const int PipeSpacing = 250;    // Borular arası yatay mesafe

        private readonly Random random = new Random();
        private readonly HighScoreStore highScoreStore = new HighScoreStore();
        private readonly List<Pipe> pipes = new List<Pipe>();

        public int Width { get; }
        public int PlayAreaHeight { get; }      // Zemin hariç oynanabilir alan

        public Bird Bird { get; }
        public IReadOnlyList<Pipe> Pipes => pipes;

        public GameState State { get; private set; } = GameState.Ready;
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public float Speed { get; private set; } = BaseSpeed;
        public int Level => Score / PointsPerLevel + 1;
        public bool IsNewHighScore { get; private set; }

        /// <summary>Skor, seviye veya durum değiştiğinde forma haber verir.</summary>
        public event EventHandler? StatusChanged;

        public GameManager(int width, int playAreaHeight)
        {
            if (width <= 0 || playAreaHeight <= Pipe.GapHeight)
                throw new ArgumentException("Oyun alanı boyutları geçersiz.");

            Width = width;
            PlayAreaHeight = playAreaHeight;
            Bird = new Bird(width / 4f, playAreaHeight / 2f);
            HighScore = highScoreStore.Load();

            ResetWorld();
        }

        /// <summary>Kuşu, boruları, skoru ve hızı başlangıç değerlerine döndürür.</summary>
        private void ResetWorld()
        {
            Bird.Reset();
            Score = 0;
            Speed = BaseSpeed;
            IsNewHighScore = false;

            pipes.Clear();
            for (int i = 0; i < PipeCount; i++)
            {
                float x = Width + 100 + i * PipeSpacing;
                var pipe = new Pipe(x, PlayAreaHeight, random);
                if (i > 0)
                    pipe.Reposition(x, PlayAreaHeight, random, pipes[i - 1].GapTop);
                pipes.Add(pipe);
            }
        }

        /// <summary>Yeni oyun başlatır (Yeni Oyun / Yeniden Başlat).</summary>
        public void StartNewGame()
        {
            ResetWorld();
            State = GameState.Playing;
            OnStatusChanged();
        }

        /// <summary>Oyuncunun zıplama komutu (Boşluk / Yukarı ok).</summary>
        public void Flap()
        {
            switch (State)
            {
                case GameState.Ready:
                    State = GameState.Playing;
                    Bird.Flap();
                    OnStatusChanged();
                    break;
                case GameState.Playing:
                    Bird.Flap();
                    break;
                // Paused ve GameOver durumunda zıplama yok
            }
        }

        /// <summary>Oyunu duraklatır veya devam ettirir.</summary>
        public void TogglePause()
        {
            if (State == GameState.Playing)
                State = GameState.Paused;
            else if (State == GameState.Paused)
                State = GameState.Playing;
            else
                return;

            OnStatusChanged();
        }

        /// <summary>
        /// Oyun döngüsünün bir adımı. Timer her tetiklendiğinde çağrılır.
        /// </summary>
        public void Update()
        {
            if (State != GameState.Playing)
                return;

            Bird.Update();

            foreach (Pipe pipe in pipes)
            {
                pipe.Update(Speed);

                // Kuş boruyu geçti mi? -> skor artır
                if (!pipe.Passed && pipe.X + Pipe.PipeWidth < Bird.X)
                {
                    pipe.Passed = true;
                    AddPoint();
                }
            }

            RecyclePipes();
            CheckCollisions();
        }

        /// <summary>Ekrandan çıkan boruları en sağdaki borunun arkasına taşır.</summary>
        private void RecyclePipes()
        {
            foreach (Pipe pipe in pipes)
            {
                if (pipe.IsOffScreen)
                {
                    Pipe last = pipes.MaxBy(p => p.X)!;
                    pipe.Reposition(last.X + PipeSpacing, PlayAreaHeight, random, last.GapTop);
                }
            }
        }

        private void AddPoint()
        {
            int oldLevel = Level;
            Score++;

            // Seviye atlandıysa hızı artır
            if (Level > oldLevel)
                Speed = Math.Min(BaseSpeed + (Level - 1) * SpeedStep, MaxSpeed);

            OnStatusChanged();
        }

        /// <summary>Zemin ve boru çarpışmalarını kontrol eder.</summary>
        private void CheckCollisions()
        {
            bool hitGround = Bird.Y + Bird.Height >= PlayAreaHeight;
            bool hitPipe = pipes.Any(p => p.CollidesWith(Bird.Bounds, PlayAreaHeight));

            if (hitGround || hitPipe)
                EndGame();
        }

        private void EndGame()
        {
            State = GameState.GameOver;

            if (Score > HighScore)
            {
                HighScore = Score;
                IsNewHighScore = true;
                highScoreStore.Save(HighScore);
            }

            OnStatusChanged();
        }

        private void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
