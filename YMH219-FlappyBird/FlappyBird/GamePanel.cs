namespace FlappyBird
{
    /// <summary>
    /// Oyunun çizildiği panel.
    /// DoubleBuffered açık olduğu için hareket sırasında titreme (flicker) olmaz.
    /// </summary>
    public class GamePanel : Panel
    {
        public GamePanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }
}
