using KeyEventLogger.Core;

namespace KeyEventLogger;

/// <summary>
/// Ana pencere. Form <c>KeyPreview = true</c> olduğu için, odaktaki kontrol hangisi olursa olsun
/// klavye olayları önce bu formun KeyDown / KeyPress / KeyUp olaylarına gelir.
/// Olaylar yalnızca bu pencere aktifken oluşur; sistem genelinde dinleme yapılmaz.
/// </summary>
public partial class MainForm : Form
{
    private readonly KayitOturumu _oturum = new();
    private readonly TekrarAlgilayici _tekrar = new();
    private string? _sonKeyDownTusu;   // KeyPress'i hangi KeyDown'ın ürettiğini göstermek için

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    private const uint MAPVK_VK_TO_CHAR = 2;

    /// <summary>
    /// Tuş tek başına bir karakter üretir mi? Üretmiyorsa (Shift, Ctrl, F1, oklar ...) Windows bu tuş için
    /// WM_CHAR göndermez, dolayısıyla KeyPress olayı da oluşmaz. Ctrl ile birlikte basılan harfler ise
    /// kontrol karakteri (Ctrl+A → U+0001) ürettiği için KeyPress oluşturur.
    /// </summary>
    private static bool KarakterUretirMi(Keys tus, Keys degistirici)
    {
        if (degistirici.HasFlag(Keys.Alt)) return false;
        if (tus is Keys.Enter or Keys.Back or Keys.Tab or Keys.Escape or Keys.Space) return true;
        return MapVirtualKey((uint)tus, MAPVK_VK_TO_CHAR) != 0;
    }
    private bool _filtreAktif;

    public MainForm()
    {
        InitializeComponent();
        _oturum.DurumDegisti += (_, _) => DurumGostergesiniGuncelle();
        _oturum.KayitEklendi += Oturum_KayitEklendi;

        GezinmeTuslariniYakala(this);
        lvKayitlar.Resize += (_, _) => AyrintiSutunuGenislet();
        AyrintiSutunuGenislet();

        dtpBaslangic.Value = DateTime.Now.AddMinutes(-10);
        dtpBitis.Value = DateTime.Now.AddMinutes(10);
        DurumGostergesiniGuncelle();
        IstatistikleriGuncelle();
    }

    // ------------------------------------------------------------ Klavye olayları

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // Tekrar takibi, olay türü kapalı olsa bile yapılır ki sonraki KeyPress doğru etiketlensin
        bool tekrar = _tekrar.TusBasildi(e.KeyCode.ToString());

        if (!chkKeyDown.Checked || ParolaAlanindaMi() || (tekrar && chkTekrarYoksay.Checked))
            return;

        string ayrinti = $"KeyCode={(int)e.KeyCode}";
        if (e.Modifiers != Keys.None)
            ayrinti = $"Değiştirici: {DegistiriciMetni(e.Modifiers)}  {ayrinti}";
        if (!KarakterUretirMi(e.KeyCode, e.Modifiers))
            ayrinti += "  · karakter üretmez → KeyPress olmaz";

