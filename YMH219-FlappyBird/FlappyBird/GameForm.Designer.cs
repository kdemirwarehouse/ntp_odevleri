#nullable disable

namespace FlappyBird
{
    partial class GameForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            gameTimer = new System.Windows.Forms.Timer(components);
            infoPanel = new Panel();
            lblSpeed = new Label();
            lblLevel = new Label();
            lblHighScore = new Label();
            lblScore = new Label();
            gamePanel = new GamePanel();
            btnRestart = new Button();
            infoPanel.SuspendLayout();
            gamePanel.SuspendLayout();
            SuspendLayout();
            //
            // gameTimer
            //
            gameTimer.Interval = 10;
            gameTimer.Tick += GameTimer_Tick;
            //
            // infoPanel
            //
            infoPanel.BackColor = Color.FromArgb(40, 44, 52);
            infoPanel.Controls.Add(lblSpeed);
            infoPanel.Controls.Add(lblLevel);
            infoPanel.Controls.Add(lblHighScore);
            infoPanel.Controls.Add(lblScore);
            infoPanel.Dock = DockStyle.Top;
            infoPanel.Location = new Point(0, 0);
            infoPanel.Name = "infoPanel";
            infoPanel.Size = new Size(480, 40);
            infoPanel.TabIndex = 0;
            //
            // lblScore
            //
            lblScore.AutoSize = true;
            lblScore.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblScore.ForeColor = Color.White;
            lblScore.Location = new Point(10, 9);
            lblScore.Name = "lblScore";
            lblScore.Text = "Skor: 0";
            //
            // lblHighScore
            //
            lblHighScore.AutoSize = true;
            lblHighScore.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblHighScore.ForeColor = Color.Gold;
            lblHighScore.Location = new Point(110, 9);
            lblHighScore.Name = "lblHighScore";
            lblHighScore.Text = "En Yüksek: 0";
            //
            // lblLevel
            //
            lblLevel.AutoSize = true;
            lblLevel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblLevel.ForeColor = Color.LightGreen;
            lblLevel.Location = new Point(245, 9);
            lblLevel.Name = "lblLevel";
            lblLevel.Text = "Orta • Sv 1";
            //
            // lblSpeed
            //
            lblSpeed.AutoSize = true;
            lblSpeed.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblSpeed.ForeColor = Color.LightSkyBlue;
            lblSpeed.Location = new Point(370, 9);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Text = "Hız: 4.0";
            //
            // gamePanel
            //
            gamePanel.BackColor = Color.FromArgb(24, 26, 32);
            gamePanel.Controls.Add(btnRestart);
            gamePanel.Dock = DockStyle.Fill;
            gamePanel.Location = new Point(0, 40);
            gamePanel.Name = "gamePanel";
            gamePanel.Size = new Size(480, 680);
            gamePanel.TabIndex = 1;
            gamePanel.Paint += GamePanel_Paint;
            //
            // btnRestart
            //
            btnRestart.BackColor = Color.OrangeRed;
            btnRestart.FlatStyle = FlatStyle.Flat;
            btnRestart.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnRestart.ForeColor = Color.White;
            btnRestart.Location = new Point(165, 400);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(150, 45);
            btnRestart.TabIndex = 0;
            btnRestart.TabStop = false;
            btnRestart.Text = "Yeni Oyun";
            btnRestart.UseVisualStyleBackColor = false;
            btnRestart.Click += BtnRestart_Click;
            //
            // GameForm
            //
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(480, 720);
            Controls.Add(gamePanel);
            Controls.Add(infoPanel);
            FormBorderStyle = FormBorderStyle.Sizable;
            KeyPreview = true;
            MinimumSize = new Size(360, 520);
            Name = "GameForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Flappy Bird - YMH219";
            Load += GameForm_Load;
            KeyDown += GameForm_KeyDown;
            KeyUp += GameForm_KeyUp;
            infoPanel.ResumeLayout(false);
            infoPanel.PerformLayout();
            gamePanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Timer gameTimer;
        private Panel infoPanel;
        private Label lblScore;
        private Label lblHighScore;
        private Label lblLevel;
        private Label lblSpeed;
        private GamePanel gamePanel;
        private Button btnRestart;
    }
}
