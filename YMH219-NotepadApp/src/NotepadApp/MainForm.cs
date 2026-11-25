using NotepadApp.Core.Models;
using NotepadApp.Core.Services;

namespace NotepadApp;

/// <summary>
/// Ana pencere. Kullanıcı etkileşimini yönetir; not işlemleri <see cref="NotDefteri"/>,
/// dosya işlemleri <see cref="NotDeposu"/> sınıfı tarafından yapılır.
/// </summary>
public partial class MainForm : Form
{
    private readonly NotDefteri _defter = new();
    private readonly NotDeposu _depo;

    private Guid? _seciliId;          // Düzenleyicide açık olan not (null = yeni not)
    private bool _duzenleyiciKirli;   // Düzenleyicide kaydedilmemiş değişiklik var mı?
    private bool _dosyaKirli;         // Bellekteki notlar dosyaya yazılmadı mı?
    private bool _arayuzGuncelleniyor; // Programatik güncellemelerde olayları yok saymak için

    public MainForm(string veriDosyasi)
    {
        InitializeComponent();
        _depo = new NotDeposu(veriDosyasi);
        _defter.Degisti += Defter_Degisti;
    }

    // ---------------------------------------------------------------- Yükleme / Kaydetme

    private void MainForm_Load(object? sender, EventArgs e)
    {
        lblDosya.Text = _depo.DosyaYolu;
        DosyadanYukle(ilkAcilis: true);
    }

    private void DosyadanYukle(bool ilkAcilis)
    {
        try
        {
            var sonuc = _depo.Yukle();
            _defter.Yukle(sonuc.Notlar);
            _dosyaKirli = false;

            if (sonuc.Uyari is not null)
                MessageBox.Show(this, sonuc.Uyari, "Not dosyası okunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            ListeyiYenile();
            YeniNotHazirla();
            DurumYaz(ilkAcilis
                ? $"{_defter.Sayi} not yüklendi."
                : $"Notlar dosyadan yeniden yüklendi ({_defter.Sayi} not).");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Not dosyası okunurken hata oluştu:\n{ex.Message}",
                "Dosya hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DurumYaz("Notlar yüklenemedi.");
        }
    }

    /// <summary>Koleksiyonu JSON dosyasına yazar. Başarılıysa true döner.</summary>
    private bool DosyayaKaydet()
    {
        try
        {
            _depo.Kaydet(_defter.Notlar);
            _dosyaKirli = false;
            DurumYaz($"Kaydedildi: {DateTime.Now:HH:mm:ss} ({_defter.Sayi} not)");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Notlar dosyaya kaydedilemedi:\n{ex.Message}",
                "Kaydetme hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DurumYaz("Kaydetme başarısız!");
            return false;
        }
    }

    private void Defter_Degisti(object? sender, EventArgs e)
    {
        _dosyaKirli = true;
        if (mnuOtomatikKaydet.Checked)
            DosyayaKaydet();
        else
            DurumYaz("Değişiklikler henüz dosyaya yazılmadı (Ctrl+S).");
    }

    // ---------------------------------------------------------------- Liste ve filtre

    private void ListeyiYenile()
    {
        _arayuzGuncelleniyor = true;
        try
        {
            // Kategori listelerini güncelle
            string? seciliFiltre = cmbFiltreKategori.SelectedItem as string;
            var kategoriler = _defter.Kategoriler();

            cmbFiltreKategori.Items.Clear();
            cmbFiltreKategori.Items.Add(NotDefteri.TumKategoriler);
            cmbFiltreKategori.Items.AddRange(kategoriler.ToArray());
            cmbFiltreKategori.SelectedItem = seciliFiltre is not null && cmbFiltreKategori.Items.Contains(seciliFiltre)
                ? seciliFiltre
                : NotDefteri.TumKategoriler;

            string kategoriMetni = cmbKategori.Text;
            cmbKategori.Items.Clear();
            cmbKategori.Items.AddRange(kategoriler.DefaultIfEmpty("Genel").ToArray());
            cmbKategori.Text = kategoriMetni;

            // Notları süzerek listele
            var liste = _defter.Filtrele(txtAra.Text, cmbFiltreKategori.SelectedItem as string);
            lstNotlar.BeginUpdate();
            lstNotlar.Items.Clear();
            foreach (var not in liste)
                lstNotlar.Items.Add(not);
            lstNotlar.EndUpdate();

            if (_seciliId is Guid id)
                lstNotlar.SelectedItem = liste.FirstOrDefault(n => n.Id == id);

            lblNotSayisi.Text = liste.Count == _defter.Sayi
                ? $"{_defter.Sayi} not"
                : $"{liste.Count} / {_defter.Sayi} not gösteriliyor";
        }
        finally
        {
            _arayuzGuncelleniyor = false;
        }
    }

    private void Filtre_Changed(object? sender, EventArgs e)
    {
        if (!_arayuzGuncelleniyor)
            ListeyiYenile();
    }

    private void LstNotlar_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_arayuzGuncelleniyor || lstNotlar.SelectedItem is not Not not || not.Id == _seciliId)
            return;

