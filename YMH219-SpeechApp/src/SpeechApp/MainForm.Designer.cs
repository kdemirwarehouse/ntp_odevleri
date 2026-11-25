#nullable disable

namespace SpeechApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

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
            lblAktif = new Label();
            grpTts = new GroupBox();
            rtbMetin = new RichTextBox();
            pnlTtsAyar = new FlowLayoutPanel();
            lblSes = new Label();
            cmbSes = new ComboBox();
            lblHiz = new Label();
            trkHiz = new TrackBar();
            lblDuzey = new Label();
            trkDuzey = new TrackBar();
            pnlTtsButon = new FlowLayoutPanel();
            btnOku = new Button();
            btnDuraklat = new Button();
            btnDurdur = new Button();
            btnTtsTemizle = new Button();
            btnWavKaydet = new Button();
            grpStt = new GroupBox();
            splitStt = new SplitContainer();
            txtTaninan = new TextBox();
            lvSonuclar = new ListView();
            colSaat = new ColumnHeader();
            colGuven = new ColumnHeader();
            colMetin = new ColumnHeader();
            pnlSttBilgi = new FlowLayoutPanel();
            lblDil = new Label();
            lblSeviye = new Label();
            pbSeviye = new ProgressBar();
            lblTahmin = new Label();
            pnlSttButon = new FlowLayoutPanel();
            btnDinle = new Button();
            btnDinlemeDurdur = new Button();
            btnDosyadanTani = new Button();
            btnSttTemizle = new Button();
            btnTxtKaydet = new Button();
            statusStrip = new StatusStrip();
            lblDurumAlt = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)trkHiz).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkDuzey).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitStt).BeginInit();
            splitStt.Panel1.SuspendLayout();
            splitStt.Panel2.SuspendLayout();
            splitStt.SuspendLayout();
            grpTts.SuspendLayout();
            grpStt.SuspendLayout();
            pnlTtsAyar.SuspendLayout();
            pnlTtsButon.SuspendLayout();
            pnlSttBilgi.SuspendLayout();
            pnlSttButon.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            //
            // lblAktif — hangi işlemin aktif olduğunu gösteren renkli şerit
            //
            lblAktif.BackColor = Color.DimGray;
            lblAktif.Dock = DockStyle.Top;
            lblAktif.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblAktif.ForeColor = Color.White;
            lblAktif.Name = "lblAktif";
            lblAktif.Size = new Size(900, 36);
            lblAktif.Text = "Hazır";
            lblAktif.TextAlign = ContentAlignment.MiddleCenter;
            //
            // grpTts
            //
            grpTts.Controls.Add(rtbMetin);
            grpTts.Controls.Add(pnlTtsButon);
            grpTts.Controls.Add(pnlTtsAyar);
            grpTts.Dock = DockStyle.Top;
            grpTts.Name = "grpTts";
            grpTts.Padding = new Padding(8);
            grpTts.Size = new Size(900, 270);
            grpTts.Text = "Text-to-Speech — yazılan metni sesli oku";
            //
            // rtbMetin
            //
            rtbMetin.Dock = DockStyle.Fill;
            rtbMetin.Font = new Font("Segoe UI", 11F);
            rtbMetin.Name = "rtbMetin";
            rtbMetin.TabIndex = 0;
            //
            // pnlTtsAyar
            //
            pnlTtsAyar.AutoSize = true;
            pnlTtsAyar.Controls.Add(lblSes);
            pnlTtsAyar.Controls.Add(cmbSes);
            pnlTtsAyar.Controls.Add(lblHiz);
            pnlTtsAyar.Controls.Add(trkHiz);
            pnlTtsAyar.Controls.Add(lblDuzey);
            pnlTtsAyar.Controls.Add(trkDuzey);
            pnlTtsAyar.Dock = DockStyle.Top;
            pnlTtsAyar.Name = "pnlTtsAyar";
            //
            // lblSes
            //
            lblSes.AutoSize = true;
            lblSes.Margin = new Padding(3, 9, 3, 0);
            lblSes.Name = "lblSes";
            lblSes.Text = "Ses:";
            //
            // cmbSes
            //
            cmbSes.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSes.Margin = new Padding(3, 6, 16, 3);
            cmbSes.Name = "cmbSes";
            cmbSes.Size = new Size(260, 23);
            cmbSes.SelectedIndexChanged += CmbSes_SelectedIndexChanged;
            //
            // lblHiz
            //
            lblHiz.AutoSize = true;
            lblHiz.Margin = new Padding(3, 9, 3, 0);
            lblHiz.Name = "lblHiz";
            lblHiz.Text = "Hız: 0";
            //
            // trkHiz
            //
            trkHiz.AutoSize = false;
            trkHiz.Maximum = 10;
            trkHiz.Minimum = -10;
            trkHiz.Name = "trkHiz";
            trkHiz.Size = new Size(150, 32);
            trkHiz.TickFrequency = 5;
            trkHiz.Scroll += TrkHiz_Scroll;
            //
            // lblDuzey
            //
            lblDuzey.AutoSize = true;
            lblDuzey.Margin = new Padding(16, 9, 3, 0);
            lblDuzey.Name = "lblDuzey";
            lblDuzey.Text = "Ses: 100";
            //
            // trkDuzey
            //
            trkDuzey.AutoSize = false;
            trkDuzey.Maximum = 100;
            trkDuzey.Name = "trkDuzey";
            trkDuzey.Size = new Size(150, 32);
            trkDuzey.TickFrequency = 25;
            trkDuzey.Value = 100;
            trkDuzey.Scroll += TrkDuzey_Scroll;
            //
            // pnlTtsButon
            //
            pnlTtsButon.AutoSize = true;
            pnlTtsButon.Controls.Add(btnOku);
            pnlTtsButon.Controls.Add(btnDuraklat);
            pnlTtsButon.Controls.Add(btnDurdur);
            pnlTtsButon.Controls.Add(btnTtsTemizle);
            pnlTtsButon.Controls.Add(btnWavKaydet);
            pnlTtsButon.Dock = DockStyle.Bottom;
            pnlTtsButon.Name = "pnlTtsButon";
            //
            // TTS butonları
            //
            btnOku.Name = "btnOku";
            btnOku.Size = new Size(110, 32);
            btnOku.Text = "▶ Oku (Başlat)";
            btnOku.Click += BtnOku_Click;
            btnDuraklat.Enabled = false;
            btnDuraklat.Name = "btnDuraklat";
            btnDuraklat.Size = new Size(110, 32);
            btnDuraklat.Text = "⏸ Duraklat";
            btnDuraklat.Click += BtnDuraklat_Click;
            btnDurdur.Enabled = false;
            btnDurdur.Name = "btnDurdur";
            btnDurdur.Size = new Size(90, 32);
            btnDurdur.Text = "■ Durdur";
            btnDurdur.Click += BtnDurdur_Click;
            btnTtsTemizle.Name = "btnTtsTemizle";
            btnTtsTemizle.Size = new Size(90, 32);
            btnTtsTemizle.Text = "Temizle";
            btnTtsTemizle.Click += BtnTtsTemizle_Click;
            btnWavKaydet.Name = "btnWavKaydet";
            btnWavKaydet.Size = new Size(130, 32);
            btnWavKaydet.Text = ".wav Olarak Kaydet";
            btnWavKaydet.Click += BtnWavKaydet_Click;
            //
            // grpStt
            //
            grpStt.Controls.Add(splitStt);
            grpStt.Controls.Add(pnlSttButon);
            grpStt.Controls.Add(pnlSttBilgi);
            grpStt.Dock = DockStyle.Fill;
            grpStt.Name = "grpStt";
            grpStt.Padding = new Padding(8);
            grpStt.Text = "Speech-to-Text — mikrofondan konuşmayı metne çevir";
            //
            // splitStt
            //
            splitStt.Dock = DockStyle.Fill;
            splitStt.Name = "splitStt";
            splitStt.Panel1.Controls.Add(txtTaninan);
            splitStt.Panel2.Controls.Add(lvSonuclar);
            splitStt.SplitterDistance = 420;
            //
            // txtTaninan
            //
            txtTaninan.Dock = DockStyle.Fill;
            txtTaninan.Font = new Font("Segoe UI", 11F);
            txtTaninan.Multiline = true;
            txtTaninan.Name = "txtTaninan";
            txtTaninan.PlaceholderText = "Tanınan konuşma burada görünecek";
            txtTaninan.ScrollBars = ScrollBars.Vertical;
            //
            // lvSonuclar
            //
            lvSonuclar.Columns.AddRange(new ColumnHeader[] { colSaat, colGuven, colMetin });
            lvSonuclar.Dock = DockStyle.Fill;
            lvSonuclar.FullRowSelect = true;
            lvSonuclar.GridLines = true;
            lvSonuclar.Name = "lvSonuclar";
            lvSonuclar.View = View.Details;
            colSaat.Text = "Saat";
            colSaat.Width = 70;
            colGuven.Text = "Güven";
            colGuven.Width = 55;
            colMetin.Text = "Tanınan cümle";
            colMetin.Width = 260;
            //
            // pnlSttBilgi
            //
            pnlSttBilgi.AutoSize = true;
            pnlSttBilgi.Controls.Add(lblDil);
            pnlSttBilgi.Controls.Add(lblSeviye);
            pnlSttBilgi.Controls.Add(pbSeviye);
            pnlSttBilgi.Controls.Add(lblTahmin);
            pnlSttBilgi.Dock = DockStyle.Top;
            pnlSttBilgi.Name = "pnlSttBilgi";
            //
            // lblDil
            //
            lblDil.AutoSize = true;
            lblDil.Margin = new Padding(3, 6, 16, 6);
            lblDil.Name = "lblDil";
            lblDil.Text = "Tanıma dili: -";
            //
            // lblSeviye, pbSeviye
            //
            lblSeviye.AutoSize = true;
            lblSeviye.Margin = new Padding(3, 6, 3, 6);
            lblSeviye.Name = "lblSeviye";
            lblSeviye.Text = "Mikrofon seviyesi:";
            pbSeviye.Margin = new Padding(3, 6, 16, 3);
            pbSeviye.Name = "pbSeviye";
            pbSeviye.Size = new Size(120, 16);
            //
            // lblTahmin
            //
            lblTahmin.AutoSize = true;
            lblTahmin.ForeColor = SystemColors.GrayText;
            lblTahmin.Margin = new Padding(3, 6, 3, 6);
            lblTahmin.Name = "lblTahmin";
            lblTahmin.Text = "";
            //
            // pnlSttButon
            //
            pnlSttButon.AutoSize = true;
            pnlSttButon.Controls.Add(btnDinle);
            pnlSttButon.Controls.Add(btnDinlemeDurdur);
            pnlSttButon.Controls.Add(btnDosyadanTani);
            pnlSttButon.Controls.Add(btnSttTemizle);
            pnlSttButon.Controls.Add(btnTxtKaydet);
            pnlSttButon.Dock = DockStyle.Bottom;
            pnlSttButon.Name = "pnlSttButon";
            //
            // STT butonları
            //
            btnDinle.Name = "btnDinle";
            btnDinle.Size = new Size(140, 32);
            btnDinle.Text = "🎤 Dinlemeyi Başlat";
            btnDinle.Click += BtnDinle_Click;
            btnDinlemeDurdur.Enabled = false;
            btnDinlemeDurdur.Name = "btnDinlemeDurdur";
            btnDinlemeDurdur.Size = new Size(90, 32);
            btnDinlemeDurdur.Text = "■ Durdur";
            btnDinlemeDurdur.Click += BtnDinlemeDurdur_Click;
            btnDosyadanTani.Name = "btnDosyadanTani";
            btnDosyadanTani.Size = new Size(140, 32);
            btnDosyadanTani.Text = ".wav Dosyasından Tanı";
            btnDosyadanTani.Click += BtnDosyadanTani_Click;
            btnSttTemizle.Name = "btnSttTemizle";
            btnSttTemizle.Size = new Size(90, 32);
            btnSttTemizle.Text = "Temizle";
            btnSttTemizle.Click += BtnSttTemizle_Click;
            btnTxtKaydet.Name = "btnTxtKaydet";
            btnTxtKaydet.Size = new Size(120, 32);
            btnTxtKaydet.Text = ".txt Olarak Kaydet";
            btnTxtKaydet.Click += BtnTxtKaydet_Click;
            //
            // statusStrip
            //
            statusStrip.Items.AddRange(new ToolStripItem[] { lblDurumAlt });
            statusStrip.Name = "statusStrip";
            lblDurumAlt.Name = "lblDurumAlt";
            lblDurumAlt.Spring = true;
            lblDurumAlt.TextAlign = ContentAlignment.MiddleLeft;
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 640);
            Controls.Add(grpStt);
            Controls.Add(grpTts);
            Controls.Add(lblAktif);
            Controls.Add(statusStrip);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(820, 600);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Speech-to-Text / Text-to-Speech";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)trkHiz).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkDuzey).EndInit();
            splitStt.Panel1.ResumeLayout(false);
            splitStt.Panel1.PerformLayout();
            splitStt.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitStt).EndInit();
            splitStt.ResumeLayout(false);
            grpTts.ResumeLayout(false);
            grpTts.PerformLayout();
            grpStt.ResumeLayout(false);
            grpStt.PerformLayout();
            pnlTtsAyar.ResumeLayout(false);
            pnlTtsAyar.PerformLayout();
            pnlTtsButon.ResumeLayout(false);
            pnlSttBilgi.ResumeLayout(false);
            pnlSttBilgi.PerformLayout();
            pnlSttButon.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblAktif;
        private GroupBox grpTts;
        private RichTextBox rtbMetin;
        private FlowLayoutPanel pnlTtsAyar;
        private Label lblSes;
        private ComboBox cmbSes;
        private Label lblHiz;
        private TrackBar trkHiz;
        private Label lblDuzey;
        private TrackBar trkDuzey;
        private FlowLayoutPanel pnlTtsButon;
        private Button btnOku;
        private Button btnDuraklat;
        private Button btnDurdur;
        private Button btnTtsTemizle;
        private Button btnWavKaydet;
        private GroupBox grpStt;
        private SplitContainer splitStt;
        private TextBox txtTaninan;
        private ListView lvSonuclar;
        private ColumnHeader colSaat;
        private ColumnHeader colGuven;
        private ColumnHeader colMetin;
        private FlowLayoutPanel pnlSttBilgi;
        private Label lblDil;
        private Label lblSeviye;
        private ProgressBar pbSeviye;
        private Label lblTahmin;
        private FlowLayoutPanel pnlSttButon;
        private Button btnDinle;
        private Button btnDinlemeDurdur;
        private Button btnDosyadanTani;
        private Button btnSttTemizle;
        private Button btnTxtKaydet;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblDurumAlt;
    }
}
