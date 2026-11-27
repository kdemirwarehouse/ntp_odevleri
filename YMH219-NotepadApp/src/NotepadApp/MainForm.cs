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
    private FileSystemWatcher? _izleyici;          // Başka pencerelerin yaptığı değişiklikleri fark etmek için
    private readonly System.Windows.Forms.Timer _disDegisiklikZamanlayici = new() { Interval = 600 };
    private bool _kendiYazmam;                     // Kendi kaydımızın tetiklediği izleyici olaylarını yok say
    private DateTime _sonKendiYazma;               // İzleyici olayları gecikmeli gelir: kendi yazmamızdan sonraki 1,5 sn yok sayılır

    // ---- Geri al / yinele: düzenleyicinin RTF + imleç anlık görüntüleri
    private readonly record struct EditorDurumu(string Rtf, int Imlec);
    private sealed class RtfKarsilastirici : IEqualityComparer<EditorDurumu>
    {
        public bool Equals(EditorDurumu a, EditorDurumu b) => a.Rtf == b.Rtf;
        public int GetHashCode(EditorDurumu d) => d.Rtf.GetHashCode();
    }
    private readonly GeriAlGecmisi<EditorDurumu> _gecmis = new(200, new RtfKarsilastirici());
    private readonly System.Windows.Forms.Timer _anlikGoruntuZamanlayici = new() { Interval = 600 };
    private bool _geriAliniyor;
    private ToolStripMenuItem mnuGeriAl = null!, mnuYinele = null!, mnuSabitle = null!;

    private readonly TextBox txtEtiketler = new() { Name = "txtEtiketler", Width = 260, PlaceholderText = "etiketler: sınav, proje, acil" };
    private readonly ComboBox cmbFiltreEtiket = new() { Name = "cmbFiltreEtiket", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

    private readonly string _ayarYolu = Ayarlar.VarsayilanYol();
    private Ayarlar _ayarlar = new();
    private ToolStripMenuItem mnuKaranlikTema = null!;

    public MainForm(string veriDosyasi)
    {
        InitializeComponent();
        EtiketAlanlariniKur();
        TemaMenusunuKur();
        _depo = new NotDeposu(veriDosyasi);
        _defter.Degisti += Defter_Degisti;
        GeriAlMenusunuKur();
    }

    // ---------------------------------------------------------------- Tema ve ayarlar

    private void TemaMenusunuKur()
    {
        _ayarlar = Ayarlar.Yukle(_ayarYolu);
        mnuOtomatikKaydet.Checked = _ayarlar.OtomatikKaydet;
        mnuOtomatikKaydet.CheckedChanged += (_, _) => { _ayarlar.OtomatikKaydet = mnuOtomatikKaydet.Checked; _ayarlar.Kaydet(_ayarYolu); };

        mnuKaranlikTema = new ToolStripMenuItem("&Karanlık Tema", null, (_, _) => TemaDegistir(!_ayarlar.KaranlikTema))
        {
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.D,
            Checked = _ayarlar.KaranlikTema
        };
        var mnuGorunum = new ToolStripMenuItem("&Görünüm");
        mnuGorunum.DropDownItems.Add(mnuKaranlikTema);
        menuStrip.Items.Add(mnuGorunum);
        HandleCreated += (_, _) => { Tema.Uygula(this, _ayarlar.KaranlikTema); IcerikRenginiUygula(); };
    }

    /// <summary>
    /// RichTextBox, RTF'deki "otomatik" yazı rengini tema rengi yerine sistem rengiyle çizer.
    /// Bu yüzden metin yüklendikten / tema değiştikten sonra tüm metin tema rengine boyanır.
    /// </summary>
    private void IcerikRenginiUygula()
    {
        bool onceki = _arayuzGuncelleniyor;
        _arayuzGuncelleniyor = true;
        try
        {
            int bas = rtbIcerik.SelectionStart, uz = rtbIcerik.SelectionLength;
            rtbIcerik.SelectAll();
            rtbIcerik.SelectionColor = _ayarlar.KaranlikTema ? Tema.Karanlik.Yazi : SystemColors.WindowText;
            rtbIcerik.Select(bas, uz);
        }
        finally { _arayuzGuncelleniyor = onceki; }
    }

    /// <summary>Kaydedilecek RTF: tema renginden bağımsız olsun diye geçici bir kopyada metin siyaha çevrilir.</summary>
    private string TemadanBagimsizRtf()
    {
        using var kopya = new RichTextBox { Rtf = rtbIcerik.Rtf };
        kopya.SelectAll();
        kopya.SelectionColor = Color.Black;
        return kopya.Rtf ?? string.Empty;
    }

    private void TemaDegistir(bool karanlik)
    {
        _ayarlar.KaranlikTema = karanlik;
        mnuKaranlikTema.Checked = karanlik;
        _ayarlar.Kaydet(_ayarYolu);
        Tema.Uygula(this, karanlik);
        IcerikRenginiUygula();
        GecmisiSifirla();
        DurumYaz(karanlik ? "Karanlık tema açıldı." : "Açık tema açıldı.");
    }

    // ---------------------------------------------------------------- Etiketler

    /// <summary>Düzenleyicide kategorinin yanına etiket kutusu, filtre paneline etiket filtresi eklenir.</summary>
    private void EtiketAlanlariniKur()
    {
        pnlDuzenleyici.Controls.Remove(cmbKategori);
        cmbKategori.Dock = DockStyle.None;
        cmbKategori.Margin = new Padding(0, 3, 12, 3);
        var satir = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        var lblEtiket = new Label { Text = "Etiketler:", AutoSize = true, Margin = new Padding(3, 7, 3, 0) };
        txtEtiketler.TextChanged += Duzenleyici_Changed;
        satir.Controls.AddRange(new Control[] { cmbKategori, lblEtiket, txtEtiketler });
        pnlDuzenleyici.Controls.Add(satir, 1, 1);

        pnlFiltre.RowCount = 4;
        pnlFiltre.SetRow(lblNotSayisi, 3);
        pnlFiltre.Controls.Add(new Label { Text = "Etiket:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        pnlFiltre.Controls.Add(cmbFiltreEtiket, 1, 2);
        cmbFiltreEtiket.SelectedIndexChanged += Filtre_Changed;
    }

    // ---------------------------------------------------------------- Geri al / yinele

    /// <summary>
    /// Düzen menüsü: Ctrl+Z / Ctrl+Y menü kısayolları RichTextBox'ın kendi (tek seviyeli, biçimlendirmede tutarsız)
    /// geri almasından önce işlenir; 200 adımlık kendi geçmişimiz kullanılır.
    /// </summary>
    private void GeriAlMenusunuKur()
    {
        mnuGeriAl = new ToolStripMenuItem("&Geri Al", null, (_, _) => GeriAlYinele(geriAl: true)) { ShortcutKeys = Keys.Control | Keys.Z };
        mnuYinele = new ToolStripMenuItem("&Yinele", null, (_, _) => GeriAlYinele(geriAl: false)) { ShortcutKeys = Keys.Control | Keys.Y };
        var mnuDuzen = new ToolStripMenuItem("D&üzen");
        mnuDuzen.DropDownItems.AddRange(new ToolStripItem[] { mnuGeriAl, mnuYinele });
        mnuDuzen.DropDownOpening += (_, _) => GeriAlMenusunuGuncelle();
        menuStrip.Items.Insert(menuStrip.Items.IndexOf(mnuBicim), mnuDuzen);

        var mnuXmlDisa = new ToolStripMenuItem("Tüm Notları &XML Olarak Dışa Aktar...", null, MnuXmlDisaAktar_Click);
        var mnuXmlIce = new ToolStripMenuItem("XML'den &İçe Aktar...", null, MnuXmlIceAktar_Click);
        int konum = mnuDosya.DropDownItems.IndexOf(mnuAyrac);
        mnuDosya.DropDownItems.Insert(konum, new ToolStripSeparator());
        mnuDosya.DropDownItems.Insert(konum + 1, mnuXmlDisa);
        mnuDosya.DropDownItems.Insert(konum + 2, mnuXmlIce);

        mnuSabitle = new ToolStripMenuItem("Notu &Sabitle", null, MnuSabitle_Click) { ShortcutKeys = Keys.Control | Keys.P };
        mnuNot.DropDownItems.Insert(0, mnuSabitle);
        mnuNot.DropDownOpening += (_, _) =>
        {
            var not = _seciliId is Guid id ? _defter.Bul(id) : null;
            mnuSabitle.Enabled = not is not null;
            mnuSabitle.Text = not?.Sabitlenmis == true ? "&Sabitlemeyi Kaldır" : "Notu &Sabitle";
        };

        rtbIcerik.TextChanged += (_, _) =>
        {
            if (_arayuzGuncelleniyor || _geriAliniyor) return;
            _anlikGoruntuZamanlayici.Stop();
            _anlikGoruntuZamanlayici.Start();   // yazma durduktan 0,6 sn sonra tek adım olarak kaydet
        };
        _anlikGoruntuZamanlayici.Tick += (_, _) => AnlikGoruntuAl();
        GeriAlMenusunuGuncelle();
    }

    private EditorDurumu SimdikiDurum() => new(rtbIcerik.Rtf ?? "", rtbIcerik.SelectionStart);

    private void AnlikGoruntuAl()
    {
        _anlikGoruntuZamanlayici.Stop();
        _gecmis.Kaydet(SimdikiDurum());
        GeriAlMenusunuGuncelle();
    }

    private void GecmisiSifirla()
    {
        _anlikGoruntuZamanlayici.Stop();
        _gecmis.Sifirla(SimdikiDurum());
        GeriAlMenusunuGuncelle();
    }

    private void GeriAlYinele(bool geriAl)
    {
        if (_anlikGoruntuZamanlayici.Enabled)
            AnlikGoruntuAl();   // bekleyen yazma önce bir adım olsun
        if (geriAl ? !_gecmis.GeriAlinabilir : !_gecmis.Yinelenebilir)
            return;

        var durum = geriAl ? _gecmis.GeriAl() : _gecmis.Yinele();
        _geriAliniyor = true;
        try
        {
            rtbIcerik.Rtf = durum.Rtf;
            rtbIcerik.Select(Math.Min(durum.Imlec, rtbIcerik.TextLength), 0);
        }
        finally { _geriAliniyor = false; }
        rtbIcerik.Focus();
        GeriAlMenusunuGuncelle();
        DurumYaz(geriAl ? $"Geri alındı ({_gecmis.GeriAlAdimSayisi} adım daha geri alınabilir)."
                        : $"Yinelendi ({_gecmis.YineleAdimSayisi} adım daha yinelenebilir).");
    }

    private void GeriAlMenusunuGuncelle()
    {
        mnuGeriAl.Enabled = _gecmis.GeriAlinabilir || _anlikGoruntuZamanlayici.Enabled;
        mnuYinele.Enabled = _gecmis.Yinelenebilir;
        mnuGeriAl.Text = $"&Geri Al ({_gecmis.GeriAlAdimSayisi})";
        mnuYinele.Text = $"&Yinele ({_gecmis.YineleAdimSayisi})";
    }

    // ---------------------------------------------------------------- Yükleme / Kaydetme

    private void MainForm_Load(object? sender, EventArgs e)
    {
        lblDosya.Text = _depo.KlasorYolu;
        DosyadanYukle(ilkAcilis: true);
        DisDegisiklikleriIzle();
    }

    // ---------------------------------------------------------------- Çoklu pencere / dış değişiklik

    /// <summary>
    /// Not klasörü izlenir: başka bir pencere bir notu kaydederse ve bu pencerede kaydedilmemiş değişiklik yoksa
    /// notlar sessizce yeniden yüklenir; varsa kullanıcı bilgilendirilir, çakışma kaydetme anında çözülür.
    /// </summary>
    private void DisDegisiklikleriIzle()
    {
        try
        {
            Directory.CreateDirectory(_depo.KlasorYolu);
            _izleyici = new FileSystemWatcher(_depo.KlasorYolu, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                SynchronizingObject = this,   // olaylar UI iş parçacığında gelsin
                EnableRaisingEvents = true
            };
            FileSystemEventHandler tetikle = (_, _) => { if (!_kendiYazmam && (DateTime.Now - _sonKendiYazma).TotalMilliseconds > 1500) { _disDegisiklikZamanlayici.Stop(); _disDegisiklikZamanlayici.Start(); } };
            _izleyici.Changed += tetikle;
            _izleyici.Created += tetikle;
            _izleyici.Deleted += tetikle;
            _izleyici.Renamed += (s, e) => tetikle(s, e);
            _disDegisiklikZamanlayici.Tick += (_, _) => { _disDegisiklikZamanlayici.Stop(); DisDegisiklikAlgilandi(); };
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            DurumYaz("Not klasörü izlenemiyor; başka pencerelerin değişiklikleri otomatik görünmeyecek.");
        }
    }

    private void DisDegisiklikAlgilandi()
    {
        if (_duzenleyiciKirli || _dosyaKirli)
        {
            DurumYaz("Başka bir pencere notları değiştirdi. Kaydettiğinizde çakışmalar size sorulacak.");
            return;
        }
        Guid? acik = _seciliId;
        DosyadanYukle(ilkAcilis: false);
        if (acik is Guid id && _defter.Bul(id) is Not not)
            NotuDuzenleyicideAc(not);
        DurumYaz($"Başka bir penceredeki değişiklikler yüklendi ({DateTime.Now:HH:mm:ss}).");
    }

    /// <summary>Kaydetme sırasında başka pencerenin değiştirdiği notlar için kullanıcıya sorar.</summary>
    private void CakismalariCoz(List<Not> cakismalar)
    {
        foreach (var disk in cakismalar)
        {
            var yerel = _defter.Bul(disk.Id);
            string ad = yerel?.Baslik ?? disk.Baslik;
            var cevap = MessageBox.Show(this,
                $"\"{ad}\" notu siz düzenlerken başka bir pencerede değiştirildi " +
                $"(diskteki: {disk.DuzenlemeTarihi:HH:mm:ss}).\n\n" +
                "Evet: Benim sürümüm kaydedilsin (diğerinin üzerine yaz)\n" +
                "Hayır: Diskteki sürümü al (benim değişikliğim atılsın)\n" +
                "İptal: İkisini de tut (benimki kopya olarak eklensin)",
                "Not çakışması", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

            _kendiYazmam = true;
            try
            {
                if (yerel is null)
                {
                    // Bu pencerede silinmiş ama başka pencerede değiştirilmiş: Hayır/İptal => geri getir
                    if (cevap != DialogResult.Yes) { _depo.DisktekiniKabulEt(disk); _defter.Degistir(disk); }
                    else { _depo.DisktekiniKabulEt(disk); _depo.Kaydet(_defter.Notlar); }
                }
                else if (cevap == DialogResult.Yes)
                    _depo.BeniminkiniYaz(yerel, disk);
                else if (cevap == DialogResult.No)
                {
                    _depo.DisktekiniKabulEt(disk);
                    _defter.Degistir(disk);
                }
                else
                {
                    _defter.KopyasiniEkle(yerel);
                    _depo.DisktekiniKabulEt(disk);
                    _defter.Degistir(disk);
                }
                _depo.Kaydet(_defter.Notlar);
            }
            finally { _kendiYazmam = false; _sonKendiYazma = DateTime.Now; }
        }
        ListeyiYenile();
        if (_seciliId is Guid id && _defter.Bul(id) is Not acik)
            NotuDuzenleyicideAc(acik);
        _dosyaKirli = false;
        DurumYaz($"{cakismalar.Count} çakışma çözüldü.");
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
            KayitSonucu sonuc;
            _kendiYazmam = true;
            try { sonuc = _depo.Kaydet(_defter.Notlar); }
            finally { _kendiYazmam = false; _sonKendiYazma = DateTime.Now; }
            _dosyaKirli = false;
            DurumYaz($"Kaydedildi: {DateTime.Now:HH:mm:ss} ({_defter.Sayi} not; {sonuc.Yazilan} dosya yazıldı, {sonuc.Silinen} silindi)");
            if (sonuc.Cakismalar.Count > 0)
                BeginInvoke(() => CakismalariCoz(sonuc.Cakismalar));
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

            string? seciliEtiket = cmbFiltreEtiket.SelectedItem as string;
            cmbFiltreEtiket.Items.Clear();
            cmbFiltreEtiket.Items.Add(NotDefteri.TumEtiketler);
            cmbFiltreEtiket.Items.AddRange(_defter.Etiketler().ToArray());
            cmbFiltreEtiket.SelectedItem = seciliEtiket is not null && cmbFiltreEtiket.Items.Contains(seciliEtiket)
                ? seciliEtiket
                : NotDefteri.TumEtiketler;

            string kategoriMetni = cmbKategori.Text;
            cmbKategori.Items.Clear();
            cmbKategori.Items.AddRange(kategoriler.DefaultIfEmpty("Genel").ToArray());
            cmbKategori.Text = kategoriMetni;

            // Notları süzerek listele
            var liste = _defter.Filtrele(txtAra.Text, cmbFiltreKategori.SelectedItem as string, cmbFiltreEtiket.SelectedItem as string);
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
            txtEtiketler.Text = not.EtiketMetni;
            if (!string.IsNullOrEmpty(not.RtfIcerik))
            {
                try { rtbIcerik.Rtf = not.RtfIcerik; }
                catch (ArgumentException) { rtbIcerik.Text = not.Icerik; } // bozuk RTF -> düz metin
            }
            else
            {
                rtbIcerik.Text = not.Icerik;
            }
            IcerikRenginiUygula();
            lblTarihler.Text = $"Oluşturulma: {not.OlusturmaTarihi:dd.MM.yyyy HH:mm}    " +
                               $"Son düzenleme: {not.DuzenlemeTarihi:dd.MM.yyyy HH:mm}";
            _duzenleyiciKirli = false;
            BasligiGuncelle();
        }
        finally
        {
            _arayuzGuncelleniyor = false;
        }
        GecmisiSifirla();
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
            txtEtiketler.Text = cmbFiltreEtiket.SelectedItem is string e && e != NotDefteri.TumEtiketler ? e : "";
            lblTarihler.Text = "Yeni not — kaydedildiğinde oluşturma tarihi atanacak.";
            _duzenleyiciKirli = false;
            BasligiGuncelle();
        }
        finally
        {
            _arayuzGuncelleniyor = false;
        }
        GecmisiSifirla();
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
            string rtf = TemadanBagimsizRtf();
            var etiketler = Not.EtiketleriAyristir(txtEtiketler.Text);
            Not not;
            if (_seciliId is Guid id && _defter.Bul(id) is not null)
            {
                _defter.Guncelle(id, txtBaslik.Text, rtbIcerik.Text, cmbKategori.Text, rtf, etiketler);
                not = _defter.Bul(id)!;
            }
            else
            {
                not = _defter.Ekle(txtBaslik.Text, rtbIcerik.Text, cmbKategori.Text, rtf, etiketler);
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
            Title = "Notu dışa aktar",
            Filter = "Zengin metin — kalın/italik korunur (*.rtf)|*.rtf|Web sayfası — kalın/italik korunur (*.html)|*.html|Düz metin (*.txt)|*.txt",
            FileName = Path.GetFileNameWithoutExtension(TxtDisaAktarici.DosyaAdiOner(not)),
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            switch (Path.GetExtension(dialog.FileName).ToLowerInvariant())
            {
                case ".rtf": BicimliDisaAktarici.RtfOlarakAktar(not, dialog.FileName); break;
                case ".html": case ".htm": BicimliDisaAktarici.HtmlOlarakAktar(not, dialog.FileName); break;
                default: TxtDisaAktarici.DisaAktar(not, dialog.FileName); break;
            }
            DurumYaz($"Dışa aktarıldı: {dialog.FileName}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Dosya yazılamadı:\n{ex.Message}", "Dışa aktarma hatası",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Seçili notu sabitler / sabitlemeyi kaldırır; sabit notlar listenin en üstünde 📌 ile görünür.</summary>
    private void MnuSabitle_Click(object? sender, EventArgs e)
    {
        if (_seciliId is not Guid id || _defter.Bul(id) is not Not not)
        {
            DurumYaz("Sabitlemek için listeden bir not seçin.");
            return;
        }
        bool sabit = _defter.SabitlemeDegistir(id);
        ListeyiYenile();
        DurumYaz(sabit ? $"Sabitlendi: {not.Baslik}" : $"Sabitleme kaldırıldı: {not.Baslik}");
    }

    private void MnuXmlDisaAktar_Click(object? sender, EventArgs e)
    {
        if (_defter.Sayi == 0) { DurumYaz("Dışa aktarılacak not yok."); return; }
        using var dialog = new SaveFileDialog
        {
            Title = "Tüm notları XML olarak dışa aktar",
            Filter = "XML not arşivi (*.xml)|*.xml",
            FileName = $"notlar-{DateTime.Now:yyyyMMdd-HHmm}.xml",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            XmlNotAktarici.DisaAktar(_defter.Notlar, dialog.FileName);
            DurumYaz($"{_defter.Sayi} not XML olarak dışa aktarıldı: {dialog.FileName}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Dosya yazılamadı:\n{ex.Message}", "XML dışa aktarma", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MnuXmlIceAktar_Click(object? sender, EventArgs e)
    {
        if (!KaydedilmemisDegisiklikleriSor()) return;
        using var dialog = new OpenFileDialog { Title = "XML not arşivini içe aktar", Filter = "XML not arşivi (*.xml)|*.xml" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var (eklenen, guncellenen, atlanan) = _defter.IceAktar(XmlNotAktarici.IceAktar(dialog.FileName));
            ListeyiYenile();
            DurumYaz($"İçe aktarıldı: {eklenen} yeni, {guncellenen} güncellenen, {atlanan} atlanan (zaten güncel) not.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Xml.XmlException)
        {
            MessageBox.Show(this, $"XML dosyası okunamadı:\n{ex.Message}", "XML içe aktarma", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        if (_anlikGoruntuZamanlayici.Enabled)
            AnlikGoruntuAl();
        var font = rtbIcerik.SelectionFont ?? rtbIcerik.Font;
        rtbIcerik.SelectionFont = new Font(font, font.Style ^ stil);
        AnlikGoruntuAl();   // biçim değişikliği ayrı bir geri alma adımıdır
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
