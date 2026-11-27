using System.Runtime.InteropServices;

namespace NotepadApp;

/// <summary>
/// Açık / karanlık tema. Formdaki tüm kontrollerin renkleri özyinelemeli olarak değiştirilir;
/// menü ve durum çubuğu için özel renk tablosu, pencere başlık çubuğu için Windows'un koyu mod özniteliği kullanılır.
/// </summary>
public static class Tema
{
    public sealed record Renkler(Color Zemin, Color Panel, Color Giris, Color Yazi, Color SolukYazi, Color Kenar, Color Vurgu);

    public static readonly Renkler Acik = new(
        SystemColors.Control, SystemColors.Control, SystemColors.Window, SystemColors.ControlText,
        SystemColors.GrayText, SystemColors.ControlDark, SystemColors.Highlight);

    public static readonly Renkler Karanlik = new(
        Color.FromArgb(32, 33, 36), Color.FromArgb(40, 42, 46), Color.FromArgb(24, 25, 28),
        Color.FromArgb(230, 232, 235), Color.FromArgb(150, 155, 162), Color.FromArgb(70, 74, 80), Color.FromArgb(56, 120, 200));

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public static void Uygula(Form form, bool karanlik)
    {
        var r = karanlik ? Karanlik : Acik;
        form.BackColor = r.Zemin;
        form.ForeColor = r.Yazi;
        KontrolleriBoya(form, r, karanlik);

        if (form.IsHandleCreated)
        {
            int deger = karanlik ? 1 : 0;
            try { DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref deger, sizeof(int)); }
            catch (DllNotFoundException) { /* eski Windows */ }
        }
        form.Invalidate(true);
    }

    private static void KontrolleriBoya(Control ust, Renkler r, bool karanlik)
    {
        foreach (Control k in ust.Controls)
        {
            switch (k)
            {
                case MenuStrip or StatusStrip or ToolStrip:
                    var ts = (ToolStrip)k;
                    ts.Renderer = karanlik ? new ToolStripProfessionalRenderer(new KaranlikMenuRenkleri()) : new ToolStripProfessionalRenderer();
                    ts.BackColor = r.Panel;
                    ts.ForeColor = r.Yazi;
                    foreach (ToolStripItem oge in ts.Items) OgeyiBoya(oge, r);
                    break;
                case TextBoxBase or ListBox or ComboBox:
                    k.BackColor = r.Giris;
                    k.ForeColor = r.Yazi;
                    if (k is TextBox t) t.BorderStyle = karanlik ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    if (k is ComboBox c) c.FlatStyle = karanlik ? FlatStyle.Flat : FlatStyle.Standard;
                    break;
                case Button b:
                    b.FlatStyle = karanlik ? FlatStyle.Flat : FlatStyle.Standard;
                    b.FlatAppearance.BorderColor = r.Kenar;
                    b.BackColor = karanlik ? r.Panel : SystemColors.Control;
                    b.ForeColor = r.Yazi;
                    b.UseVisualStyleBackColor = !karanlik;
                    break;
                case Label l:
                    l.BackColor = Color.Transparent;
                    l.ForeColor = l.Name is "lblTarihler" or "lblNotSayisi" ? r.SolukYazi : r.Yazi;
                    break;
                case SplitContainer s:
                    s.BackColor = r.Kenar;
                    s.Panel1.BackColor = s.Panel2.BackColor = r.Zemin;
                    break;
                default:
                    k.BackColor = r.Zemin;
                    k.ForeColor = r.Yazi;
                    break;
            }
            if (k is not ToolStrip)
                KontrolleriBoya(k, r, karanlik);
        }
    }

    private static void OgeyiBoya(ToolStripItem oge, Renkler r)
    {
        oge.ForeColor = r.Yazi;
        if (oge is ToolStripMenuItem m)
            foreach (ToolStripItem alt in m.DropDownItems) OgeyiBoya(alt, r);
    }

    /// <summary>Menüler için koyu renk tablosu.</summary>
    private sealed class KaranlikMenuRenkleri : ProfessionalColorTable
    {
        private static readonly Color Zemin = Color.FromArgb(40, 42, 46), Secili = Color.FromArgb(62, 66, 74), Kenar = Color.FromArgb(70, 74, 80);
        public override Color MenuStripGradientBegin => Zemin;
        public override Color MenuStripGradientEnd => Zemin;
        public override Color ToolStripDropDownBackground => Zemin;
        public override Color ImageMarginGradientBegin => Zemin;
        public override Color ImageMarginGradientMiddle => Zemin;
        public override Color ImageMarginGradientEnd => Zemin;
        public override Color MenuItemSelected => Secili;
        public override Color MenuItemSelectedGradientBegin => Secili;
        public override Color MenuItemSelectedGradientEnd => Secili;
        public override Color MenuItemPressedGradientBegin => Secili;
        public override Color MenuItemPressedGradientEnd => Secili;
        public override Color MenuItemBorder => Kenar;
        public override Color MenuBorder => Kenar;
        public override Color SeparatorDark => Kenar;
        public override Color SeparatorLight => Zemin;
        public override Color StatusStripGradientBegin => Zemin;
        public override Color StatusStripGradientEnd => Zemin;
        public override Color CheckBackground => Secili;
        public override Color CheckSelectedBackground => Secili;
    }
}
