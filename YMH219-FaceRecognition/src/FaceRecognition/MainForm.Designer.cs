#nullable disable

namespace FaceRecognition
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
            pbGoruntu = new PictureBox();
            pnlSag = new FlowLayoutPanel();
            grpKaynak = new GroupBox();
            btnKameraBaslat = new Button();
            btnKameraDurdur = new Button();
            btnGoruntuSec = new Button();
            grpSonuc = new GroupBox();
            lblSonuc = new Label();
            lblYuzSayisi = new Label();
            lblEsik = new Label();
            nudEsik = new NumericUpDown();
            btnYuzleriKaydet = new Button();
            grpEgitim = new GroupBox();
            lblEtiket = new Label();
            txtEtiket = new TextBox();
            chkVeriArtirma = new CheckBox();
            btnOrnekEkle = new Button();
            lvEtiketler = new ListView();
            colEtiket = new ColumnHeader();
            colOrnek = new ColumnHeader();
            btnEgit = new Button();
            btnEtiketSil = new Button();
            btnKlasorAc = new Button();
            statusStrip = new StatusStrip();
            lblDurum = new ToolStripStatusLabel();
            lblModel = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)pbGoruntu).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudEsik).BeginInit();
            pnlSag.SuspendLayout();
            grpKaynak.SuspendLayout();
            grpSonuc.SuspendLayout();
            grpEgitim.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            //
            // pbGoruntu
            //
            pbGoruntu.BackColor = Color.FromArgb(30, 30, 30);
            pbGoruntu.Dock = DockStyle.Fill;
            pbGoruntu.Name = "pbGoruntu";
            pbGoruntu.SizeMode = PictureBoxSizeMode.Zoom;
            //
            // pnlSag
            //
            pnlSag.Controls.Add(grpKaynak);
            pnlSag.Controls.Add(grpSonuc);
            pnlSag.Controls.Add(grpEgitim);
            pnlSag.Dock = DockStyle.Right;
            pnlSag.FlowDirection = FlowDirection.TopDown;
            pnlSag.Name = "pnlSag";
            pnlSag.Padding = new Padding(6);
            pnlSag.Size = new Size(300, 640);
            pnlSag.WrapContents = false;
            //
            // grpKaynak
            //
            grpKaynak.Controls.Add(btnKameraBaslat);
            grpKaynak.Controls.Add(btnKameraDurdur);
            grpKaynak.Controls.Add(btnGoruntuSec);
            grpKaynak.Name = "grpKaynak";
            grpKaynak.Size = new Size(285, 100);
            grpKaynak.Text = "Görüntü kaynağı";
            btnKameraBaslat.Location = new Point(10, 22);
            btnKameraBaslat.Name = "btnKameraBaslat";
            btnKameraBaslat.Size = new Size(128, 32);
            btnKameraBaslat.Text = "Kamera Başlat";
            btnKameraBaslat.Click += BtnKameraBaslat_Click;
            btnKameraDurdur.Enabled = false;
            btnKameraDurdur.Location = new Point(146, 22);
            btnKameraDurdur.Name = "btnKameraDurdur";
            btnKameraDurdur.Size = new Size(128, 32);
            btnKameraDurdur.Text = "Kamera Durdur";
            btnKameraDurdur.Click += BtnKameraDurdur_Click;
            btnGoruntuSec.Location = new Point(10, 60);
            btnGoruntuSec.Name = "btnGoruntuSec";
            btnGoruntuSec.Size = new Size(264, 32);
            btnGoruntuSec.Text = "Görüntü Seç (dosyadan)...";
            btnGoruntuSec.Click += BtnGoruntuSec_Click;
            //
            // grpSonuc
            //
            grpSonuc.Controls.Add(lblSonuc);
            grpSonuc.Controls.Add(lblYuzSayisi);
            grpSonuc.Controls.Add(lblEsik);
            grpSonuc.Controls.Add(nudEsik);
            grpSonuc.Controls.Add(btnYuzleriKaydet);
            grpSonuc.Name = "grpSonuc";
            grpSonuc.Size = new Size(285, 184);
            grpSonuc.Text = "Tanıma sonucu";
            lblSonuc.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblSonuc.Location = new Point(10, 22);
            lblSonuc.Name = "lblSonuc";
            lblSonuc.Size = new Size(264, 66);
            lblSonuc.Text = "-";
            lblYuzSayisi.ForeColor = SystemColors.GrayText;
            lblYuzSayisi.Location = new Point(10, 90);
            lblYuzSayisi.Name = "lblYuzSayisi";
            lblYuzSayisi.Size = new Size(264, 20);
            lblYuzSayisi.Text = "Algılanan yüz: 0";
            lblEsik.Location = new Point(10, 116);
            lblEsik.Name = "lblEsik";
            lblEsik.Size = new Size(170, 20);
            lblEsik.Text = "Tanıma eşiği (LBPH mesafesi):";
            nudEsik.Location = new Point(194, 113);
            nudEsik.Maximum = 200;
            nudEsik.Minimum = 10;
            nudEsik.Name = "nudEsik";
            nudEsik.Size = new Size(80, 23);
            nudEsik.Value = 80;
            nudEsik.ValueChanged += NudEsik_ValueChanged;
            btnYuzleriKaydet.Location = new Point(10, 142);
            btnYuzleriKaydet.Name = "btnYuzleriKaydet";
            btnYuzleriKaydet.Size = new Size(264, 32);
            btnYuzleriKaydet.Text = "Algılanan Yüzleri Klasöre Kaydet";
            btnYuzleriKaydet.Click += BtnYuzleriKaydet_Click;
            //
            // grpEgitim
            //
            grpEgitim.Controls.Add(lblEtiket);
            grpEgitim.Controls.Add(txtEtiket);
            grpEgitim.Controls.Add(chkVeriArtirma);
            grpEgitim.Controls.Add(btnOrnekEkle);
            grpEgitim.Controls.Add(lvEtiketler);
            grpEgitim.Controls.Add(btnEgit);
            grpEgitim.Controls.Add(btnEtiketSil);
            grpEgitim.Controls.Add(btnKlasorAc);
            grpEgitim.Name = "grpEgitim";
            grpEgitim.Size = new Size(285, 330);
            grpEgitim.Text = "Eğitim (yerel örnekler)";
            lblEtiket.Location = new Point(10, 25);
            lblEtiket.Name = "lblEtiket";
            lblEtiket.Size = new Size(50, 20);
            lblEtiket.Text = "Etiket:";
            txtEtiket.Location = new Point(64, 22);
            txtEtiket.MaxLength = 30;
            txtEtiket.Name = "txtEtiket";
            txtEtiket.PlaceholderText = "kişi adı";
            txtEtiket.Size = new Size(210, 23);
            chkVeriArtirma.AutoSize = true;
            chkVeriArtirma.Checked = true;
            chkVeriArtirma.Location = new Point(12, 52);
            chkVeriArtirma.Name = "chkVeriArtirma";
            chkVeriArtirma.Text = "Veri artırma (ayna, ışık, döndürme)";
            btnOrnekEkle.Location = new Point(10, 76);
            btnOrnekEkle.Name = "btnOrnekEkle";
            btnOrnekEkle.Size = new Size(264, 32);
            btnOrnekEkle.Text = "Görüntüdeki Yüzü Örnek Olarak Ekle";
            btnOrnekEkle.Click += BtnOrnekEkle_Click;
            lvEtiketler.Columns.AddRange(new ColumnHeader[] { colEtiket, colOrnek });
            lvEtiketler.FullRowSelect = true;
            lvEtiketler.GridLines = true;
            lvEtiketler.Location = new Point(10, 116);
            lvEtiketler.MultiSelect = false;
            lvEtiketler.Name = "lvEtiketler";
            lvEtiketler.Size = new Size(264, 130);
            lvEtiketler.View = View.Details;
            colEtiket.Text = "Etiket";
            colEtiket.Width = 170;
            colOrnek.Text = "Örnek";
            colOrnek.Width = 70;
            btnEgit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnEgit.Location = new Point(10, 252);
            btnEgit.Name = "btnEgit";
            btnEgit.Size = new Size(264, 32);
            btnEgit.Text = "Modeli Eğit";
            btnEgit.Click += BtnEgit_Click;
            btnEtiketSil.Location = new Point(10, 290);
            btnEtiketSil.Name = "btnEtiketSil";
            btnEtiketSil.Size = new Size(128, 30);
            btnEtiketSil.Text = "Seçili Etiketi Sil";
            btnEtiketSil.Click += BtnEtiketSil_Click;
            btnKlasorAc.Location = new Point(146, 290);
            btnKlasorAc.Name = "btnKlasorAc";
            btnKlasorAc.Size = new Size(128, 30);
            btnKlasorAc.Text = "Örnek Klasörünü Aç";
            btnKlasorAc.Click += BtnKlasorAc_Click;
            //
            // statusStrip
            //
            statusStrip.Items.AddRange(new ToolStripItem[] { lblDurum, lblModel });
            statusStrip.Name = "statusStrip";
            lblDurum.Name = "lblDurum";
            lblDurum.Spring = true;
            lblDurum.Text = "Hazır";
            lblDurum.TextAlign = ContentAlignment.MiddleLeft;
            lblModel.Name = "lblModel";
            lblModel.Text = "Model: eğitilmedi";
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 660);
            Controls.Add(pbGoruntu);
            Controls.Add(pnlSag);
            Controls.Add(statusStrip);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(860, 640);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Yüz Algılama ve Tanıma (OpenCV)";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)pbGoruntu).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudEsik).EndInit();
            pnlSag.ResumeLayout(false);
            grpKaynak.ResumeLayout(false);
            grpSonuc.ResumeLayout(false);
            grpEgitim.ResumeLayout(false);
            grpEgitim.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pbGoruntu;
        private FlowLayoutPanel pnlSag;
        private GroupBox grpKaynak;
        private Button btnKameraBaslat;
        private Button btnKameraDurdur;
        private Button btnGoruntuSec;
        private GroupBox grpSonuc;
        private Label lblSonuc;
        private Label lblYuzSayisi;
        private Label lblEsik;
        private NumericUpDown nudEsik;
        private Button btnYuzleriKaydet;
        private GroupBox grpEgitim;
        private Label lblEtiket;
        private TextBox txtEtiket;
        private CheckBox chkVeriArtirma;
        private Button btnOrnekEkle;
        private ListView lvEtiketler;
        private ColumnHeader colEtiket;
        private ColumnHeader colOrnek;
        private Button btnEgit;
        private Button btnEtiketSil;
        private Button btnKlasorAc;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblDurum;
        private ToolStripStatusLabel lblModel;
    }
}
