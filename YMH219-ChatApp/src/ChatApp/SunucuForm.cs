using System.Net.Sockets;
using ChatApp.Core;

namespace ChatApp;

/// <summary>
/// Sunucu penceresi: sunucuyu başlatır/durdurur, bağlı istemcileri ve olay günlüğünü gösterir.
/// ChatSunucu olayları arka plan iş parçacığından geldiği için arayüz BeginInvoke ile güncellenir.
/// </summary>
public class SunucuForm : Form
{
    private readonly ChatSunucu _sunucu = new();

    private readonly NumericUpDown nudPort;
    private readonly Button btnBaslat;
    private readonly Button btnDurdur;
    private readonly Label lblDurum;
    private readonly ListView lvIstemciler;
    private readonly ListBox lstGunluk;

    public SunucuForm()
    {
        Text = "Chat Sunucusu";
        Font = new Font("Segoe UI", 9.5F);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(600, 420);

        // --- Üst panel: port ve butonlar
        var pnlUst = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 10, 8, 0) };
        nudPort = new NumericUpDown { Name = "nudPort", Minimum = 1, Maximum = 65535, Value = 5000, Width = 80 };
        btnBaslat = new Button { Name = "btnBaslat", Text = "Sunucuyu Başlat", AutoSize = true };
        btnDurdur = new Button { Name = "btnDurdur", Text = "Durdur", AutoSize = true, Enabled = false };
        lblDurum = new Label
        {
            Name = "lblDurum", AutoSize = true, Padding = new Padding(10, 5, 10, 5),
            ForeColor = Color.White, BackColor = Color.DimGray, Text = "Durduruldu",
            Font = new Font(Font, FontStyle.Bold), Margin = new Padding(16, 0, 0, 0)
        };
        pnlUst.Controls.AddRange(new Control[]
        {
            new Label { Text = "Port:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) },
            nudPort, btnBaslat, btnDurdur, lblDurum
        });

        // --- Bağlı istemciler
        var grpIstemciler = new GroupBox { Text = "Bağlı istemciler", Dock = DockStyle.Top, Height = 200, Padding = new Padding(8) };
        lvIstemciler = new ListView
        {
            Name = "lvIstemciler", Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true
        };
        lvIstemciler.Columns.Add("Kullanıcı adı", 150);
        lvIstemciler.Columns.Add("IP adresi : port", 180);
        lvIstemciler.Columns.Add("Bağlanma zamanı", 150);
        lvIstemciler.Columns.Add("Mesaj", 70);
        grpIstemciler.Controls.Add(lvIstemciler);

        // --- Günlük
        var grpGunluk = new GroupBox { Text = "Sunucu günlüğü", Dock = DockStyle.Fill, Padding = new Padding(8) };
        lstGunluk = new ListBox { Name = "lstGunluk", Dock = DockStyle.Fill, IntegralHeight = false, Font = new Font("Consolas", 9.5F) };
        grpGunluk.Controls.Add(lstGunluk);

        var pnlIcerik = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 8) };
        pnlIcerik.Controls.Add(grpGunluk);
        pnlIcerik.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
        pnlIcerik.Controls.Add(grpIstemciler);
        Controls.Add(pnlIcerik);
        Controls.Add(pnlUst);

        btnBaslat.Click += BtnBaslat_Click;
        btnDurdur.Click += (_, _) => { _sunucu.Durdur(); ArayuzuGuncelle(); };
        FormClosed += (_, _) => _sunucu.Dispose();

        // Core olayları -> arayüz (UI iş parçacığına aktarılarak)
        _sunucu.Gunluk += (_, metin) => UIdaCalistir(() => GunlugeEkle(metin));
        _sunucu.IstemciBaglandi += (_, _) => UIdaCalistir(IstemciListesiniYenile);
        _sunucu.IstemciAyrildi += (_, _) => UIdaCalistir(IstemciListesiniYenile);
        _sunucu.MesajIletildi += (_, m) => UIdaCalistir(() => { GunlugeEkle(m.GosterimMetni()); IstemciListesiniYenile(); });
    }

    private void BtnBaslat_Click(object? sender, EventArgs e)
    {
        try
        {
            _sunucu.Baslat((int)nudPort.Value);
        }
        catch (SocketException ex)
        {
            MessageBox.Show(this,
                $"Sunucu {nudPort.Value} portunda başlatılamadı.\n{ex.Message}\n\nPort başka bir uygulama tarafından kullanılıyor olabilir; farklı bir port deneyin.",
                "Sunucu başlatılamadı", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        ArayuzuGuncelle();
    }

    private void ArayuzuGuncelle()
    {
        bool calisiyor = _sunucu.Calisiyor;
        btnBaslat.Enabled = !calisiyor;
        nudPort.Enabled = !calisiyor;
        btnDurdur.Enabled = calisiyor;
        lblDurum.Text = calisiyor ? $"Çalışıyor — port {_sunucu.Port}" : "Durduruldu";
        lblDurum.BackColor = calisiyor ? Color.FromArgb(30, 140, 60) : Color.DimGray;
        Text = calisiyor ? $"Chat Sunucusu — port {_sunucu.Port}" : "Chat Sunucusu";
        IstemciListesiniYenile();
    }

    private void IstemciListesiniYenile()
    {
        lvIstemciler.BeginUpdate();
        lvIstemciler.Items.Clear();
        foreach (var i in _sunucu.Istemciler.OrderBy(i => i.BaglanmaZamani))
        {
            var oge = new ListViewItem(i.KullaniciAdi);
            oge.SubItems.Add(i.UzakAdres);
            oge.SubItems.Add(i.BaglanmaZamani.ToString("dd.MM.yyyy HH:mm:ss"));
            oge.SubItems.Add(i.MesajSayisi.ToString());
            lvIstemciler.Items.Add(oge);
        }
        lvIstemciler.EndUpdate();
    }

    private void GunlugeEkle(string metin)
    {
        lstGunluk.Items.Add(metin);
        lstGunluk.TopIndex = Math.Max(0, lstGunluk.Items.Count - 1);
    }

    private void UIdaCalistir(Action islem)
    {
        if (IsDisposed || !IsHandleCreated)
            return;
        BeginInvoke(islem);
    }
}
