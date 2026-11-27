using System.Media;

namespace FlappyBird.Game
{
    /// <summary>
    /// Oyunun ses efektleri. Harici ses dosyası kullanılmaz: kısa tonlar program
    /// açılırken bellekte 16-bit PCM WAV olarak üretilir ve <see cref="SoundPlayer"/> ile çalınır.
    /// </summary>
    public sealed class SoundEffects : IDisposable
    {
        public const int SampleRate = 22050;

        private readonly SoundPlayer flap;
        private readonly SoundPlayer point;
        private readonly SoundPlayer hit;

        /// <summary>Sessiz mod (M tuşu).</summary>
        public bool Muted { get; set; }

        public SoundEffects()
        {
            // Zıplama: kısa, yükselen ses
            flap = CreatePlayer(Sweep(520, 880, 0.08, 0.35));
            // Puan: iki notalı "di-ding"
            point = CreatePlayer(Concat(Tone(988, 0.07, 0.3), Tone(1319, 0.12, 0.3)));
            // Çarpma: alçalan, sert ses
            hit = CreatePlayer(Sweep(300, 70, 0.30, 0.45, square: true));
        }

        public void PlayFlap() => Play(flap);
        public void PlayPoint() => Play(point);
        public void PlayHit() => Play(hit);

        private void Play(SoundPlayer player)
        {
            if (Muted) return;
            try
            {
                player.Play();   // Asenkron çalar, oyun döngüsünü bekletmez
            }
            catch (InvalidOperationException)
            {
                // Ses aygıtı yoksa oyun sessiz devam eder
            }
        }

        // ---------------------------------------------------------------
        // Dalga üretimi
        // ---------------------------------------------------------------

        /// <summary>Sabit frekanslı sinüs tonu (yumuşak giriş/çıkışlı).</summary>
        public static short[] Tone(double frequency, double seconds, double volume) =>
            Sweep(frequency, frequency, seconds, volume);

        /// <summary>Frekansı başlangıçtan bitişe doğrusal kayan ton.</summary>
        public static short[] Sweep(double startHz, double endHz, double seconds, double volume, bool square = false)
        {
            int count = (int)(SampleRate * seconds);
            var samples = new short[count];
            double phase = 0;

            for (int i = 0; i < count; i++)
            {
                double t = (double)i / count;
                double freq = startHz + (endHz - startHz) * t;
                phase += 2 * Math.PI * freq / SampleRate;

                double wave = Math.Sin(phase);
                if (square) wave = Math.Sign(wave) * 0.6;

                // Tık sesini önlemek için kısa fade-in, sona doğru fade-out
                double envelope = Math.Min(1.0, i / (SampleRate * 0.005)) * (1.0 - t);
                samples[i] = (short)(wave * envelope * volume * short.MaxValue);
            }
            return samples;
        }

        public static short[] Concat(params short[][] parts) => parts.SelectMany(p => p).ToArray();

        /// <summary>PCM örneklerini RIFF/WAV formatında bayt dizisine çevirir.</summary>
        public static byte[] ToWav(short[] samples)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            int dataSize = samples.Length * 2;

            w.Write("RIFF"u8.ToArray());
            w.Write(36 + dataSize);
            w.Write("WAVE"u8.ToArray());
            w.Write("fmt "u8.ToArray());
            w.Write(16);                 // fmt bölümü boyutu
            w.Write((short)1);           // PCM
            w.Write((short)1);           // mono
            w.Write(SampleRate);
            w.Write(SampleRate * 2);     // bayt/sn
            w.Write((short)2);           // blok hizalama
            w.Write((short)16);          // bit/örnek
            w.Write("data"u8.ToArray());
            w.Write(dataSize);
            foreach (short s in samples) w.Write(s);

            w.Flush();
            return ms.ToArray();
        }

        private static SoundPlayer CreatePlayer(short[] samples)
        {
            var player = new SoundPlayer(new MemoryStream(ToWav(samples)));
            player.Load();
            return player;
        }

        public void Dispose()
        {
            flap.Dispose();
            point.Dispose();
            hit.Dispose();
        }
    }
}
