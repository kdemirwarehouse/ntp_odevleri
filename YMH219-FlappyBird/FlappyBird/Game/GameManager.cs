namespace FlappyBird.Game
{
    /// <summary>
    /// Oyunun kurallarını ve durumunu yöneten sınıf.
    /// Kuşu, boruları, skoru, hızı ve oyun durumunu (GameState) bir arada tutar.
    /// Form sadece çizim ve klavye olaylarıyla ilgilenir; oyun mantığı buradadır.
    /// </summary>
    public class GameManager
    {
        // Hız, boşluk ve boru aralığı ayarları Difficulty sınıfından gelir.
        private const int PointsPerLevel = 5;   // Kaç puanda bir seviye atlanır
        private const int PipeCount = 3;

        private readonly Random random;
        private readonly HighScoreStore highScoreStore;
        private readonly List<Pipe> pipes = new List<Pipe>();

        public int Width { get; }
        public int PlayAreaHeight { get; }      // Zemin hariç oynanabilir alan

        public Bird Bird { get; }
        public IReadOnlyList<Pipe> Pipes => pipes;

        public GameState State { get; private set; } = GameState.Ready;
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public float Speed { get; private set; }
        public Difficulty Difficulty { get; private set; } = Difficulty.Normal;
        public int Level => Score / PointsPerLevel + 1;
        public bool IsNewHighScore { get; private set; }

        /// <summary>
        /// Demo modu: kuşu bilgisayar yönetir. Demo kullanılan oyunlarda skor rekor olarak kaydedilmez.
        /// </summary>
        public bool AutoPilot
        {
            get => autoPilot;
            set
            {
                autoPilot = value;
                if (value) AutoPilotUsed = true;
                OnStatusChanged();
            }
        }
        private bool autoPilot;

        /// <summary>Bu oyunda demo modu en az bir kez açıldı mı?</summary>
        public bool AutoPilotUsed { get; private set; }

        /// <summary>Skor, seviye veya durum değiştiğinde forma haber verir.</summary>
        public event EventHandler? StatusChanged;

        /// <summary>Ses efektleri için: kuş zıpladı, puan alındı, çarpışma oldu.</summary>
        public event EventHandler? Flapped;
        public event EventHandler? Scored;
        public event EventHandler? Crashed;

        public GameManager(int width, int playAreaHeight)
            : this(width, playAreaHeight, new HighScoreStore(), new Random()) { }

        /// <summary>Bağımlılıkları dışarıdan alan kurucu (testlerde sabit tohumlu Random ve geçici dosya için).</summary>
        public GameManager(int width, int playAreaHeight, HighScoreStore highScoreStore, Random random)
        {
            this.highScoreStore = highScoreStore;
            this.random = random;
            if (width <= 0 || playAreaHeight <= Difficulty.Easy.GapHeight + 120)
                throw new ArgumentException("Oyun alanı boyutları geçersiz.");

            Width = width;
            PlayAreaHeight = playAreaHeight;
            Bird = new Bird(width / 4f, playAreaHeight / 2f);
            HighScore = highScoreStore.Load(Difficulty);

            ResetWorld();
        }

        /// <summary>Kuşu, boruları, skoru ve hızı başlangıç değerlerine döndürür.</summary>
        private void ResetWorld()
        {
            Bird.Reset();
            Score = 0;
            Speed = Difficulty.BaseSpeed;
            IsNewHighScore = false;
            AutoPilotUsed = autoPilot;

            pipes.Clear();
            for (int i = 0; i < PipeCount; i++)
            {
                float x = Width + 100 + i * Difficulty.PipeSpacing;
                var pipe = new Pipe(x, PlayAreaHeight, random, Difficulty.GapHeight);
                if (i > 0)
                    pipe.Reposition(x, PlayAreaHeight, random, pipes[i - 1].GapTop);
                pipes.Add(pipe);
            }
        }

        /// <summary>
        /// Zorluğu değiştirir. Yalnızca oyun başlamadan (Hazır) veya bittikten sonra (Game Over)
        /// izin verilir; dünya sıfırlanır ve o zorluğun rekoru yüklenir.
        /// </summary>
        public bool SetDifficulty(Difficulty difficulty)
        {
            if (State == GameState.Playing || State == GameState.Paused)
                return false;

            Difficulty = difficulty;
            HighScore = highScoreStore.Load(difficulty);
            State = GameState.Ready;
            ResetWorld();
            OnStatusChanged();
            return true;
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
                    Flapped?.Invoke(this, EventArgs.Empty);
                    OnStatusChanged();
                    break;
                case GameState.Playing:
                    Bird.Flap();
                    Flapped?.Invoke(this, EventArgs.Empty);
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

            if (AutoPilot && Game.AutoPilot.ShouldFlap(Bird, pipes, Speed, PlayAreaHeight))
                Flap();

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
                    pipe.Reposition(last.X + Difficulty.PipeSpacing, PlayAreaHeight, random, last.GapTop);
                }
            }
        }

        private void AddPoint()
        {
            int oldLevel = Level;
            Score++;

            // Seviye atlandıysa hızı artır
            if (Level > oldLevel)
                Speed = Math.Min(Difficulty.BaseSpeed + (Level - 1) * Difficulty.SpeedStep, Difficulty.MaxSpeed);

            Scored?.Invoke(this, EventArgs.Empty);
            OnStatusChanged();
        }

        /// <summary>Zemin ve boru çarpışmalarını kontrol eder.</summary>
        private void CheckCollisions()
        {
            bool hitGround = Bird.Y + Bird.Height >= PlayAreaHeight;
            bool hitPipe = pipes.Any(p => p.CollidesWith(Bird, PlayAreaHeight));

            if (hitGround || hitPipe)
                EndGame();
        }

        private void EndGame()
        {
            State = GameState.GameOver;
            Crashed?.Invoke(this, EventArgs.Empty);

            if (Score > HighScore && !AutoPilotUsed)
            {
                HighScore = Score;
                IsNewHighScore = true;
                highScoreStore.Save(Difficulty, HighScore);
            }

            OnStatusChanged();
        }

        private void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
