namespace ChatApp;

/// <summary>
/// Açılış penceresi: sunucu veya istemci penceresi açılır.
/// Aynı uygulama içinden birden fazla istemci açılabilir (aynı bilgisayarda deneme için).
/// </summary>
public class BaslangicForm : Form
{
    private readonly Button btnSunucu;
    private readonly Button btnIstemci;
    private int _istemciSayaci;

    public BaslangicForm()
    {
        Text = "YMH219 Chat";
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(380, 210);

        var lblBaslik = new Label
        {
            Text = "İstemci-Sunucu Sohbet Uygulaması",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(10, 15, 360, 30)
        };
        var lblAciklama = new Label
        {
            Text = "Önce sunucuyu başlatın, sonra bir veya daha fazla istemci açın.\n" +
                   "Farklı bilgisayarlarda her biri ayrı ayrı açılabilir.",
            ForeColor = SystemColors.GrayText,
            Font = new Font("Segoe UI", 9F),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(10, 48, 360, 40)
        };
        btnSunucu = new Button { Name = "btnSunucu", Text = "Sunucu Penceresi", Bounds = new Rectangle(30, 105, 150, 60) };
        btnIstemci = new Button { Name = "btnIstemci", Text = "Yeni İstemci", Bounds = new Rectangle(200, 105, 150, 60) };
        btnSunucu.Click += (_, _) => new SunucuForm().Show();
        btnIstemci.Click += (_, _) => new IstemciForm(++_istemciSayaci).Show();

        Controls.AddRange(new Control[] { lblBaslik, lblAciklama, btnSunucu, btnIstemci });
    }
}
