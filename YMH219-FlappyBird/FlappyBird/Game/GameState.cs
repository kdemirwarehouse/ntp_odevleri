namespace FlappyBird.Game
{
    /// <summary>
    /// Oyunun içinde bulunabileceği durumlar.
    /// </summary>
    public enum GameState
    {
        /// <summary>Oyun açıldı, oyuncunun başlamasını bekliyor.</summary>
        Ready,

        /// <summary>Oyun devam ediyor.</summary>
        Playing,

        /// <summary>Oyun duraklatıldı.</summary>
        Paused,

        /// <summary>Kuş bir engele veya zemine çarptı.</summary>
        GameOver
    }
}
