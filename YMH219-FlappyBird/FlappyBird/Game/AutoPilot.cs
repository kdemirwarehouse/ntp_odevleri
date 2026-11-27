namespace FlappyBird.Game
{
    /// <summary>
    /// Demo modunun karar verici sınıfı.
    /// Her karede "şimdi zıplamazsam" ve "şimdi zıplarsam" seçeneklerini, kuşun fiziğini
    /// birkaç kare ileriye simüle ederek dener (derinlik öncelikli arama + başarısız durum belleği).
    /// Önce boşluğu hedefleyen hamle denenir; o hamle ileride kaçınılmaz bir çarpışmaya
    /// götürüyorsa diğeri seçilir.
    /// </summary>
    public static class AutoPilot
    {
        private const int LookAheadFrames = 36;   // ~0.6 saniye ileri bakış

        public static bool ShouldFlap(Bird bird, IReadOnlyList<Pipe> pipes, float speed, int playAreaHeight)
        {
            var probe = new Bird(0, 0);
            var failed = new HashSet<(int, int, int)>();

            bool Safe(int t, float y, float v)
            {
                if (t == LookAheadFrames) return true;

                var key = (t, (int)y, (int)(v * 2));
                if (failed.Contains(key)) return false;

                // Önce zıplamadan, sonra zıplayarak dene
                if (Step(t, y, v, flap: false, out float y1, out float v1) && Safe(t + 1, y1, v1)) return true;
                if (Step(t, y, v, flap: true, out float y2, out float v2) && Safe(t + 1, y2, v2)) return true;

                failed.Add(key);
                return false;
            }

            bool Step(int t, float y, float v, bool flap, out float ny, out float nv)
            {
                nv = Math.Min((flap ? Bird.FlapStrength : v) + Bird.Gravity, Bird.MaxFallSpeed);
                ny = y + nv;
                if (ny < 0) { ny = 0; nv = 0; }

                if (ny + bird.Height >= playAreaHeight) return false;

                // Borular sola kayacağına göre, kuşu göreli olarak sağa kaydırıp çarpışmayı kontrol et
                probe.SetPosition(bird.X + speed * (t + 1), ny);
                foreach (Pipe pipe in pipes)
                    if (pipe.CollidesWith(probe, playAreaHeight)) return false;
                return true;
            }

            // Tercih edilen hamle: önündeki boşluğun biraz altını hedefle (zemine yapışmayı önler)
            Pipe? next = pipes.Where(p => p.X + Pipe.PipeWidth > bird.X)
                              .OrderBy(p => p.X)
                              .FirstOrDefault();
            float target = next == null ? playAreaHeight / 2f : next.GapTop + next.GapHeight * 0.6f;
            bool prefer = bird.Y + bird.Height / 2f > target && bird.Velocity >= 0;

            foreach (bool flap in new[] { prefer, !prefer })
            {
                if (Step(0, bird.Y, bird.Velocity, flap, out float y0, out float v0) && Safe(1, y0, v0))
                    return flap;
            }

            // Güvenli yol bulunamadı: en azından zemine düşmemeye çalış
            return bird.Y > playAreaHeight / 2f && bird.Velocity >= 0;
        }
    }
}
