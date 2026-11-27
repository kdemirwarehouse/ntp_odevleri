using FlappyBird.Game;

// Harici test çatısı gerektirmeyen basit konsol test çalıştırıcısı.
// Çalıştırma: dotnet run --project tests/FlappyBird.Tests

int basarili = 0, basarisiz = 0;
string geciciKlasor = Path.Combine(Path.GetTempPath(), "FlappyBirdTests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(geciciKlasor);

void Test(string ad, Action govde)
{
    try
    {
        govde();
        basarili++;
        Console.WriteLine($"  [OK]   {ad}");
    }
    catch (Exception ex)
    {
        basarisiz++;
        Console.WriteLine($"  [HATA] {ad}: {ex.Message}");
    }
}

void Dogrula(bool kosul, string mesaj)
{
    if (!kosul) throw new Exception(mesaj);
}

GameManager YeniOyun(int tohum = 1) =>
    new GameManager(480, 620, new HighScoreStore(Path.Combine(geciciKlasor, Guid.NewGuid() + ".txt")), new Random(tohum));

Console.WriteLine("FlappyBird testleri");

Test("Oyun Hazır durumunda başlar, ilk zıplama oyunu başlatır", () =>
{
    var oyun = YeniOyun();
    Dogrula(oyun.State == GameState.Ready, "Başlangıç durumu Ready olmalı");
    oyun.Flap();
    Dogrula(oyun.State == GameState.Playing, "Zıplama sonrası Playing olmalı");
    Dogrula(oyun.Bird.Velocity < 0, "Kuş yukarı hız almalı");
});

Test("Yer çekimi kuşu aşağı çeker, düşüş hızı sınırlıdır", () =>
{
    var kus = new Bird(100, 100);
    for (int i = 0; i < 200; i++) kus.Update();
    Dogrula(kus.Y > 100, "Kuş aşağı inmeli");
    Dogrula(kus.Velocity <= 12f, "Düşüş hızı 12'yi geçmemeli");
});

Test("Kuş tavanın dışına çıkamaz", () =>
{
    var kus = new Bird(100, 5);
    for (int i = 0; i < 5; i++) { kus.Flap(); kus.Update(); }
    Dogrula(kus.Y >= 0, "Y negatif olmamalı");
});

Test("Duraklatma ve devam", () =>
{
    var oyun = YeniOyun();
    oyun.TogglePause();
    Dogrula(oyun.State == GameState.Ready, "Ready durumunda duraklatma etkisiz olmalı");
    oyun.StartNewGame();
    oyun.TogglePause();
    Dogrula(oyun.State == GameState.Paused, "Paused olmalı");
    float y = oyun.Bird.Y;
    oyun.Update();
    Dogrula(oyun.Bird.Y == y, "Duraklatılmışken kuş hareket etmemeli");
    oyun.TogglePause();
    Dogrula(oyun.State == GameState.Playing, "Tekrar Playing olmalı");
});

Test("Zemine düşen kuş oyunu bitirir", () =>
{
    var oyun = YeniOyun();
    oyun.StartNewGame();
    for (int i = 0; i < 600 && oyun.State == GameState.Playing; i++) oyun.Update();
    Dogrula(oyun.State == GameState.GameOver, "Oyun GameOver olmalı");
});

Test("Elips çarpışma: köşeye yakın ama gövdeye değmeyen dikdörtgen çarpışma sayılmaz", () =>
{
    var kus = new Bird(0, 0);           // 38x28 kutu, elips merkezi (19,14)
    var koseKutusu = new RectangleF(34, 0, 4, 3);   // sağ üst köşe, elipsin dışında
    Dogrula(kus.Bounds.IntersectsWith(Rectangle.Ceiling(koseKutusu)), "Dikdörtgen testi çakışma bulmalı");
    Dogrula(!kus.Intersects(koseKutusu), "Elips testi köşeyi çarpışma saymamalı");
    Dogrula(kus.Intersects(new RectangleF(15, 10, 8, 8)), "Merkezdeki dikdörtgen çarpışmalı");
    Dogrula(kus.Intersects(new RectangleF(36, 12, 10, 4)), "Sağ kenara değen dikdörtgen çarpışmalı");
});

Test("Borunun geniş ağız kısmı da çarpışma sayılır", () =>
{
    var boru = new Pipe(200, 620, new Random(3));
    var kus = new Bird(0, 0);
    // Kuşu üst borunun ağzının sol çıkıntısına (gövdenin 4 px solu) yerleştir
    kus.SetPosition(200 - 4 - kus.Width + 2, boru.GapTop - 10 - kus.Height / 2f);
    Dogrula(boru.CollidesWith(kus, 620), "Ağız çıkıntısına çarpma algılanmalı");
    kus.SetPosition(200 + 10, boru.GapTop + boru.GapHeight / 2f - kus.Height / 2f);
    Dogrula(!boru.CollidesWith(kus, 620), "Boşluğun ortasındaki kuş çarpmamalı");
});

Test("Ardışık boru boşlukları arasındaki dikey fark sınırlıdır", () =>
{
    var rnd = new Random(42);
    var boru = new Pipe(0, 620, rnd);
    for (int i = 0; i < 500; i++)
    {
        int onceki = boru.GapTop;
        boru.Reposition(0, 620, rnd, onceki);
        Dogrula(Math.Abs(boru.GapTop - onceki) <= 170, "Fark 170 pikseli aşmamalı");
        Dogrula(boru.GapTop >= 60 && boru.GapTop + boru.GapHeight <= 620 - 60, "Boşluk kenar payı içinde kalmalı");
    }
});

Test("En yüksek skor dosyaya yazılır ve okunur, bozuk dosya 0 döner", () =>
{
    string yol = Path.Combine(geciciKlasor, "skor.txt");
    var depo = new HighScoreStore(yol);
    Dogrula(depo.Load() == 0, "Dosya yokken 0 olmalı");
    Dogrula(depo.Save(17), "Kayıt başarılı olmalı");
    Dogrula(new HighScoreStore(yol).Load() == 17, "17 okunmalı");
    File.WriteAllText(yol, "abc");
    Dogrula(depo.Load() == 0, "Bozuk dosyada 0 olmalı");
});

Test("Geçersiz oyun alanı boyutu reddedilir", () =>
{
    bool hata = false;
    try { _ = new GameManager(0, 100); } catch (ArgumentException) { hata = true; }
    Dogrula(hata, "ArgumentException beklenirdi");
});

Test("Üretilen WAV başlığı geçerli ve boyutu doğru", () =>
{
    short[] ornekler = SoundEffects.Tone(440, 0.1, 0.5);
    byte[] wav = SoundEffects.ToWav(ornekler);
    Dogrula(System.Text.Encoding.ASCII.GetString(wav, 0, 4) == "RIFF", "RIFF başlığı yok");
    Dogrula(System.Text.Encoding.ASCII.GetString(wav, 8, 4) == "WAVE", "WAVE başlığı yok");
    Dogrula(wav.Length == 44 + ornekler.Length * 2, "Dosya boyutu hatalı");
    Dogrula(ornekler.Max() > 0 && ornekler.Min() < 0, "Dalga sessiz olmamalı");
});

Test("Zıplama, puan ve çarpma olayları tetiklenir", () =>
{
    var oyun = YeniOyun();
    int ziplama = 0, puan = 0, carpma = 0;
    oyun.Flapped += (_, _) => ziplama++;
    oyun.Scored += (_, _) => puan++;
    oyun.Crashed += (_, _) => carpma++;
    oyun.Flap();
    Dogrula(ziplama == 1, "Flapped bir kez tetiklenmeli");
    for (int i = 0; i < 2000 && oyun.State == GameState.Playing; i++) oyun.Update();
    Dogrula(carpma == 1, "Crashed bir kez tetiklenmeli");
    Dogrula(puan == oyun.Score, "Scored sayısı skora eşit olmalı");
});

Test("Zorluk boşluğu, hızı ve rekoru değiştirir; oyun sırasında değiştirilemez", () =>
{
    string yol = Path.Combine(geciciKlasor, "zorluk.txt");
    var depo = new HighScoreStore(yol);
    depo.Save(Difficulty.Hard, 9);
    var oyun = new GameManager(480, 620, depo, new Random(5));
    Dogrula(oyun.Difficulty == Difficulty.Normal, "Varsayılan Orta olmalı");
    Dogrula(oyun.SetDifficulty(Difficulty.Hard), "Hazır durumunda değişebilmeli");
    Dogrula(oyun.HighScore == 9, "Zor rekoru yüklenmeli");
    Dogrula(oyun.Pipes.All(p => p.GapHeight == Difficulty.Hard.GapHeight), "Boşluk Zor ayarında olmalı");
    Dogrula(oyun.Speed == Difficulty.Hard.BaseSpeed, "Hız Zor ayarında olmalı");
    oyun.Flap();
    Dogrula(!oyun.SetDifficulty(Difficulty.Easy), "Oyun sırasında değişmemeli");
});

Test("Rekorlar zorluk başına ayrı saklanır, eski tek sayılı dosya Orta sayılır", () =>
{
    string yol = Path.Combine(geciciKlasor, "eski.txt");
    File.WriteAllText(yol, "23");
    var depo = new HighScoreStore(yol);
    Dogrula(depo.Load(Difficulty.Normal) == 23, "Eski dosya Orta rekoru olmalı");
    Dogrula(depo.Load(Difficulty.Easy) == 0, "Kolay rekoru 0 olmalı");
    depo.Save(Difficulty.Easy, 40);
    Dogrula(depo.Load(Difficulty.Normal) == 23 && depo.Load(Difficulty.Easy) == 40, "İki rekor birlikte korunmalı");
});

Test("Demo modu boruları geçer ve skoru rekor olarak kaydetmez", () =>
{
    string yol = Path.Combine(geciciKlasor, "demo.txt");
    var oyun = new GameManager(480, 620, new HighScoreStore(yol), new Random(11));
    oyun.SetDifficulty(Difficulty.Easy);
    oyun.AutoPilot = true;
    oyun.Flap();
    for (int i = 0; i < 60 * 60 && oyun.State == GameState.Playing; i++) oyun.Update();   // ~1 dakika
    Dogrula(oyun.Score >= 10, $"Otomatik pilot en az 10 boru geçmeli (skor {oyun.Score})");
    oyun.AutoPilot = false;
    for (int i = 0; i < 2000 && oyun.State == GameState.Playing; i++) oyun.Update();
    Dogrula(oyun.State == GameState.GameOver, "Pilot kapanınca oyun bitmeli");
    Dogrula(oyun.HighScore == 0 && !oyun.IsNewHighScore, "Demo skoru rekor olmamalı");
    Dogrula(!File.Exists(yol), "Rekor dosyası yazılmamalı");
});

try { Directory.Delete(geciciKlasor, true); } catch (IOException) { }

Console.WriteLine();
Console.WriteLine($"Sonuç: {basarili} başarılı, {basarisiz} başarısız");
return basarisiz == 0 ? 0 : 1;
