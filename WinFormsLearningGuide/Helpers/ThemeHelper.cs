using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinFormsLearningGuide.Helpers;

/// <summary>Implemented by custom controls that need extra work when the theme changes.</summary>
public interface IThemeable
{
    void ApplyTheme(Theme theme);
}

public sealed class Theme
{
    public string Name { get; init; } = "";
    public bool IsDark { get; init; }
    public Color Background { get; init; }
    public Color Surface { get; init; }
    public Color Sidebar { get; init; }
    public Color TopBar { get; init; }
    public Color Border { get; init; }
    public Color Text { get; init; }
    public Color TextMuted { get; init; }
    public Color Accent { get; init; }
    public Color AccentText { get; init; }
    public Color Hover { get; init; }
    public Color Selection { get; init; }
    public Color SelectionText { get; init; }
    public Color InputBack { get; init; }
}

/// <summary>
/// Central place for colors. Controls are styled through their Tag:
/// "sidebar", "topbar", "surface", "border", "input" (panels) and
/// "caption", "muted", "link", "accent" (labels).
/// </summary>
public static class ThemeHelper
{
    public static readonly Theme Dark = new Theme
    {
        Name = "Dark",
        IsDark = true,
        Background = ColorTranslator.FromHtml("#1B1D21"),
        Surface = ColorTranslator.FromHtml("#23262C"),
        Sidebar = ColorTranslator.FromHtml("#16181C"),
        TopBar = ColorTranslator.FromHtml("#202329"),
        Border = ColorTranslator.FromHtml("#343842"),
        Text = ColorTranslator.FromHtml("#E4E6EB"),
        TextMuted = ColorTranslator.FromHtml("#9AA1AD"),
        Accent = ColorTranslator.FromHtml("#4C9AFF"),
        AccentText = Color.White,
        Hover = ColorTranslator.FromHtml("#2B2F37"),
        Selection = ColorTranslator.FromHtml("#2F4B73"),
        SelectionText = Color.White,
        InputBack = ColorTranslator.FromHtml("#101216")
    };

    public static readonly Theme Light = new Theme
    {
        Name = "Light",
        IsDark = false,
        Background = ColorTranslator.FromHtml("#F6F7F9"),
        Surface = ColorTranslator.FromHtml("#FFFFFF"),
        Sidebar = ColorTranslator.FromHtml("#EEF0F3"),
        TopBar = ColorTranslator.FromHtml("#FFFFFF"),
        Border = ColorTranslator.FromHtml("#D5D9E0"),
        Text = ColorTranslator.FromHtml("#1F2328"),
        TextMuted = ColorTranslator.FromHtml("#656D76"),
        Accent = ColorTranslator.FromHtml("#0969DA"),
        AccentText = Color.White,
        Hover = ColorTranslator.FromHtml("#E4E8EE"),
        Selection = ColorTranslator.FromHtml("#CFE3FF"),
        SelectionText = ColorTranslator.FromHtml("#0B2A55"),
        InputBack = Color.White
    };

    public static Theme Current { get; private set; } = Dark;

    public static void SetTheme(bool dark) => Current = dark ? Dark : Light;

    /// <summary>Scales a 96-DPI pixel value to the control's current DPI.</summary>
    public static int Scale(Control control, int value) =>
        (int)Math.Round(value * control.DeviceDpi / 96.0);

    /// <summary>Applies the current theme to a control and all of its descendants.</summary>
    public static void Apply(Control root) => ApplyRecursive(root, Current);

    private static void ApplyRecursive(Control c, Theme t)
    {
        ApplyTo(c, t);
        foreach (Control child in c.Controls)
            ApplyRecursive(child, t);
        if (c is IThemeable themeable)
            themeable.ApplyTheme(t);
    }

    private static void ApplyTo(Control c, Theme t)
    {
        string tag = c.Tag as string;
        switch (c)
        {
            case Form f:
                f.BackColor = t.Background;
                f.ForeColor = t.Text;
                break;
            case SplitContainer sc:
                sc.BackColor = t.Border;
                break;
            case TextBox tb:
                tb.BackColor = t.InputBack;
                tb.ForeColor = t.Text;
                tb.BorderStyle = BorderStyle.FixedSingle;
                break;
            case Button b:
                StyleButton(b, t, tag);
                break;
            case TreeView tv:
                tv.BackColor = t.Sidebar;
                tv.ForeColor = t.Text;
                tv.BorderStyle = BorderStyle.None;
                break;
            case Label l:
                l.ForeColor = LabelColor(tag, t);
                break;
            case Panel p:
                p.BackColor = RoleColor(tag, t);
                p.ForeColor = t.Text;
                break;
            case UserControl u:
                u.BackColor = RoleColor(tag, t);
                u.ForeColor = t.Text;
                break;
        }
    }

    private static Color RoleColor(string tag, Theme t)
    {
        switch (tag)
        {
            case "sidebar": return t.Sidebar;
            case "topbar": return t.TopBar;
            case "surface": return t.Surface;
            case "border": return t.Border;
            case "input": return t.InputBack;
            default: return t.Background;
        }
    }

    private static Color LabelColor(string tag, Theme t)
    {
        switch (tag)
        {
            case "caption":
            case "muted": return t.TextMuted;
            case "link":
            case "accent": return t.Accent;
            default: return t.Text;
        }
    }

    private static void StyleButton(Button b, Theme t, string tag)
    {
        bool accent = tag == "accent";
        b.UseVisualStyleBackColor = false;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = accent ? t.Accent : t.Border;
        b.FlatAppearance.MouseOverBackColor = t.Hover;
        b.FlatAppearance.MouseDownBackColor = t.Selection;
        b.BackColor = accent ? t.Accent : t.Surface;
        b.ForeColor = accent ? t.AccentText : t.Text;
        b.Cursor = Cursors.Hand;
    }

    // ---- Native window theming (dark title bar, dark scrollbars) ----

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void ApplyNativeTheme(Control control, Theme t)
    {
        if (!control.IsHandleCreated) return;
        try { SetWindowTheme(control.Handle, t.IsDark ? "DarkMode_Explorer" : "Explorer", null); }
        catch (Exception) { /* cosmetic only */ }
    }

    public static void ApplyTitleBar(Form form)
    {
        if (!form.IsHandleCreated) return;
        try
        {
            int value = Current.IsDark ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref value, sizeof(int));
        }
        catch (Exception) { /* cosmetic only */ }
    }
}
