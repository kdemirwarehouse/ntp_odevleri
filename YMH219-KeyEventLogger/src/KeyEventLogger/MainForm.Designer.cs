#nullable disable

namespace KeyEventLogger
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
            pnlUst = new FlowLayoutPanel();
            lblGosterge = new Label();
            btnBaslat = new Button();
            btnDurdur = new Button();
            btnTemizle = new Button();
            btnKaydet = new Button();
            lblAyrac = new Label();
            chkKeyDown = new CheckBox();
            chkKeyUp = new CheckBox();
            chkKeyPress = new CheckBox();
            tblAna = new TableLayoutPanel();
            lblDeneme = new Label();
            txtDeneme = new TextBox();
            lvKayitlar = new ListView();
            colZaman = new ColumnHeader();
            colOlay = new ColumnHeader();
            colTus = new ColumnHeader();
            colAyrinti = new ColumnHeader();
            pnlSag = new FlowLayoutPanel();
            grpIstatistik = new GroupBox();
            lblToplamTus = new Label();
            lblOlaySayisi = new Label();
            lblEnSikBaslik = new Label();
            lstEnSik = new ListBox();
            grpFiltre = new GroupBox();
            lblFiltreBas = new Label();
            dtpBaslangic = new DateTimePicker();
            lblFiltreBit = new Label();
            dtpBitis = new DateTimePicker();
            btnFiltrele = new Button();
            btnFiltreKaldir = new Button();
            lblGizlilik = new Label();
            statusStrip = new StatusStrip();
            lblDurum = new ToolStripStatusLabel();
            pnlUst.SuspendLayout();
            tblAna.SuspendLayout();
            pnlSag.SuspendLayout();
            grpIstatistik.SuspendLayout();
            grpFiltre.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            //
            // pnlUst
            //
            pnlUst.AutoSize = true;
            pnlUst.Controls.Add(lblGosterge);
            pnlUst.Controls.Add(btnBaslat);
            pnlUst.Controls.Add(btnDurdur);
            pnlUst.Controls.Add(btnTemizle);
            pnlUst.Controls.Add(btnKaydet);
            pnlUst.Controls.Add(lblAyrac);
            pnlUst.Controls.Add(chkKeyDown);
            pnlUst.Controls.Add(chkKeyUp);
            pnlUst.Controls.Add(chkKeyPress);
            pnlUst.Dock = DockStyle.Top;
            pnlUst.Name = "pnlUst";
            pnlUst.Padding = new Padding(8, 8, 8, 4);
            //
            // lblGosterge
            //
            lblGosterge.BackColor = Color.DimGray;
            lblGosterge.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblGosterge.ForeColor = Color.White;
            lblGosterge.Margin = new Padding(0, 0, 12, 0);
            lblGosterge.Name = "lblGosterge";
            lblGosterge.Size = new Size(170, 32);
            lblGosterge.Text = "■  KAYIT KAPALI";
            lblGosterge.TextAlign = ContentAlignment.MiddleCenter;
            //
            // btnBaslat
            //
            btnBaslat.Name = "btnBaslat";
            btnBaslat.Size = new Size(90, 32);
            btnBaslat.TabIndex = 0;
            btnBaslat.Text = "▶ Başlat";
            btnBaslat.Click += BtnBaslat_Click;
            //
            // btnDurdur
            //
            btnDurdur.Enabled = false;
            btnDurdur.Name = "btnDurdur";
            btnDurdur.Size = new Size(90, 32);
            btnDurdur.TabIndex = 1;
            btnDurdur.Text = "■ Durdur";
            btnDurdur.Click += BtnDurdur_Click;
            //
            // btnTemizle
            //
            btnTemizle.Name = "btnTemizle";
            btnTemizle.Size = new Size(90, 32);
            btnTemizle.TabIndex = 2;
            btnTemizle.Text = "Temizle";
            btnTemizle.Click += BtnTemizle_Click;
            //
            // btnKaydet
            //
            btnKaydet.Name = "btnKaydet";
            btnKaydet.Size = new Size(120, 32);
            btnKaydet.TabIndex = 3;
            btnKaydet.Text = ".txt Olarak Kaydet";
            btnKaydet.Click += BtnKaydet_Click;
            //
            // lblAyrac
            //
            lblAyrac.Name = "lblAyrac";
            lblAyrac.Size = new Size(110, 32);
            lblAyrac.Text = "Yakalanan olaylar:";
            lblAyrac.TextAlign = ContentAlignment.MiddleRight;
            //
            // chkKeyDown
            //
            chkKeyDown.AutoSize = true;
            chkKeyDown.Checked = true;
            chkKeyDown.Margin = new Padding(3, 8, 3, 3);
            chkKeyDown.Name = "chkKeyDown";
            chkKeyDown.Text = "KeyDown";
            //
            // chkKeyUp
            //
            chkKeyUp.AutoSize = true;
            chkKeyUp.Checked = true;
            chkKeyUp.Margin = new Padding(3, 8, 3, 3);
            chkKeyUp.Name = "chkKeyUp";
            chkKeyUp.Text = "KeyUp";
            //
            // chkKeyPress
            //
            chkKeyPress.AutoSize = true;
            chkKeyPress.Checked = true;
            chkKeyPress.Margin = new Padding(3, 8, 3, 3);
            chkKeyPress.Name = "chkKeyPress";
            chkKeyPress.Text = "KeyPress";
            //
            // tblAna
            //
            tblAna.ColumnCount = 2;
            tblAna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblAna.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
            tblAna.Controls.Add(lblDeneme, 0, 0);
            tblAna.Controls.Add(txtDeneme, 0, 1);
            tblAna.Controls.Add(lvKayitlar, 0, 2);
            tblAna.Controls.Add(pnlSag, 1, 0);
            tblAna.SetRowSpan(pnlSag, 3);
            tblAna.Dock = DockStyle.Fill;
            tblAna.Name = "tblAna";
            tblAna.Padding = new Padding(8, 0, 8, 4);
            tblAna.RowCount = 3;
            tblAna.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblAna.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            tblAna.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            //
            // lblDeneme
            //
            lblDeneme.AutoSize = true;
            lblDeneme.Margin = new Padding(3, 6, 3, 3);
            lblDeneme.Name = "lblDeneme";
            lblDeneme.Text = "Deneme alanı (kişisel bilgi veya parola yazmayın):";
            //
            // txtDeneme
            //
            txtDeneme.Dock = DockStyle.Fill;
            txtDeneme.Font = new Font("Segoe UI", 11F);
            txtDeneme.Multiline = true;
            txtDeneme.Name = "txtDeneme";
            txtDeneme.ScrollBars = ScrollBars.Vertical;
            txtDeneme.TabIndex = 4;
            //
            // lvKayitlar
            //
            lvKayitlar.Columns.AddRange(new ColumnHeader[] { colZaman, colOlay, colTus, colAyrinti });
            lvKayitlar.Dock = DockStyle.Fill;
            lvKayitlar.Font = new Font("Consolas", 9.5F);
            lvKayitlar.FullRowSelect = true;
            lvKayitlar.GridLines = true;
            lvKayitlar.Name = "lvKayitlar";
            lvKayitlar.TabIndex = 5;
            lvKayitlar.View = View.Details;
            //
            // columns
            //
            colZaman.Text = "Zaman";
            colZaman.Width = 190;
            colOlay.Text = "Olay";
            colOlay.Width = 85;
            colTus.Text = "Tuş";
            colTus.Width = 110;
            colAyrinti.Text = "Ayrıntı";
            colAyrinti.Width = 230;
            //
            // pnlSag
            //
            pnlSag.Controls.Add(grpIstatistik);
            pnlSag.Controls.Add(grpFiltre);
            pnlSag.Controls.Add(lblGizlilik);
            pnlSag.Dock = DockStyle.Fill;
            pnlSag.FlowDirection = FlowDirection.TopDown;
            pnlSag.Name = "pnlSag";
            pnlSag.WrapContents = false;
            //
            // grpIstatistik
            //
            grpIstatistik.Controls.Add(lstEnSik);
            grpIstatistik.Controls.Add(lblEnSikBaslik);
            grpIstatistik.Controls.Add(lblOlaySayisi);
            grpIstatistik.Controls.Add(lblToplamTus);
            grpIstatistik.Name = "grpIstatistik";
            grpIstatistik.Size = new Size(255, 205);
            grpIstatistik.Text = "İstatistik";
            //
            // lblToplamTus
            //
            lblToplamTus.Location = new Point(10, 22);
            lblToplamTus.Name = "lblToplamTus";
            lblToplamTus.Size = new Size(235, 20);
            lblToplamTus.Text = "Toplam tuş basımı: 0";
            //
            // lblOlaySayisi
            //
            lblOlaySayisi.Location = new Point(10, 44);
            lblOlaySayisi.Name = "lblOlaySayisi";
            lblOlaySayisi.Size = new Size(235, 20);
            lblOlaySayisi.Text = "Kayıtlı olay: 0 / 5000";
            //
            // lblEnSikBaslik
            //
            lblEnSikBaslik.Location = new Point(10, 68);
            lblEnSikBaslik.Name = "lblEnSikBaslik";
            lblEnSikBaslik.Size = new Size(235, 20);
            lblEnSikBaslik.Text = "En sık kullanılan 5 tuş:";
            //
            // lstEnSik
            //
            lstEnSik.IntegralHeight = false;
            lstEnSik.Location = new Point(10, 90);
            lstEnSik.Name = "lstEnSik";
            lstEnSik.Size = new Size(235, 105);
            lstEnSik.TabStop = false;
            //
            // grpFiltre
            //
            grpFiltre.Controls.Add(btnFiltreKaldir);
            grpFiltre.Controls.Add(btnFiltrele);
            grpFiltre.Controls.Add(dtpBitis);
            grpFiltre.Controls.Add(lblFiltreBit);
            grpFiltre.Controls.Add(dtpBaslangic);
            grpFiltre.Controls.Add(lblFiltreBas);
            grpFiltre.Name = "grpFiltre";
            grpFiltre.Size = new Size(255, 150);
            grpFiltre.Text = "Zaman aralığına göre filtrele";
            //
            // lblFiltreBas
            //
            lblFiltreBas.Location = new Point(10, 26);
            lblFiltreBas.Name = "lblFiltreBas";
            lblFiltreBas.Size = new Size(70, 20);
            lblFiltreBas.Text = "Başlangıç:";
            //
            // dtpBaslangic
            //
            dtpBaslangic.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            dtpBaslangic.Format = DateTimePickerFormat.Custom;
            dtpBaslangic.Location = new Point(85, 22);
            dtpBaslangic.Name = "dtpBaslangic";
            dtpBaslangic.ShowUpDown = true;
            dtpBaslangic.Size = new Size(160, 23);
            //
            // lblFiltreBit
            //
            lblFiltreBit.Location = new Point(10, 60);
            lblFiltreBit.Name = "lblFiltreBit";
            lblFiltreBit.Size = new Size(70, 20);
            lblFiltreBit.Text = "Bitiş:";
            //
            // dtpBitis
            //
            dtpBitis.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            dtpBitis.Format = DateTimePickerFormat.Custom;
            dtpBitis.Location = new Point(85, 56);
            dtpBitis.Name = "dtpBitis";
            dtpBitis.ShowUpDown = true;
            dtpBitis.Size = new Size(160, 23);
            //
            // btnFiltrele
            //
            btnFiltrele.Location = new Point(10, 98);
            btnFiltrele.Name = "btnFiltrele";
            btnFiltrele.Size = new Size(110, 32);
            btnFiltrele.Text = "Filtrele";
            btnFiltrele.Click += BtnFiltrele_Click;
            //
            // btnFiltreKaldir
            //
            btnFiltreKaldir.Enabled = false;
            btnFiltreKaldir.Location = new Point(135, 98);
            btnFiltreKaldir.Name = "btnFiltreKaldir";
            btnFiltreKaldir.Size = new Size(110, 32);
            btnFiltreKaldir.Text = "Filtreyi Kaldır";
            btnFiltreKaldir.Click += BtnFiltreKaldir_Click;
            //
            // lblGizlilik
            //
            lblGizlilik.ForeColor = SystemColors.GrayText;
            lblGizlilik.Margin = new Padding(3, 10, 3, 3);
            lblGizlilik.Name = "lblGizlilik";
            lblGizlilik.Size = new Size(255, 120);
            lblGizlilik.Text = "Eğitsel uygulama: Yalnızca bu pencere odaktayken olaylar yakalanır. " +
                "Global klavye kancası, arka planda çalışma veya ağ iletişimi yoktur. " +
                "Pencere odaktan çıkınca kayıt otomatik durur. Kayıtlar yalnızca siz " +
                "\".txt Olarak Kaydet\" dediğinizde diske yazılır.";
            //
            // statusStrip
            //
            statusStrip.Items.AddRange(new ToolStripItem[] { lblDurum });
            statusStrip.Name = "statusStrip";
            //
            // lblDurum
            //
            lblDurum.Name = "lblDurum";
            lblDurum.Spring = true;
            lblDurum.Text = "Hazır. Kayda başlamak için Başlat'a basın.";
            lblDurum.TextAlign = ContentAlignment.MiddleLeft;
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 621);
            Controls.Add(tblAna);
            Controls.Add(statusStrip);
            Controls.Add(pnlUst);
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;
            MinimumSize = new Size(900, 560);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Key Event Logger (Eğitsel)";
            Deactivate += MainForm_Deactivate;
            KeyDown += MainForm_KeyDown;
            KeyPress += MainForm_KeyPress;
            KeyUp += MainForm_KeyUp;
            pnlUst.ResumeLayout(false);
            pnlUst.PerformLayout();
            tblAna.ResumeLayout(false);
            tblAna.PerformLayout();
            pnlSag.ResumeLayout(false);
            grpIstatistik.ResumeLayout(false);
            grpFiltre.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private FlowLayoutPanel pnlUst;
        private Label lblGosterge;
        private Button btnBaslat;
        private Button btnDurdur;
        private Button btnTemizle;
        private Button btnKaydet;
        private Label lblAyrac;
        private CheckBox chkKeyDown;
        private CheckBox chkKeyUp;
        private CheckBox chkKeyPress;
        private TableLayoutPanel tblAna;
        private Label lblDeneme;
        private TextBox txtDeneme;
        private ListView lvKayitlar;
        private ColumnHeader colZaman;
        private ColumnHeader colOlay;
        private ColumnHeader colTus;
        private ColumnHeader colAyrinti;
        private FlowLayoutPanel pnlSag;
        private GroupBox grpIstatistik;
        private Label lblToplamTus;
        private Label lblOlaySayisi;
        private Label lblEnSikBaslik;
        private ListBox lstEnSik;
        private GroupBox grpFiltre;
        private Label lblFiltreBas;
        private DateTimePicker dtpBaslangic;
        private Label lblFiltreBit;
        private DateTimePicker dtpBitis;
        private Button btnFiltrele;
        private Button btnFiltreKaldir;
        private Label lblGizlilik;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblDurum;
    }
}
