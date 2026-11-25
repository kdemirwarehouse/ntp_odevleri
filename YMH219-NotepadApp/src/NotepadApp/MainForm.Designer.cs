#nullable disable

namespace NotepadApp
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
            menuStrip = new MenuStrip();
            mnuDosya = new ToolStripMenuItem();
            mnuYeni = new ToolStripMenuItem();
            mnuKaydet = new ToolStripMenuItem();
            mnuYenidenYukle = new ToolStripMenuItem();
            mnuDisaAktar = new ToolStripMenuItem();
            mnuAyrac = new ToolStripSeparator();
            mnuCikis = new ToolStripMenuItem();
            mnuNot = new ToolStripMenuItem();
            mnuSil = new ToolStripMenuItem();
            mnuBicim = new ToolStripMenuItem();
            mnuKalin = new ToolStripMenuItem();
            mnuItalik = new ToolStripMenuItem();
            mnuOtomatikKaydet = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            lblDurum = new ToolStripStatusLabel();
            lblDosya = new ToolStripStatusLabel();
            splitContainer = new SplitContainer();
            lstNotlar = new ListBox();
            pnlFiltre = new TableLayoutPanel();
            lblAra = new Label();
            txtAra = new TextBox();
            lblFiltreKategori = new Label();
            cmbFiltreKategori = new ComboBox();
            lblNotSayisi = new Label();
            pnlDuzenleyici = new TableLayoutPanel();
            lblBaslik = new Label();
            txtBaslik = new TextBox();
            lblKategori = new Label();
            cmbKategori = new ComboBox();
            lblIcerik = new Label();
            rtbIcerik = new RichTextBox();
            lblTarihler = new Label();
            pnlButonlar = new FlowLayoutPanel();
            btnYeni = new Button();
            btnKaydet = new Button();
            btnSil = new Button();
            btnDisaAktar = new Button();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            pnlFiltre.SuspendLayout();
            pnlDuzenleyici.SuspendLayout();
            pnlButonlar.SuspendLayout();
            SuspendLayout();
            //
            // menuStrip
            //
            menuStrip.Items.AddRange(new ToolStripItem[] { mnuDosya, mnuNot, mnuBicim });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(984, 24);
            //
            // mnuDosya
            //
            mnuDosya.DropDownItems.AddRange(new ToolStripItem[] { mnuYeni, mnuKaydet, mnuYenidenYukle, mnuDisaAktar, mnuAyrac, mnuCikis });
            mnuDosya.Name = "mnuDosya";
            mnuDosya.Text = "&Dosya";
            //
            // mnuYeni
            //
            mnuYeni.Name = "mnuYeni";
            mnuYeni.ShortcutKeys = Keys.Control | Keys.N;
            mnuYeni.Text = "&Yeni Not";
            mnuYeni.Click += BtnYeni_Click;
            //
            // mnuKaydet
            //
            mnuKaydet.Name = "mnuKaydet";
            mnuKaydet.ShortcutKeys = Keys.Control | Keys.S;
            mnuKaydet.Text = "&Kaydet";
            mnuKaydet.Click += BtnKaydet_Click;
            //
            // mnuYenidenYukle
            //
            mnuYenidenYukle.Name = "mnuYenidenYukle";
            mnuYenidenYukle.ShortcutKeys = Keys.F5;
            mnuYenidenYukle.Text = "Dosyadan &Yeniden Yükle";
            mnuYenidenYukle.Click += MnuYenidenYukle_Click;
            //
            // mnuDisaAktar
            //
            mnuDisaAktar.Name = "mnuDisaAktar";
            mnuDisaAktar.ShortcutKeys = Keys.Control | Keys.E;
            mnuDisaAktar.Text = "Notu .txt Olarak &Dışa Aktar...";
            mnuDisaAktar.Click += BtnDisaAktar_Click;
            //
            // mnuCikis
            //
            mnuCikis.Name = "mnuCikis";
            mnuCikis.Text = "Çı&kış";
            mnuCikis.Click += (_, _) => Close();
            //
            // mnuNot
            //
            mnuNot.DropDownItems.AddRange(new ToolStripItem[] { mnuSil, mnuOtomatikKaydet });
            mnuNot.Name = "mnuNot";
            mnuNot.Text = "&Not";
            //
            // mnuSil
            //
            mnuSil.Name = "mnuSil";
            mnuSil.ShortcutKeys = Keys.Control | Keys.Delete;
            mnuSil.Text = "Seçili Notu &Sil";
            mnuSil.Click += BtnSil_Click;
            //
            // mnuOtomatikKaydet
            //
            mnuOtomatikKaydet.Checked = true;
            mnuOtomatikKaydet.CheckOnClick = true;
            mnuOtomatikKaydet.Name = "mnuOtomatikKaydet";
            mnuOtomatikKaydet.Text = "Her İşlemden Sonra &Otomatik Kaydet";
            //
            // mnuBicim
            //
            mnuBicim.DropDownItems.AddRange(new ToolStripItem[] { mnuKalin, mnuItalik });
            mnuBicim.Name = "mnuBicim";
            mnuBicim.Text = "&Biçim";
            //
            // mnuKalin
            //
            mnuKalin.Name = "mnuKalin";
            mnuKalin.ShortcutKeys = Keys.Control | Keys.B;
            mnuKalin.Text = "Seçili Metni &Kalın Yap";
            mnuKalin.Click += MnuKalin_Click;
            //
            // mnuItalik
            //
            mnuItalik.Name = "mnuItalik";
            mnuItalik.ShortcutKeys = Keys.Control | Keys.I;
            mnuItalik.Text = "Seçili Metni &İtalik Yap";
            mnuItalik.Click += MnuItalik_Click;
            //
            // statusStrip
            //
            statusStrip.Items.AddRange(new ToolStripItem[] { lblDurum, lblDosya });
            statusStrip.Location = new Point(0, 639);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(984, 22);
            //
            // lblDurum
            //
            lblDurum.Name = "lblDurum";
            lblDurum.Spring = true;
            lblDurum.Text = "Hazır";
            lblDurum.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblDosya
            //
            lblDosya.ForeColor = SystemColors.GrayText;
            lblDosya.Name = "lblDosya";
            lblDosya.Text = "";
            //
            // splitContainer
            //
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 24);
            splitContainer.Name = "splitContainer";
            splitContainer.Panel1.Controls.Add(lstNotlar);
            splitContainer.Panel1.Controls.Add(pnlFiltre);
            splitContainer.Panel1.Padding = new Padding(8);
            splitContainer.Panel2.Controls.Add(pnlDuzenleyici);
            splitContainer.Panel2.Padding = new Padding(8);
            splitContainer.Size = new Size(984, 615);
            splitContainer.SplitterDistance = 340;
            //
            // lstNotlar
            //
            lstNotlar.Dock = DockStyle.Fill;
            lstNotlar.Font = new Font("Segoe UI", 10F);
            lstNotlar.IntegralHeight = false;
            lstNotlar.ItemHeight = 17;
            lstNotlar.Name = "lstNotlar";
            lstNotlar.TabIndex = 1;
            lstNotlar.SelectedIndexChanged += LstNotlar_SelectedIndexChanged;
            //
            // pnlFiltre
            //
            pnlFiltre.AutoSize = true;
            pnlFiltre.ColumnCount = 2;
            pnlFiltre.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlFiltre.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlFiltre.Controls.Add(lblAra, 0, 0);
            pnlFiltre.Controls.Add(txtAra, 1, 0);
            pnlFiltre.Controls.Add(lblFiltreKategori, 0, 1);
            pnlFiltre.Controls.Add(cmbFiltreKategori, 1, 1);
            pnlFiltre.Controls.Add(lblNotSayisi, 0, 2);
            pnlFiltre.SetColumnSpan(lblNotSayisi, 2);
            pnlFiltre.Dock = DockStyle.Top;
            pnlFiltre.Name = "pnlFiltre";
            pnlFiltre.RowCount = 3;
            pnlFiltre.TabIndex = 0;
            //
            // lblAra
            //
            lblAra.Anchor = AnchorStyles.Left;
            lblAra.AutoSize = true;
            lblAra.Name = "lblAra";
            lblAra.Text = "Ara:";
            //
            // txtAra
            //
            txtAra.Dock = DockStyle.Fill;
            txtAra.Name = "txtAra";
            txtAra.PlaceholderText = "Başlık veya içerikte ara...";
            txtAra.TabIndex = 0;
            txtAra.TextChanged += Filtre_Changed;
            //
            // lblFiltreKategori
            //
            lblFiltreKategori.Anchor = AnchorStyles.Left;
            lblFiltreKategori.AutoSize = true;
            lblFiltreKategori.Name = "lblFiltreKategori";
            lblFiltreKategori.Text = "Kategori:";
            //
            // cmbFiltreKategori
            //
            cmbFiltreKategori.Dock = DockStyle.Fill;
            cmbFiltreKategori.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFiltreKategori.Name = "cmbFiltreKategori";
            cmbFiltreKategori.TabIndex = 1;
            cmbFiltreKategori.SelectedIndexChanged += Filtre_Changed;
            //
            // lblNotSayisi
            //
            lblNotSayisi.AutoSize = true;
            lblNotSayisi.ForeColor = SystemColors.GrayText;
            lblNotSayisi.Margin = new Padding(3, 6, 3, 6);
            lblNotSayisi.Name = "lblNotSayisi";
            lblNotSayisi.Text = "0 not";
            //
            // pnlDuzenleyici
            //
            pnlDuzenleyici.ColumnCount = 2;
            pnlDuzenleyici.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlDuzenleyici.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlDuzenleyici.Controls.Add(lblBaslik, 0, 0);
            pnlDuzenleyici.Controls.Add(txtBaslik, 1, 0);
            pnlDuzenleyici.Controls.Add(lblKategori, 0, 1);
            pnlDuzenleyici.Controls.Add(cmbKategori, 1, 1);
            pnlDuzenleyici.Controls.Add(lblIcerik, 0, 2);
            pnlDuzenleyici.Controls.Add(rtbIcerik, 0, 3);
            pnlDuzenleyici.SetColumnSpan(rtbIcerik, 2);
            pnlDuzenleyici.Controls.Add(lblTarihler, 0, 4);
            pnlDuzenleyici.SetColumnSpan(lblTarihler, 2);
            pnlDuzenleyici.Controls.Add(pnlButonlar, 0, 5);
            pnlDuzenleyici.SetColumnSpan(pnlButonlar, 2);
            pnlDuzenleyici.Dock = DockStyle.Fill;
            pnlDuzenleyici.Name = "pnlDuzenleyici";
            pnlDuzenleyici.RowCount = 6;
            pnlDuzenleyici.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlDuzenleyici.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlDuzenleyici.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlDuzenleyici.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlDuzenleyici.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlDuzenleyici.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            //
            // lblBaslik
            //
            lblBaslik.Anchor = AnchorStyles.Left;
            lblBaslik.AutoSize = true;
            lblBaslik.Name = "lblBaslik";
            lblBaslik.Text = "Başlık:";
            //
            // txtBaslik
            //
            txtBaslik.Dock = DockStyle.Fill;
            txtBaslik.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            txtBaslik.MaxLength = 120;
            txtBaslik.Name = "txtBaslik";
            txtBaslik.PlaceholderText = "Not başlığı";
            txtBaslik.TabIndex = 2;
            txtBaslik.TextChanged += Duzenleyici_Changed;
            //
            // lblKategori
            //
            lblKategori.Anchor = AnchorStyles.Left;
            lblKategori.AutoSize = true;
            lblKategori.Name = "lblKategori";
            lblKategori.Text = "Kategori:";
            //
            // cmbKategori
            //
            cmbKategori.Dock = DockStyle.Left;
            cmbKategori.Name = "cmbKategori";
            cmbKategori.Size = new Size(220, 23);
            cmbKategori.TabIndex = 3;
            cmbKategori.TextChanged += Duzenleyici_Changed;
            //
            // lblIcerik
            //
            lblIcerik.AutoSize = true;
            lblIcerik.Margin = new Padding(3, 8, 3, 3);
            lblIcerik.Name = "lblIcerik";
            lblIcerik.Text = "İçerik:";
            //
            // rtbIcerik
            //
            rtbIcerik.AcceptsTab = true;
            rtbIcerik.Dock = DockStyle.Fill;
            rtbIcerik.Font = new Font("Segoe UI", 10.5F);
            rtbIcerik.Name = "rtbIcerik";
            rtbIcerik.TabIndex = 4;
            rtbIcerik.TextChanged += Duzenleyici_Changed;
            //
            // lblTarihler
            //
            lblTarihler.AutoSize = true;
            lblTarihler.ForeColor = SystemColors.GrayText;
            lblTarihler.Margin = new Padding(3, 6, 3, 3);
            lblTarihler.Name = "lblTarihler";
            lblTarihler.Text = "Yeni not";
            //
            // pnlButonlar
            //
            pnlButonlar.AutoSize = true;
            pnlButonlar.Controls.Add(btnYeni);
            pnlButonlar.Controls.Add(btnKaydet);
            pnlButonlar.Controls.Add(btnSil);
            pnlButonlar.Controls.Add(btnDisaAktar);
            pnlButonlar.Dock = DockStyle.Fill;
            pnlButonlar.Name = "pnlButonlar";
            //
            // btnYeni
            //
            btnYeni.AutoSize = true;
            btnYeni.Name = "btnYeni";
            btnYeni.Padding = new Padding(8, 2, 8, 2);
            btnYeni.TabIndex = 5;
            btnYeni.Text = "Yeni Not";
            btnYeni.Click += BtnYeni_Click;
            //
            // btnKaydet
            //
            btnKaydet.AutoSize = true;
            btnKaydet.Name = "btnKaydet";
            btnKaydet.Padding = new Padding(8, 2, 8, 2);
            btnKaydet.TabIndex = 6;
            btnKaydet.Text = "Kaydet";
            btnKaydet.Click += BtnKaydet_Click;
            //
            // btnSil
            //
            btnSil.AutoSize = true;
            btnSil.Name = "btnSil";
            btnSil.Padding = new Padding(8, 2, 8, 2);
            btnSil.TabIndex = 7;
            btnSil.Text = "Sil";
            btnSil.Click += BtnSil_Click;
            //
            // btnDisaAktar
            //
            btnDisaAktar.AutoSize = true;
            btnDisaAktar.Name = "btnDisaAktar";
            btnDisaAktar.Padding = new Padding(8, 2, 8, 2);
            btnDisaAktar.TabIndex = 8;
            btnDisaAktar.Text = ".txt Dışa Aktar";
            btnDisaAktar.Click += BtnDisaAktar_Click;
            //
            // MainForm
            //
            AcceptButton = null;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 661);
            Controls.Add(splitContainer);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            Font = new Font("Segoe UI", 9F);
            MainMenuStrip = menuStrip;
            MinimumSize = new Size(760, 480);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Not Defteri";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel1.PerformLayout();
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            pnlFiltre.ResumeLayout(false);
            pnlFiltre.PerformLayout();
            pnlDuzenleyici.ResumeLayout(false);
            pnlDuzenleyici.PerformLayout();
            pnlButonlar.ResumeLayout(false);
            pnlButonlar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip;
        private ToolStripMenuItem mnuDosya;
        private ToolStripMenuItem mnuYeni;
        private ToolStripMenuItem mnuKaydet;
        private ToolStripMenuItem mnuYenidenYukle;
        private ToolStripMenuItem mnuDisaAktar;
        private ToolStripSeparator mnuAyrac;
        private ToolStripMenuItem mnuCikis;
        private ToolStripMenuItem mnuNot;
        private ToolStripMenuItem mnuSil;
        private ToolStripMenuItem mnuOtomatikKaydet;
        private ToolStripMenuItem mnuBicim;
        private ToolStripMenuItem mnuKalin;
        private ToolStripMenuItem mnuItalik;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblDurum;
        private ToolStripStatusLabel lblDosya;
        private SplitContainer splitContainer;
        private ListBox lstNotlar;
        private TableLayoutPanel pnlFiltre;
        private Label lblAra;
        private TextBox txtAra;
        private Label lblFiltreKategori;
        private ComboBox cmbFiltreKategori;
        private Label lblNotSayisi;
        private TableLayoutPanel pnlDuzenleyici;
        private Label lblBaslik;
        private TextBox txtBaslik;
        private Label lblKategori;
        private ComboBox cmbKategori;
        private Label lblIcerik;
        private RichTextBox rtbIcerik;
        private Label lblTarihler;
        private FlowLayoutPanel pnlButonlar;
        private Button btnYeni;
        private Button btnKaydet;
        private Button btnSil;
        private Button btnDisaAktar;
    }
}