        if (!KaydedilmemisDegisiklikleriSor())
        {
            ListeyiYenile(); // seçimi eski nota geri al
            return;
        }

        NotuDuzenleyicideAc(not);
    }

    // ---------------------------------------------------------------- Düzenleyici

    private void NotuDuzenleyicideAc(Not not)
    {
        _arayuzGuncelleniyor = true;
        try
        {
            _seciliId = not.Id;
            txtBaslik.Text = not.Baslik;
            cmbKategori.Text = not.Kategori;
            if (!string.IsNullOrEmpty(not.RtfIcerik))
            {
                try { rtbIcerik.Rtf = not.RtfIcerik; }
                catch (ArgumentException) { rtbIcerik.Text = not.Icerik; } // bozuk RTF -> düz metin
            }
            else
            {
                rtbIcerik.Text = not.Icerik;
            }
            lblTarihler.Text = $"Oluşturulma: {not.OlusturmaTarihi:dd.MM.yyyy HH:mm}    " +
                               $"Son düzenleme: {not.DuzenlemeTarihi:dd.MM.yyyy HH:mm}";
            _duzenleyiciKirli = false;
            BasligiGuncelle();
        }
        finally
        {
            _arayuzGuncelleniyor = false;
        }
    }

    private void YeniNotHazirla()
    {
        _arayuzGuncelleniyor = true;
        try
        {
            _seciliId = null;
            lstNotlar.ClearSelected();
            txtBaslik.Clear();
            cmbKategori.Text = cmbFiltreKategori.SelectedItem is string k && k != NotDefteri.TumKategoriler ? k : "Genel";
            rtbIcerik.Clear();
            lblTarihler.Text = "Yeni not — kaydedildiğinde oluşturma tarihi atanacak.";
            _duzenleyiciKirli = false;
            BasligiGuncelle();
        }
        finally
        {
            _arayuzGuncelleniyor = false;
        }
        txtBaslik.Focus();
    }

    private void Duzenleyici_Changed(object? sender, EventArgs e)
    {
        if (_arayuzGuncelleniyor)
            return;
        _duzenleyiciKirli = true;
        BasligiGuncelle();
    }

    private void BasligiGuncelle()
    {
        string ad = _seciliId is null ? "Yeni not" : txtBaslik.Text;
        Text = $"{(_duzenleyiciKirli ? "* " : "")}{ad} — Not Defteri";
        btnSil.Enabled = mnuSil.Enabled = _seciliId is not null;
    }

    /// <summary>
    /// Düzenleyicide kaydedilmemiş değişiklik varsa kullanıcıya sorar.
    /// İşleme devam edilebilecekse true döner.
    /// </summary>
    private bool KaydedilmemisDegisiklikleriSor()
    {
        if (!_duzenleyiciKirli)
            return true;

        var cevap = MessageBox.Show(this, "Açık notta kaydedilmemiş değişiklikler var. Kaydedilsin mi?",
            "Kaydedilmemiş değişiklik", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        return cevap switch
        {
            DialogResult.Yes => NotuKaydet(),
            DialogResult.No => true,
            _ => false
        };
    }

    /// <summary>Düzenleyicideki notu koleksiyona ekler veya günceller.</summary>
    private bool NotuKaydet()
    {
        if (string.IsNullOrWhiteSpace(txtBaslik.Text))
        {
            MessageBox.Show(this, "Lütfen not için bir başlık girin.", "Başlık gerekli",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtBaslik.Focus();
            return false;
        }

        try
        {
            string rtf = rtbIcerik.Rtf ?? string.Empty;
            Not not;
            if (_seciliId is Guid id && _defter.Bul(id) is not null)
            {
                _defter.Guncelle(id, txtBaslik.Text, rtbIcerik.Text, cmbKategori.Text, rtf);
                not = _defter.Bul(id)!;
            }
            else
            {
                not = _defter.Ekle(txtBaslik.Text, rtbIcerik.Text, cmbKategori.Text, rtf);
            }

            _duzenleyiciKirli = false;
            ListeyiYenile();
            NotuDuzenleyicideAc(not);
            return true;
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Geçersiz değer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    // ---------------------------------------------------------------- Buton / menü olayları

    private void BtnYeni_Click(object? sender, EventArgs e)
    {
        if (KaydedilmemisDegisiklikleriSor())
        {
            YeniNotHazirla();
            DurumYaz("Yeni not oluşturuluyor.");
        }
    }

    private void BtnKaydet_Click(object? sender, EventArgs e)
    {
        bool yeniMi = _seciliId is null;
        if (NotuKaydet())
        {
            if (!mnuOtomatikKaydet.Checked)
                DosyayaKaydet();
            DurumYaz(yeniMi ? $"Not eklendi: {txtBaslik.Text}" : $"Not güncellendi: {txtBaslik.Text}");
        }
    }

    private void BtnSil_Click(object? sender, EventArgs e)
    {
        if (_seciliId is not Guid id || _defter.Bul(id) is not Not not)
        {
            MessageBox.Show(this, "Silmek için listeden bir not seçin.", "Not seçilmedi",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var cevap = MessageBox.Show(this, $"\"{not.Baslik}\" notu silinsin mi?\nBu işlem geri alınamaz.",
            "Notu sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (cevap != DialogResult.Yes)
            return;

        _defter.Sil(id);
        _duzenleyiciKirli = false;
        YeniNotHazirla();
        ListeyiYenile();
        DurumYaz($"Not silindi: {not.Baslik}");
    }

    private void BtnDisaAktar_Click(object? sender, EventArgs e)
    {
        if (_seciliId is not Guid id || _defter.Bul(id) is not Not not)
        {
            MessageBox.Show(this, "Dışa aktarmak için önce bir not seçin veya yeni notu kaydedin.",
                "Not seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_duzenleyiciKirli && !KaydedilmemisDegisiklikleriSor())
            return;

        using var dialog = new SaveFileDialog
        {
            Title = "Notu düz metin olarak kaydet",
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = TxtDisaAktarici.DosyaAdiOner(not),
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            TxtDisaAktarici.DisaAktar(not, dialog.FileName);
            DurumYaz($"Dışa aktarıldı: {dialog.FileName}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Dosya yazılamadı:\n{ex.Message}", "Dışa aktarma hatası",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MnuYenidenYukle_Click(object? sender, EventArgs e)
    {
        if (_dosyaKirli)
        {
            var cevap = MessageBox.Show(this,
                "Dosyaya yazılmamış değişiklikler kaybolacak. Yine de dosyadan yeniden yüklensin mi?",
                "Yeniden yükle", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (cevap != DialogResult.Yes)
                return;
        }
        DosyadanYukle(ilkAcilis: false);
    }

    private void MnuKalin_Click(object? sender, EventArgs e) => SecimStiliDegistir(FontStyle.Bold);
    private void MnuItalik_Click(object? sender, EventArgs e) => SecimStiliDegistir(FontStyle.Italic);

    private void SecimStiliDegistir(FontStyle stil)
    {
        var font = rtbIcerik.SelectionFont ?? rtbIcerik.Font;
        rtbIcerik.SelectionFont = new Font(font, font.Style ^ stil);
        rtbIcerik.Focus();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!KaydedilmemisDegisiklikleriSor())
        {
            e.Cancel = true;
            return;
        }

        if (_dosyaKirli)
        {
            var cevap = MessageBox.Show(this, "Notlar dosyaya kaydedilsin mi?", "Çıkış",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (cevap == DialogResult.Cancel || (cevap == DialogResult.Yes && !DosyayaKaydet()))
                e.Cancel = true;
        }
    }

    private void DurumYaz(string mesaj) => lblDurum.Text = mesaj;
}
