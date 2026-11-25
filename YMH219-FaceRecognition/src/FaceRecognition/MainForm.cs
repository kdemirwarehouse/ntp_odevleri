using System.Diagnostics;
using FaceRecognition.Core;
using FaceRecognition.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Rect = OpenCvSharp.Rect;

namespace FaceRecognition;

/// <summary>
/// Ana pencere. Görüntü kaynağını (kamera / dosya) yönetir, sonuçları PictureBox'ta gösterir.
/// Algılama/tanıma <see cref="GoruntuServisi"/>, kamera okuma <see cref="KameraServisi"/>,
/// örnek dosyaları <see cref="OrnekDeposu"/> sınıfındadır.
/// </summary>
public partial class MainForm : Form
{
    private readonly OrnekDeposu _depo;
    private readonly KameraServisi _kamera = new();
    private GoruntuServisi? _goruntu;

    private readonly object _kareKilidi = new();
    private Mat? _sonKare;               // örnek eklemek için son ham kare
    private List<AlgilananYuz> _sonYuzler = new();
    private bool _kareIsleniyor;         // arayüz yetişemezse kare atlanır

    public MainForm(string ornekKlasoru)
    {
        InitializeComponent();
        _depo = new OrnekDeposu(ornekKlasoru);
        _kamera.KareGeldi += Kamera_KareGeldi;
        _kamera.Hata += (_, m) => BeginInvoke(() =>
        {
            KameraDurumunuGuncelle();
            MessageBox.Show(this, m, "Kamera hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        });
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        string cascade = Path.Combine(AppContext.BaseDirectory, "Models", "haarcascade_frontalface_default.xml");
        try
        {
            _goruntu = new GoruntuServisi(cascade) { Esik = (double)nudEsik.Value };
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException or OpenCVException or TypeInitializationException or DllNotFoundException)
        {
            MessageBox.Show(this, ex.Message, "Model / OpenCV yüklenemedi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            grpKaynak.Enabled = grpEgitim.Enabled = grpSonuc.Enabled = false;
            lblDurum.Text = "Yüz algılama modeli yüklenemedi.";
            return;
        }

        EtiketListesiniYenile();
        if (_depo.EgitimHatasi() is null)
            ModeliEgit(sessiz: true); // önceki oturumdan kalan örneklerle otomatik eğit
    }

    // ------------------------------------------------------------ Görüntü kaynağı

    private void BtnKameraBaslat_Click(object? sender, EventArgs e)
    {
        try
        {
            _kamera.Baslat();
            lblDurum.Text = "Kamera açık. Yüzler gerçek zamanlı işaretleniyor.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or OpenCVException)
        {
            MessageBox.Show(this, ex.Message, "Kamera açılamadı", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        KameraDurumunuGuncelle();
    }

    private void BtnKameraDurdur_Click(object? sender, EventArgs e)
    {
        _kamera.Durdur();
        KameraDurumunuGuncelle();
        lblDurum.Text = "Kamera durduruldu.";
    }

    private void KameraDurumunuGuncelle()
    {
        btnKameraBaslat.Enabled = !_kamera.Calisiyor;
        btnKameraDurdur.Enabled = _kamera.Calisiyor;
    }

    /// <summary>Arka plan iş parçacığında çalışır: kareyi işler, çizer, sonucu UI'a gönderir.</summary>
    private void Kamera_KareGeldi(object? sender, Mat kare)
    {
        if (_kareIsleniyor || _goruntu is null || IsDisposed)
            return;
        _kareIsleniyor = true;
        try
        {
            using var ayna = new Mat();
            Cv2.Flip(kare, ayna, FlipMode.Y); // kamera görüntüsü ayna gibi gösterilir
            KareyiIsleVeGoster(ayna);
        }
        catch (Exception ex) when (ex is OpenCVException or ObjectDisposedException or InvalidOperationException)
        {
            _kareIsleniyor = false;
        }
    }

    private void BtnGoruntuSec_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Yüz içeren bir görüntü seçin",
            Filter = "Görüntü dosyaları|*.jpg;*.jpeg;*.png;*.bmp|Tüm dosyalar|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            GoruntuDosyasiAc(dialog.FileName);
    }

    /// <summary>Komut satırından da kullanılır: FaceRecognition.exe --resim yol.jpg</summary>
    public void GoruntuDosyasiAc(string yol)
    {
        if (_goruntu is null)
            return;
        if (_kamera.Calisiyor)
        {
            _kamera.Durdur();
            KameraDurumunuGuncelle();
        }

        using var resim = Cv2.ImRead(yol, ImreadModes.Color);
        if (resim.Empty())
        {
            MessageBox.Show(this, $"Görüntü açılamadı veya desteklenmeyen biçim:\n{yol}", "Görüntü hatası",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _kareIsleniyor = true;
        KareyiIsleVeGoster(resim);
        lblDurum.Text = $"Görüntü: {Path.GetFileName(yol)} ({resim.Width}x{resim.Height})";
    }

    /// <summary>Algılama + tanıma + çizim. Sonuç Bitmap'i UI iş parçacığına aktarılır.</summary>
    private void KareyiIsleVeGoster(Mat kare)
    {
        var yuzler = _goruntu!.Isle(kare);
        using var cizim = kare.Clone();
        GoruntuServisi.Ciz(cizim, yuzler);
        var bitmap = cizim.ToBitmap();

        lock (_kareKilidi)
        {
            _sonKare?.Dispose();
            _sonKare = kare.Clone();
            _sonYuzler = yuzler;
        }

        void Goster()
        {
            var eski = pbGoruntu.Image;
            pbGoruntu.Image = bitmap;
            eski?.Dispose();
            SonucuYaz(yuzler);
            _kareIsleniyor = false;
        }

        if (InvokeRequired) BeginInvoke(Goster); else Goster();
    }

    private void SonucuYaz(List<AlgilananYuz> yuzler)
    {
        lblYuzSayisi.Text = $"Algılanan yüz: {yuzler.Count}";
        if (yuzler.Count == 0)
        {
            lblSonuc.Text = "Yüz bulunamadı";
            lblSonuc.ForeColor = SystemColors.GrayText;
            return;
        }

        // En büyük yüzün sonucu öne çıkarılır
        var ana = yuzler.OrderByDescending(y => y.Bolge.Width * y.Bolge.Height).First();
        if (ana.Karar is not TanimaKarari karar)
        {
            lblSonuc.Text = "Yüz algılandı\n(model eğitilmedi)";
            lblSonuc.ForeColor = Color.DarkOrange;
        }
        else
        {
            lblSonuc.Text = karar.Taninan
                ? $"{karar.Etiket}\nGüven: %{karar.GuvenYuzde}  (mesafe {karar.Mesafe:0.0})"
                : $"{TanimaKarari.Bilinmiyor}\nEn yakın: {karar.EnYakin ?? "-"} (mesafe {karar.Mesafe:0.0} > eşik {nudEsik.Value})";
            lblSonuc.ForeColor = karar.Taninan ? Color.ForestGreen : Color.Firebrick;
        }
    }

    // ------------------------------------------------------------ Eğitim

    private void BtnOrnekEkle_Click(object? sender, EventArgs e)
    {
        string? hata = EtiketKurallari.Hata(txtEtiket.Text);
        if (hata is not null)
        {
            MessageBox.Show(this, hata, "Geçersiz etiket", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtEtiket.Focus();
            return;
        }

        Mat? kare;
        List<AlgilananYuz> yuzler;
        lock (_kareKilidi)
        {
            kare = _sonKare?.Clone();
            yuzler = _sonYuzler;
        }

        using (kare)
        {
            if (kare is null || yuzler.Count == 0)
            {
                MessageBox.Show(this, "Görüntüde yüz yok. Önce kamerayı başlatın veya yüz içeren bir görüntü seçin.",
                    "Yüz bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var enBuyuk = yuzler.OrderByDescending(y => y.Bolge.Width * y.Bolge.Height).First();
            try
            {
                using var hazir = GoruntuServisi.YuzuHazirla(kare, enBuyuk.Bolge);
                int adet = GoruntuServisi.OrnekKaydet(_depo, txtEtiket.Text, hazir, chkVeriArtirma.Checked);
                lblDurum.Text = $"'{txtEtiket.Text.Trim()}' etiketine {adet} örnek eklendi. Tanıma için 'Modeli Eğit'e basın.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OpenCVException)
            {
                MessageBox.Show(this, "Örnek kaydedilemedi:\n" + ex.Message, "Dosya hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        EtiketListesiniYenile();
    }

    private void BtnEgit_Click(object? sender, EventArgs e) => ModeliEgit(sessiz: false);

    private void ModeliEgit(bool sessiz)
    {
        if (_goruntu is null)
            return;
        try
        {
            Cursor = Cursors.WaitCursor;
            int adet = _goruntu.Egit(_depo);
            lblModel.Text = $"Model: {_goruntu.ModeldekiEtiketler.Count} etiket, {adet} örnek";
            lblDurum.Text = "Model eğitildi. Kamera veya görüntü ile tanıma yapılabilir.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or OpenCVException)
        {
            if (!sessiz)
                MessageBox.Show(this, ex.Message, "Model eğitilemedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally { Cursor = Cursors.Default; }
    }

    private void BtnEtiketSil_Click(object? sender, EventArgs e)
    {
        if (lvEtiketler.SelectedItems.Count == 0)
        {
            MessageBox.Show(this, "Silmek için listeden bir etiket seçin.", "Etiket seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        string etiket = lvEtiketler.SelectedItems[0].Text;
        if (MessageBox.Show(this, $"'{etiket}' etiketi ve tüm örnekleri silinsin mi?", "Etiketi sil",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            _depo.EtiketSil(etiket);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Silinemedi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        EtiketListesiniYenile();

        if (_depo.EgitimHatasi() is null)
            ModeliEgit(sessiz: true);
        else
        {
            _goruntu?.ModeliSifirla();
            lblModel.Text = "Model: eğitilmedi";
        }
    }

    private void BtnKlasorAc_Click(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(_depo.KokKlasor);
        Process.Start(new ProcessStartInfo("explorer.exe", _depo.KokKlasor) { UseShellExecute = true });
    }

    /// <summary>İsteğe bağlı: algılanan tüm yüzleri ayrı klasöre (algilanan/) kaydeder.</summary>
    private void BtnYuzleriKaydet_Click(object? sender, EventArgs e)
    {
        Mat? kare;
        List<AlgilananYuz> yuzler;
        lock (_kareKilidi) { kare = _sonKare?.Clone(); yuzler = _sonYuzler; }
        using (kare)
        {
            if (kare is null || yuzler.Count == 0)
            {
                lblDurum.Text = "Kaydedilecek yüz yok.";
                return;
            }
            string klasor = Path.Combine(Path.GetDirectoryName(_depo.KokKlasor)!, "algilanan");
            Directory.CreateDirectory(klasor);
            int i = 0;
            foreach (var y in yuzler)
            {
                using var kesit = new Mat(kare, y.Bolge.Intersect(new Rect(0, 0, kare.Width, kare.Height)));
                Cv2.ImWrite(Path.Combine(klasor, $"yuz-{DateTime.Now:yyyyMMdd-HHmmss}-{++i}.png"), kesit);
            }
            lblDurum.Text = $"{i} yüz kaydedildi: {klasor}";
        }
    }

    private void EtiketListesiniYenile()
    {
        lvEtiketler.BeginUpdate();
        lvEtiketler.Items.Clear();
        foreach (var et in _depo.Etiketler())
        {
            var oge = new ListViewItem(et.Etiket);
            oge.SubItems.Add(et.OrnekSayisi.ToString());
            lvEtiketler.Items.Add(oge);
        }
        lvEtiketler.EndUpdate();
    }

    private void NudEsik_ValueChanged(object? sender, EventArgs e)
    {
        if (_goruntu is not null)
            _goruntu.Esik = (double)nudEsik.Value;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _kamera.Dispose();
        _goruntu?.Dispose();
        lock (_kareKilidi) _sonKare?.Dispose();
    }
}
