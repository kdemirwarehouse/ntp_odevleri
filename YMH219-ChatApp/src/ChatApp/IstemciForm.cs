using System.Text;
using ChatApp.Core;

namespace ChatApp;

/// <summary>
/// İstemci penceresi: kullanıcı adıyla bağlanma, mesaj gönderme/alma, özel mesaj,
/// bağlantıyı kesme ve sohbet geçmişini dosyaya kaydetme.
/// </summary>
public class IstemciForm : Form
{
    private readonly ChatIstemci _istemci = new();
    private readonly List<ChatMesaji> _gecmis = new();

    private readonly TextBox txtSunucu;
    private readonly NumericUpDown nudPort;
    private readonly TextBox txtKullanici;
    private readonly Button btnBaglan;
    private readonly Button btnKes;
    private readonly Label lblDurum;
    private readonly ListBox lstMesajlar;
    private readonly ListBox lstKullanicilar;
    private readonly CheckBox chkOzel;
    private readonly TextBox txtMesaj;
    private readonly Button btnGonder;
    private readonly Button btnGecmisKaydet;
    private readonly CheckBox chkYenidenBaglan;
    private readonly TextBox txtParola;
    private readonly ComboBox cmbOda;
    private readonly CheckBox chkTls;
    private readonly Button btnOdaGec;

    public IstemciForm(int numara = 1)
    {
        Text = $"Chat İstemcisi #{numara}";
        Font = new Font("Segoe UI", 9.5F);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 540);
        MinimumSize = new Size(640, 420);

