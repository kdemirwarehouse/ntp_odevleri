using System.Text;
using SpeechApp.Core;

namespace SpeechApp;

/// <summary>
/// Ana pencere: üst bölüm Text-to-Speech, alt bölüm Speech-to-Text.
/// Ses işlemleri <see cref="KonusmaServisi"/> sınıfındadır; form yalnızca kullanıcı etkileşimini yönetir.
/// </summary>
public partial class MainForm : Form
{
    private KonusmaServisi? _servis;
    private readonly TanimaGecmisi _gecmis = new(minimumGuven: 0.2f);

    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        try
        {
            _servis = new KonusmaServisi();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Konuşma servisi başlatılamadı. Ses çıkış aygıtını kontrol edin.\n\n" + ex.Message,
                "Ses hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            grpTts.Enabled = grpStt.Enabled = false;
            return;
        }

        // Servis olayları arka plan iş parçacığından gelebilir → BeginInvoke
        _servis.DurumDegisti += (_, d) => UIdaCalistir(() => DurumGoster(d));
        _servis.CumleTanindi += (_, s) => UIdaCalistir(() => CumleEkle(s.Metin, s.Guven));
        _servis.TahminGuncellendi += (_, t) => UIdaCalistir(() => lblTahmin.Text = "Duyulan: " + t);
        _servis.SesSeviyesi += (_, s) => UIdaCalistir(() => pbSeviye.Value = Math.Clamp(s, 0, 100));
        _servis.OkunanKelime += (_, p) => UIdaCalistir(() => KelimeyiVurgula(p.Baslangic, p.Uzunluk));
        _servis.Hata += (_, m) => UIdaCalistir(() => MessageBox.Show(this, m, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning));

        // Ses listesi
        var sesler = _servis.Sesler();
        cmbSes.Items.AddRange(sesler.ToArray());
        if (sesler.Count > 0)
            cmbSes.SelectedIndex = 0;
        else
        {
            cmbSes.Items.Add("(kurulu ses yok)");
            cmbSes.SelectedIndex = 0;
            btnOku.Enabled = false;
        }
        trkHiz.Value = _servis.Hiz;
        trkDuzey.Value = _servis.Duzey;

        // Tanıyıcı bilgisi
        var taniyicilar = KonusmaServisi.KuruluTaniyicilar();
        string? dil = DilSecici.EnUygun(taniyicilar, System.Globalization.CultureInfo.CurrentUICulture.Name);
        lblDil.Text = dil is null
            ? "Tanıma dili: kurulu tanıyıcı yok!"
            : $"Tanıma dili: {DilSecici.GorunenAd(dil)}";
        if (dil is null)
            lblDil.ForeColor = Color.Firebrick;