        _sonKeyDownTusu = e.KeyCode.ToString();
        _oturum.Ekle(OlayTuru.KeyDown, e.KeyCode.ToString(), ayrinti, tekrar);
    }

    private void MainForm_KeyUp(object? sender, KeyEventArgs e)
    {
        _tekrar.TusBirakildi(e.KeyCode.ToString());

        if (!chkKeyUp.Checked || ParolaAlanindaMi())
            return;
        _oturum.Ekle(OlayTuru.KeyUp, e.KeyCode.ToString(), $"KeyCode={(int)e.KeyCode}");
    }

    private void MainForm_KeyPress(object? sender, KeyPressEventArgs e)
    {
        bool tekrar = _tekrar.SonBasimTekrar;
        if (!chkKeyPress.Checked || ParolaAlanindaMi() || (tekrar && chkTekrarYoksay.Checked))
            return;
        string kaynak = _sonKeyDownTusu is null ? "" : $"  ← KeyDown: {_sonKeyDownTusu}";
        _oturum.Ekle(OlayTuru.KeyPress, KeyLogEntry.KarakterAdi(e.KeyChar), $"Karakter kodu=U+{(int)e.KeyChar:X4}{kaynak}", tekrar);
    }

    /// <summary>
    /// Ok tuşları ve Tab normalde "iletişim kutusu tuşu" olarak Windows tarafından işlenir:
    /// odak bir butondayken odağı başka kontrole taşırlar ve formun KeyDown olayına ulaşmazlar.
    /// Kayıt açıkken her kontrolün PreviewKeyDown olayında bu tuşları "girdi tuşu" (IsInputKey)
    /// olarak işaretleriz; böylece KeyDown / KeyUp her durumda oluşur ve listeye düşer.
    /// Kayıt kapalıyken normal klavye ile gezinme davranışı korunur.
    /// </summary>
    private void GezinmeTuslariniYakala(Control ust)
    {
        foreach (Control kontrol in ust.Controls)
        {
            kontrol.PreviewKeyDown += Kontrol_PreviewKeyDown;
            GezinmeTuslariniYakala(kontrol);
        }
    }

    private void Kontrol_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        if (_oturum.KayitAcik && GezinmeTusuMu(e.KeyCode))
            e.IsInputKey = true;
    }

    internal static bool GezinmeTusuMu(Keys tus) =>
        tus is Keys.Tab or Keys.Up or Keys.Down or Keys.Left or Keys.Right;

    /// <summary>
    /// Güvenlik: Odaktaki kontrol bir parola alanıysa olay kaydedilmez.
    /// (Bu formda parola alanı yok; ileride eklenirse yanlışlıkla kayıt yapılmasın diye kontrol edilir.)
    /// </summary>
    private bool ParolaAlanindaMi() =>
        ActiveControl is TextBox t && (t.UseSystemPasswordChar || t.PasswordChar != '\0')
        || ActiveControl is MaskedTextBox m && (m.UseSystemPasswordChar || m.PasswordChar != '\0');

    private static string DegistiriciMetni(Keys degistirici)
    {
        var parcalar = new List<string>();
        if (degistirici.HasFlag(Keys.Control)) parcalar.Add("Ctrl");
        if (degistirici.HasFlag(Keys.Shift)) parcalar.Add("Shift");
        if (degistirici.HasFlag(Keys.Alt)) parcalar.Add("Alt");
        return string.Join("+", parcalar);
    }

    /// <summary>Ayrıntı sütunu, listenin kalan genişliğini doldursun (uzun açıklamalar kesilmesin).</summary>
    private void AyrintiSutunuGenislet()
    {
        int digerleri = colZaman.Width + colOlay.Width + colTus.Width;
        colAyrinti.Width = Math.Max(230, lvKayitlar.ClientSize.Width - digerleri - 4);
    }

    // ------------------------------------------------------------ Oturum olayları

    private void Oturum_KayitEklendi(object? sender, KeyLogEntry kayit)
    {
        if (!_filtreAktif)
        {
            lvKayitlar.Items.Add(ListeOgesi(kayit));
            lvKayitlar.EnsureVisible(lvKayitlar.Items.Count - 1);
        }
        IstatistikleriGuncelle();

        if (_oturum.SinirDoldu)
            DurumYaz($"Kayıt sınırına ({_oturum.Sinir} olay) ulaşıldı, kayıt durduruldu. Kaydedip temizleyebilirsiniz.");
    }

    private static ListViewItem ListeOgesi(KeyLogEntry k)
    {
        var oge = new ListViewItem(k.Zaman.ToString("dd.MM.yyyy HH:mm:ss.fff"));
        oge.SubItems.Add(k.Tur.ToString());
        oge.SubItems.Add(k.Tus);
        oge.SubItems.Add(k.GorunenAyrinti);
        oge.ForeColor = k.Tekrar ? Color.FromArgb(170, 120, 40) : k.Tur switch
        {
            OlayTuru.KeyDown => Color.FromArgb(0, 90, 170),
            OlayTuru.KeyUp => Color.FromArgb(110, 110, 110),
            _ => Color.FromArgb(0, 128, 60)
        };
        return oge;
    }

    private void DurumGostergesiniGuncelle()
    {
        bool acik = _oturum.KayitAcik;
        lblGosterge.Text = acik ? "●  KAYIT AÇIK" : "■  KAYIT KAPALI";
        lblGosterge.BackColor = acik ? Color.FromArgb(200, 35, 35) : Color.DimGray;
        btnBaslat.Enabled = !acik;
        btnDurdur.Enabled = acik;
        Text = acik ? "● Kayıt açık — Key Event Logger (Eğitsel)" : "Key Event Logger (Eğitsel)";
    }

    private void IstatistikleriGuncelle()
    {
        lblToplamTus.Text = $"Tuş basımı: {_oturum.ToplamTusBasimi}  (tekrar: {_oturum.TekrarSayisi})";
        lblOlaySayisi.Text = $"Olay: {_oturum.Kayitlar.Count} / {_oturum.Sinir}  (kaydedilmemiş: {_oturum.KaydedilmemisOlaySayisi})";

        lstEnSik.BeginUpdate();
        lstEnSik.Items.Clear();
        int sira = 1;
        foreach (var (tus, adet) in _oturum.EnSikTuslar(5))
            lstEnSik.Items.Add($"{sira++}. {tus}  —  {adet} kez");
        lstEnSik.EndUpdate();
    }

    // ------------------------------------------------------------ Butonlar

    private void BtnBaslat_Click(object? sender, EventArgs e)
    {
        try
        {
            _oturum.Baslat();
            DurumYaz($"Kayıt başladı ({DateTime.Now:HH:mm:ss}). Deneme alanına yazabilirsiniz.");
            txtDeneme.Focus(); // Boşluk/Enter tuşları butonu tekrar tetiklemesin
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Kayıt başlatılamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnDurdur_Click(object? sender, EventArgs e)
    {
        _oturum.Durdur();
        DurumYaz($"Kayıt durduruldu. {_oturum.Kayitlar.Count} olay bellekte.");
    }

    private void BtnTemizle_Click(object? sender, EventArgs e)
    {
        if (_oturum.Kayitlar.Count == 0)
        {
            DurumYaz("Temizlenecek kayıt yok.");
            return;
        }
        _oturum.Temizle();
        lvKayitlar.Items.Clear();
        FiltreyiKaldir();
        IstatistikleriGuncelle();
        DurumYaz("Ekrandaki kayıtlar temizlendi.");
    }

    private void BtnKaydet_Click(object? sender, EventArgs e)
    {
        if (_oturum.Kayitlar.Count == 0)
        {
            MessageBox.Show(this, "Kaydedilecek olay yok. Önce Başlat ile kayıt alın.", "Kayıt yok",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        KayitlariKaydet();
    }

    /// <summary>
    /// Kaydetme penceresini açar ve kayıtları yazar. Kayıt başarılıysa true döner
    /// (kapatma onayında "Evet" seçildiğinde de kullanılır).
    /// </summary>
    private bool KayitlariKaydet()
    {
        // Dosya penceresi açılırken yazılan tuşlar karışmasın diye kayıt durdurulur.
        _oturum.Durdur();

        var kaydedilecek = _filtreAktif
            ? _oturum.Filtrele(dtpBaslangic.Value, dtpBitis.Value)
            : _oturum.Kayitlar.ToList();

        using var dialog = new SaveFileDialog
        {
            Title = "Klavye olaylarını kaydet",
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = KayitDosyasi.VarsayilanDosyaAdi(),
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            DurumYaz("Kaydetme iptal edildi. Kayıt durdurulmuş durumda.");
            return false;
        }

        try
        {
            KayitDosyasi.Kaydet(kaydedilecek, dialog.FileName);
            if (!_filtreAktif)
                _oturum.KaydedildiOlarakIsaretle();   // Filtreli kayıtta bazı olaylar hâlâ yazılmamış olabilir
            DurumYaz($"{kaydedilecek.Count} olay kaydedildi: {dialog.FileName}");
            IstatistikleriGuncelle();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Dosya yazılamadı:\n{ex.Message}\n\nKayıtlar bellekte korunuyor.",
                "Kaydetme hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // ------------------------------------------------------------ Kapatma

    /// <summary>
    /// Kaydedilmemiş olay varken pencere kapatılırsa kullanıcıya sorulur:
    /// Evet → kaydet ve kapat, Hayır → kaydetmeden kapat, İptal → açık kal.
    /// Kayıtlar yine de diske kendiliğinden yazılmaz; karar her zaman kullanıcınındır.
    /// </summary>
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_oturum.KaydedilmemisDegisiklikVar || e.CloseReason == CloseReason.WindowsShutDown)
            return;

        _oturum.Durdur();
        var cevap = MessageBox.Show(this,
            $"{_oturum.KaydedilmemisOlaySayisi} olay henüz dosyaya kaydedilmedi.\n\n" +
            "Kapatmadan önce kaydetmek ister misiniz?",
            "Kaydedilmemiş olaylar", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        if (cevap == DialogResult.Cancel || (cevap == DialogResult.Yes && !KayitlariKaydet()))
            e.Cancel = true;
    }

    private void BtnFiltrele_Click(object? sender, EventArgs e)
    {
        try
        {
            var sonuc = _oturum.Filtrele(dtpBaslangic.Value, dtpBitis.Value);
            _filtreAktif = true;
            btnFiltreKaldir.Enabled = true;
            ListeyiDoldur(sonuc);
            DurumYaz($"Filtre aktif: {sonuc.Count} / {_oturum.Kayitlar.Count} olay gösteriliyor. " +
                     "Filtre açıkken yeni olaylar listeye eklenmez.");
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Geçersiz aralık", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnFiltreKaldir_Click(object? sender, EventArgs e)
    {
        FiltreyiKaldir();
        DurumYaz("Filtre kaldırıldı, tüm olaylar gösteriliyor.");
    }

    private void FiltreyiKaldir()
    {
        _filtreAktif = false;
        btnFiltreKaldir.Enabled = false;
        ListeyiDoldur(_oturum.Kayitlar);
    }

    private void ListeyiDoldur(IEnumerable<KeyLogEntry> kayitlar)
    {
        lvKayitlar.BeginUpdate();
        lvKayitlar.Items.Clear();
        lvKayitlar.Items.AddRange(kayitlar.Select(ListeOgesi).ToArray());
        lvKayitlar.EndUpdate();
        if (lvKayitlar.Items.Count > 0)
            lvKayitlar.EnsureVisible(lvKayitlar.Items.Count - 1);
    }

    // ------------------------------------------------------------ Odak kaybı

    /// <summary>Pencere odağı kaybedince kayıt durur; geri gelince kullanıcı açıkça Başlat'a basmalıdır.</summary>
    private void MainForm_Deactivate(object? sender, EventArgs e)
    {
        _tekrar.Sifirla();
        if (_oturum.KayitAcik)
        {
            _oturum.Durdur();
            DurumYaz("Pencere odaktan çıktı; güvenlik için kayıt durduruldu. Devam etmek için Başlat'a basın.");
        }
    }

    private void DurumYaz(string mesaj) => lblDurum.Text = mesaj;
}
