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

    public IstemciForm(int numara = 1)
    {
        Text = $"Chat İstemcisi #{numara}";
        Font = new Font("Segoe UI", 9.5F);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 520);
        MinimumSize = new Size(640, 420);

        // --- Bağlantı paneli
        var pnlBaglanti = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(8, 10, 8, 0) };
        txtSunucu = new TextBox { Name = "txtSunucu", Text = "127.0.0.1", Width = 110 };
        nudPort = new NumericUpDown { Name = "nudPort", Minimum = 1, Maximum = 65535, Value = 5000, Width = 70 };
        txtKullanici = new TextBox { Name = "txtKullanici", Width = 110, MaxLength = ChatSunucu.MaksimumKullaniciAdiUzunlugu, PlaceholderText = "kullanıcı adı" };
        btnBaglan = new Button { Name = "btnBaglan", Text = "Bağlan", AutoSize = true };
        btnKes = new Button { Name = "btnKes", Text = "Bağlantıyı Kes", AutoSize = true, Enabled = false };
        lblDurum = new Label
        {
            Name = "lblDurum", AutoSize = true, Padding = new Padding(8, 4, 8, 4), Margin = new Padding(10, 0, 0, 0),
            ForeColor = Color.White, BackColor = Color.DimGray, Font = new Font(Font, FontStyle.Bold), Text = "Bağlı değil"
        };
        pnlBaglanti.Controls.AddRange(new Control[]
        {
            Etiket("Sunucu:"), txtSunucu, Etiket("Port:"), nudPort, Etiket("Ad:"), txtKullanici, btnBaglan, btnKes, lblDurum
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
        var grpKullanicilar = new GroupBox { Text = "Çevrimiçi", Dock = DockStyle.Fill };
        lstKullanicilar = new ListBox { Name = "lstKullanicilar", Dock = DockStyle.Fill, IntegralHeight = false };
        grpKullanicilar.Controls.Add(lstKullanicilar);
        split.Panel1.Controls.Add(lstMesajlar);
        split.Panel1.Padding = new Padding(8, 0, 0, 0);
        split.Panel2.Controls.Add(grpKullanicilar);
        split.Panel2.Padding = new Padding(0, 0, 8, 0);

        Controls.Add(split);
        Controls.Add(pnlAlt);
        Controls.Add(pnlBaglanti);
        Load += (_, _) => split.SplitterDistance = Math.Max(100, split.Width - 180);
        AcceptButton = btnGonder;

        btnBaglan.Click += BtnBaglan_Click;
        btnKes.Click += (_, _) => { _istemci.BaglantiyiKes(); SistemMesajiEkle("Bağlantı kesildi."); ArayuzuGuncelle(); };
        btnGonder.Click += BtnGonder_Click;
        btnGecmisKaydet.Click += BtnGecmisKaydet_Click;
        FormClosed += (_, _) => _istemci.Dispose();

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
            await _istemci.BaglanAsync(txtSunucu.Text, (int)nudPort.Value, txtKullanici.Text);
            txtMesaj.Focus();
        }
        catch (ChatBaglantiHatasi ex)
        {
            MessageBox.Show(this, ex.Message, "Bağlanılamadı", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        ArayuzuGuncelle();
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
            return;
        }
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

        Color renk = m.Tur switch
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
        btnBaglan.Enabled = !bagli;
        txtSunucu.Enabled = nudPort.Enabled = txtKullanici.Enabled = !bagli;
        btnKes.Enabled = bagli;
        txtMesaj.Enabled = btnGonder.Enabled = chkOzel.Enabled = bagli;
        lblDurum.Text = bagli ? $"Bağlı: {_istemci.KullaniciAdi}" : "Bağlı değil";
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