        splitStt.SplitterDistance = splitStt.Width * 55 / 100;
        DurumGoster(SesDurumu.Hazir);
    }

    // ------------------------------------------------------------ Text-to-Speech

    private void BtnOku_Click(object? sender, EventArgs e)
    {
        try
        {
            VurguyuTemizle();
            _servis!.Oku(rtbMetin.Text);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Okunamadı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void BtnDuraklat_Click(object? sender, EventArgs e) => _servis?.DuraklatDevam();
    private void BtnDurdur_Click(object? sender, EventArgs e) => _servis?.OkumayiDurdur();

    private void BtnTtsTemizle_Click(object? sender, EventArgs e)
    {
        _servis?.OkumayiDurdur();
        rtbMetin.Clear();
        rtbMetin.Focus();
    }

    private void BtnWavKaydet_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog { Filter = "WAV ses dosyası (*.wav)|*.wav", FileName = "okunan-metin.wav" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            Cursor = Cursors.WaitCursor;
            _servis!.WavOlarakKaydet(rtbMetin.Text, dialog.FileName);
            lblDurumAlt.Text = "Ses dosyası kaydedildi: " + dialog.FileName;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Kaydedilemedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { Cursor = Cursors.Default; }
    }

    private void CmbSes_SelectedIndexChanged(object? sender, EventArgs e) => _servis?.SesSec(cmbSes.SelectedIndex);
    private void TrkHiz_Scroll(object? sender, EventArgs e) { if (_servis is not null) _servis.Hiz = trkHiz.Value; lblHiz.Text = $"Hız: {trkHiz.Value}"; }
    private void TrkDuzey_Scroll(object? sender, EventArgs e) { if (_servis is not null) _servis.Duzey = trkDuzey.Value; lblDuzey.Text = $"Ses: {trkDuzey.Value}"; }

    private void VurguyuTemizle()
    {
        int imlec = rtbMetin.SelectionStart;
        rtbMetin.SelectAll();
        rtbMetin.SelectionBackColor = rtbMetin.BackColor;
        rtbMetin.Select(imlec, 0);
    }

    /// <summary>Okunan kelimeyi sarı arka planla işaretler.</summary>
    private void KelimeyiVurgula(int baslangic, int uzunluk)
    {
        if (baslangic + uzunluk > rtbMetin.TextLength)
            return;
        rtbMetin.SelectAll();
        rtbMetin.SelectionBackColor = rtbMetin.BackColor;
        rtbMetin.Select(baslangic, uzunluk);
        rtbMetin.SelectionBackColor = Color.Gold;
        rtbMetin.Select(baslangic, 0);
    }

    // ------------------------------------------------------------ Speech-to-Text

    private void BtnDinle_Click(object? sender, EventArgs e)
    {
        try
        {
            _servis!.DinlemeyiBaslat();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Mikrofon / tanıma servisi kullanılamıyor",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDinlemeDurdur_Click(object? sender, EventArgs e) => _servis?.DinlemeyiDurdur();

    private void BtnDosyadanTani_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "WAV ses dosyası (*.wav)|*.wav", Title = "Metne çevrilecek ses dosyası" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        DosyadanTani(dialog.FileName);
    }

    /// <summary>Komut satırından da çağrılabilir: SpeechApp.exe --wav dosya.wav</summary>
    public void DosyadanTani(string yol)
    {
        try
        {
            _servis!.DosyadanTani(yol);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Dosya tanınamadı", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CumleEkle(string metin, float guven)
    {
        if (!_gecmis.Ekle(metin, guven))
        {
            lblTahmin.Text = $"Düşük güvenli sonuç atlandı: \"{metin}\" ({guven:P0})";
            return;
        }
        var kayit = _gecmis.Kayitlar[^1];

        if (txtTaninan.TextLength > 0 && !txtTaninan.Text.EndsWith(' '))
            txtTaninan.AppendText(" ");
        txtTaninan.AppendText(kayit.Metin);

        var oge = new ListViewItem(kayit.Zaman.ToString("HH:mm:ss"));
        oge.SubItems.Add($"%{kayit.GuvenYuzde}");
        oge.SubItems.Add(kayit.Metin);
        lvSonuclar.Items.Add(oge);
        lvSonuclar.EnsureVisible(lvSonuclar.Items.Count - 1);
        lblTahmin.Text = $"Ortalama güven: %{_gecmis.OrtalamaGuven * 100:0}";
    }

    private void BtnSttTemizle_Click(object? sender, EventArgs e)
    {
        _gecmis.Temizle();
        txtTaninan.Clear();
        lvSonuclar.Items.Clear();
        lblTahmin.Text = "";
    }

    private void BtnTxtKaydet_Click(object? sender, EventArgs e)
    {
        if (_gecmis.Kayitlar.Count == 0)
        {
            MessageBox.Show(this, "Kaydedilecek tanıma sonucu yok.", "Boş", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new SaveFileDialog { Filter = "Metin dosyası (*.txt)|*.txt", FileName = $"konusma-{DateTime.Now:yyyyMMdd-HHmmss}.txt" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            _gecmis.TxtOlarakKaydet(dialog.FileName);
            lblDurumAlt.Text = "Kaydedildi: " + dialog.FileName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Kaydedilemedi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ------------------------------------------------------------ Durum göstergesi

    private void DurumGoster(SesDurumu d)
    {
        lblAktif.Text = d.Aciklama();
        lblAktif.BackColor = d switch
        {
            SesDurumu.Okunuyor => Color.FromArgb(0, 110, 200),
            SesDurumu.Duraklatildi => Color.DarkOrange,
            SesDurumu.Dinleniyor => Color.FromArgb(200, 35, 35),
            SesDurumu.DosyaTaniniyor => Color.FromArgb(130, 60, 170),
            _ => Color.DimGray
        };

        bool okuma = d is SesDurumu.Okunuyor or SesDurumu.Duraklatildi;
        if (!okuma)
            VurguyuTemizle();
        bool tanima = d is SesDurumu.Dinleniyor or SesDurumu.DosyaTaniniyor;
        btnDuraklat.Enabled = btnDurdur.Enabled = okuma;
        btnDuraklat.Text = d == SesDurumu.Duraklatildi ? "▶ Devam" : "⏸ Duraklat";
        btnDinle.Enabled = btnDosyadanTani.Enabled = !tanima;
        btnDinlemeDurdur.Enabled = tanima;
        if (_servis?.TaniyiciDili is string dil && tanima)
            lblDil.Text = $"Tanıma dili: {DilSecici.GorunenAd(dil)}";
        lblDurumAlt.Text = d.Aciklama();
    }

    private void UIdaCalistir(Action islem)
    {
        if (IsDisposed || !IsHandleCreated)
            return;
        BeginInvoke(islem);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e) => _servis?.Dispose();
}