        // --- Bağlantı paneli
        var pnlBaglanti = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8, 10, 8, 6) };
        txtSunucu = new TextBox { Name = "txtSunucu", Text = "127.0.0.1", Width = 110 };
        nudPort = new NumericUpDown { Name = "nudPort", Minimum = 1, Maximum = 65535, Value = 5000, Width = 70 };
        txtKullanici = new TextBox { Name = "txtKullanici", Width = 110, MaxLength = ChatSunucu.MaksimumKullaniciAdiUzunlugu, PlaceholderText = "kullanıcı adı" };
        txtParola = new TextBox { Name = "txtParola", Width = 90, UseSystemPasswordChar = true, PlaceholderText = "(yoksa boş)" };
        btnBaglan = new Button { Name = "btnBaglan", Text = "Bağlan", AutoSize = true };
        btnKes = new Button { Name = "btnKes", Text = "Bağlantıyı Kes", AutoSize = true, Enabled = false };
        chkTls = new CheckBox { Name = "chkTls", Text = "TLS", AutoSize = true, Checked = true, Margin = new Padding(6, 6, 0, 0) };
        chkYenidenBaglan = new CheckBox { Name = "chkYenidenBaglan", Text = "Otomatik yeniden bağlan", AutoSize = true, Checked = true, Margin = new Padding(8, 6, 0, 0) };
        lblDurum = new Label
        {
            Name = "lblDurum", AutoSize = true, Padding = new Padding(8, 4, 8, 4), Margin = new Padding(10, 0, 0, 0),
            ForeColor = Color.White, BackColor = Color.DimGray, Font = new Font(Font, FontStyle.Bold), Text = "Bağlı değil"
        };
        pnlBaglanti.Controls.AddRange(new Control[]
        {
            Etiket("Sunucu:"), txtSunucu, Etiket("Port:"), nudPort, Etiket("Ad:"), txtKullanici, Etiket("Parola:"), txtParola, chkTls, btnBaglan, btnKes, chkYenidenBaglan, lblDurum
        });

        // --- Alt panel: mesaj yazma
        var pnlAlt = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 44, ColumnCount = 4, Padding = new Padding(8, 4, 8, 8) };
        pnlAlt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlAlt.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pnlAlt.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pnlAlt.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        txtMesaj = new TextBox { Name = "txtMesaj", Dock = DockStyle.Fill, Enabled = false, MaxLength = 1000, PlaceholderText = "Mesajınızı yazın ve Enter'a basın" };
        chkOzel = new CheckBox { Name = "chkOzel", Text = "Seçili kişiye özel", AutoSize = true, Enabled = false, Margin = new Padding(6, 6, 6, 0) };
        btnGonder = new Button { Name = "btnGonder", Text = "Gönder", AutoSize = true, Enabled = false };
        btnGecmisKaydet = new Button { Name = "btnGecmisKaydet", Text = "Geçmişi Kaydet", AutoSize = true };
        pnlAlt.Controls.Add(txtMesaj, 0, 0);
        pnlAlt.Controls.Add(chkOzel, 1, 0);
        pnlAlt.Controls.Add(btnGonder, 2, 0);
        pnlAlt.Controls.Add(btnGecmisKaydet, 3, 0);

        // --- Orta: mesajlar + kullanıcı listesi
        var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2 };
        lstMesajlar = new ListBox
        {
            Name = "lstMesajlar", Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true,
            DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 20, Font = new Font("Segoe UI", 10F)
        };
        lstMesajlar.DrawItem += LstMesajlar_DrawItem;
        var grpKullanicilar = new GroupBox { Name = "grpKullanicilar", Text = "Oda: Genel", Dock = DockStyle.Fill };
        lstKullanicilar = new ListBox { Name = "lstKullanicilar", Dock = DockStyle.Fill, IntegralHeight = false };
        var pnlOda = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 34, ColumnCount = 2 };
        pnlOda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlOda.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cmbOda = new ComboBox { Name = "cmbOda", Dock = DockStyle.Fill, Text = ChatMesaji.VarsayilanOda, MaxLength = 20 };
        btnOdaGec = new Button { Name = "btnOdaGec", Text = "Geç", AutoSize = true, Enabled = false };
        pnlOda.Controls.Add(cmbOda, 0, 0);
        pnlOda.Controls.Add(btnOdaGec, 1, 0);
        grpKullanicilar.Controls.Add(lstKullanicilar);
        grpKullanicilar.Controls.Add(pnlOda);
        btnOdaGec.Click += BtnOdaGec_Click;
        split.Panel1.Controls.Add(lstMesajlar);
        split.Panel1.Padding = new Padding(8, 0, 0, 0);
        split.Panel2.Controls.Add(grpKullanicilar);
        split.Panel2.Padding = new Padding(0, 0, 8, 0);

        Controls.Add(split);
        Controls.Add(pnlAlt);
        Controls.Add(pnlBaglanti);
        Load += (_, _) => split.SplitterDistance = Math.Max(100, split.Width - 200);
        AcceptButton = btnGonder;

        btnBaglan.Click += BtnBaglan_Click;
        btnKes.Click += (_, _) => { _istemci.BaglantiyiKes(); SistemMesajiEkle("Bağlantı kesildi."); ArayuzuGuncelle(); };
        btnGonder.Click += BtnGonder_Click;
        btnGecmisKaydet.Click += BtnGecmisKaydet_Click;
        FormClosed += (_, _) => _istemci.Dispose();

        _istemci.OtomatikYenidenBaglan = chkYenidenBaglan.Checked;
        chkYenidenBaglan.CheckedChanged += (_, _) => _istemci.OtomatikYenidenBaglan = chkYenidenBaglan.Checked;
        _istemci.YenidenBaglaniyor += (_, d) => UIdaCalistir(() =>
        {
            if (d.Deneme == 1)
                SistemMesajiEkle("Bağlantı koptu. Otomatik olarak yeniden bağlanılacak...");
            ArayuzuGuncelle();
            lblDurum.Text = $"Yeniden bağlanıyor ({d.Deneme}/{_istemci.MaksimumYenidenBaglanmaDenemesi}, {d.Bekleme.TotalSeconds:0} sn)";
            lblDurum.BackColor = Color.DarkOrange;
        });
        _istemci.YenidenBaglandi += (_, _) => UIdaCalistir(() =>
        {
            SistemMesajiEkle("Yeniden bağlanıldı.");
            ArayuzuGuncelle();
        });
        _istemci.MesajAlindi += (_, m) => UIdaCalistir(() => MesajiIsle(m));
        _istemci.BaglantiKoptu += (_, sebep) => UIdaCalistir(() =>
        {
            SistemMesajiEkle(sebep);
            ArayuzuGuncelle();
            MessageBox.Show(this, sebep, "Bağlantı koptu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        });
    }

    private static Label Etiket(string metin) => new() { Text = metin, AutoSize = true, Padding = new Padding(0, 5, 0, 0) };

    private async void BtnBaglan_Click(object? sender, EventArgs e)
    {
        btnBaglan.Enabled = false;
        lblDurum.Text = "Bağlanıyor...";
        lblDurum.BackColor = Color.DarkOrange;
        try
        {
            // await sırasında arayüz donmaz; devamı yine UI iş parçacığında çalışır
            await _istemci.BaglanAsync(txtSunucu.Text, (int)nudPort.Value, txtKullanici.Text, parola: txtParola.Text, oda: cmbOda.Text, tls: chkTls.Checked);
            if (_istemci.Sifreli)
                SistemMesajiEkle(_istemci.YeniSunucuKaydedildi
                    ? $"TLS ile şifreli bağlantı. Sunucu ilk kez görüldü, parmak izi kaydedildi: {_istemci.SunucuParmakIzi![..23]}..."
                    : $"TLS ile şifreli bağlantı (parmak izi doğrulandı: {_istemci.SunucuParmakIzi![..23]}...).");
            else
                SistemMesajiEkle("Uyarı: bağlantı şifresiz (TLS kapalı).");
            txtMesaj.Focus();
        }
        catch (ChatBaglantiHatasi ex)
        {
            MessageBox.Show(this, ex.Message, "Bağlanılamadı", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        ArayuzuGuncelle();
    }

    private async void BtnOdaGec_Click(object? sender, EventArgs e)
    {
        try
        {
            await _istemci.OdaDegistirAsync(cmbOda.Text);
            txtMesaj.Focus();
        }
        catch (ChatBaglantiHatasi ex)
        {
            MessageBox.Show(this, ex.Message, "Odaya geçilemedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnGonder_Click(object? sender, EventArgs e)
    {
        string metin = txtMesaj.Text.Trim();
        if (metin.Length == 0 || !_istemci.Bagli)
            return;

        try
        {
            if (chkOzel.Checked)
            {
                if (lstKullanicilar.SelectedItem is not string alici)
                {
                    MessageBox.Show(this, "Özel mesaj için sağdaki listeden bir kullanıcı seçin.", "Alıcı seçilmedi",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                await _istemci.OzelGonderAsync(alici, metin);
            }
            else
            {
                await _istemci.GonderAsync(metin);
            }
            txtMesaj.Clear();
        }
        catch (ChatBaglantiHatasi ex)
        {
            SistemMesajiEkle(ex.Message);
            ArayuzuGuncelle();
        }
    }

    private void MesajiIsle(ChatMesaji m)
    {
        if (m.Tur == MesajTuru.KullaniciListesi)
        {
            string? secili = lstKullanicilar.SelectedItem as string;
            lstKullanicilar.Items.Clear();
            foreach (var k in m.Kullanicilar ?? new List<string>())
                lstKullanicilar.Items.Add(k);
            if (secili is not null && lstKullanicilar.Items.Contains(secili))
                lstKullanicilar.SelectedItem = secili;

            if (lstKullanicilar.Parent is GroupBox grp)
                grp.Text = $"Oda: {_istemci.Oda} ({lstKullanicilar.Items.Count} kişi)";
            string yazilan = cmbOda.Text;
            cmbOda.Items.Clear();
            foreach (var oda in m.Odalar ?? new List<string>())
                cmbOda.Items.Add(oda);
            cmbOda.Text = yazilan;
            return;
        }
        // Yeniden bağlanınca sunucu geçmişi tekrar gönderir; zaten gösterilmiş mesajları atla
        if (m.Gecmis == true && _gecmis.Any(g => g.Zaman == m.Zaman && g.Gonderen == m.Gonderen && g.Metin == m.Metin))
            return;
        MesajEkle(m);
        ArayuzuGuncelle();
    }

    private void SistemMesajiEkle(string metin) =>
        MesajEkle(new ChatMesaji { Tur = MesajTuru.Sistem, Gonderen = "İstemci", Metin = metin });

    private void MesajEkle(ChatMesaji m)
    {
        _gecmis.Add(m);
        lstMesajlar.Items.Add(m);
        lstMesajlar.TopIndex = Math.Max(0, lstMesajlar.Items.Count - 1);
    }

    /// <summary>Mesaj türüne göre renkli çizim: kendi mesajlarım mavi, özel mor, sistem gri, hata kırmızı.</summary>
    private void LstMesajlar_DrawItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0 || lstMesajlar.Items[e.Index] is not ChatMesaji m)
            return;

        Color renk = m.Gecmis == true ? Color.FromArgb(140, 140, 150) : m.Tur switch
        {
            MesajTuru.Hata => Color.Firebrick,
            MesajTuru.Sistem => Color.Gray,
            MesajTuru.Ozel => Color.DarkMagenta,
            _ when m.Gonderen == _istemci.KullaniciAdi => Color.FromArgb(0, 90, 170),
            _ => Color.Black
        };
        if ((e.State & DrawItemState.Selected) != 0)
            renk = SystemColors.HighlightText;

        var font = m.Tur == MesajTuru.Sistem ? new Font(e.Font!, FontStyle.Italic) : e.Font!;
        TextRenderer.DrawText(e.Graphics, m.GosterimMetni(), font, e.Bounds, renk,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        if (!ReferenceEquals(font, e.Font))
            font.Dispose();
    }

    private void BtnGecmisKaydet_Click(object? sender, EventArgs e)
    {
        if (_gecmis.Count == 0)
        {
            MessageBox.Show(this, "Kaydedilecek mesaj yok.", "Geçmiş boş", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = $"sohbet-gecmisi-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllLines(dialog.FileName, _gecmis.Select(m => m.GosterimMetni()), new UTF8Encoding(true));
            SistemMesajiEkle($"Geçmiş kaydedildi: {dialog.FileName}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Dosya yazılamadı:\n{ex.Message}", "Kaydetme hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ArayuzuGuncelle()
    {
        bool bagli = _istemci.Bagli;
        bool deniyor = _istemci.YenidenBaglaniyorMu && !bagli;
        btnBaglan.Enabled = !bagli && !deniyor;
        txtSunucu.Enabled = nudPort.Enabled = txtKullanici.Enabled = txtParola.Enabled = chkTls.Enabled = !bagli && !deniyor;
        btnKes.Enabled = bagli || deniyor;
        txtMesaj.Enabled = btnGonder.Enabled = chkOzel.Enabled = btnOdaGec.Enabled = bagli;
        lblDurum.Text = bagli ? $"{(_istemci.Sifreli ? "🔒 " : "")}Bağlı: {_istemci.KullaniciAdi}" : "Bağlı değil";
        lblDurum.BackColor = bagli ? Color.FromArgb(30, 140, 60) : Color.DimGray;
        if (!bagli)
            lstKullanicilar.Items.Clear();
    }

    private void UIdaCalistir(Action islem)
    {
        if (IsDisposed || !IsHandleCreated)
            return;
        BeginInvoke(islem);
    }
}
