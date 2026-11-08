using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFormsLearningGuide.Services;

/// <summary>How an appearance preview was produced.</summary>
public enum PreviewMode
{
    /// <summary>The real System.Windows.Forms control, rendered to a bitmap.</summary>
    Live,
    /// <summary>Real controls arranged to illustrate something the control itself does not draw (for example a tooltip).</summary>
    Simulated,
    /// <summary>The component has no visual appearance at run time.</summary>
    None
}

/// <summary>One rendered illustration. The caller owns (and must dispose) the image.</summary>
public sealed class PreviewImage
{
    public Bitmap Image { get; set; }
    public string Caption { get; set; }
}

/// <summary>
/// Creates real WinForms controls with default settings and renders them to bitmaps
/// for the Appearance section. Everything is drawn on a "form" background so it looks
/// like it would on a real form, whatever theme the guide itself uses.
/// </summary>
public sealed class ControlPreviewService
{
    private const int Pad = 16;

    private sealed class Spec
    {
        public Func<Control> Build;
        public PreviewMode Mode = PreviewMode.Live;
        public bool ShowDisabled;
        public string Caption;
    }

    private readonly Dictionary<string, Spec> _specs;

    public ControlPreviewService()
    {
        _specs = new Dictionary<string, Spec>(StringComparer.OrdinalIgnoreCase)
        {
            ["Label"] = Live(() => Auto(new Label { Text = "Hello, WinForms!" })),
            ["Button"] = Live(() => new Button { Text = "Click me", Size = new Size(100, 32) }, disabled: true),
            ["TextBox"] = Live(() => new TextBox { Text = "Sample text", Width = 180 }, disabled: true),
            ["RichTextBox"] = Live(BuildRichTextBox, disabled: true),
            ["MaskedTextBox"] = Live(() => new MaskedTextBox { Mask = "(000) 000-0000", Text = "5551234567", Width = 150 }, disabled: true),
            ["CheckBox"] = Live(BuildCheckBoxes, disabled: true),
            ["RadioButton"] = Live(BuildRadioButtons, disabled: true),
            ["ComboBox"] = Live(BuildComboBox, disabled: true),
            ["ListBox"] = Live(BuildListBox, disabled: true),
            ["CheckedListBox"] = Live(BuildCheckedListBox, disabled: true),
            ["PictureBox"] = Live(BuildPictureBox),
            ["DateTimePicker"] = Live(() => new DateTimePicker { Value = new DateTime(2026, 1, 15), Width = 200 }, disabled: true),
            ["MonthCalendar"] = Live(BuildMonthCalendar),
            ["NumericUpDown"] = Live(() => new NumericUpDown { Value = 42, Width = 90 }, disabled: true),
            ["ProgressBar"] = Live(() => new ProgressBar { Value = 60, Size = new Size(220, 22) }),
            ["TrackBar"] = Live(() => new TrackBar { Minimum = 0, Maximum = 10, Value = 5, Size = new Size(220, 45) }, disabled: true),
            ["LinkLabel"] = Live(() => Auto(new LinkLabel { Text = "Visit the documentation" }), disabled: true),
            ["ToolTip"] = Sim(BuildToolTipMock, "Simulated: a ToolTip is drawn by Windows next to the control when the mouse hovers over it."),
            ["ErrorProvider"] = Sim(BuildErrorProviderMock, "Simulated: an ErrorProvider shows a small red icon beside the invalid control; hovering over it shows the message."),

            ["Panel"] = Live(BuildPanel),
            ["GroupBox"] = Live(BuildGroupBox, disabled: true),
            ["TabControl"] = Live(() => BuildTabControl(false)),
            ["TabPage"] = Live(() => BuildTabControl(true),
                caption: "A TabPage is one page of a TabControl. The selected page here is the second one."),
            ["SplitContainer"] = Live(BuildSplitContainer),
            ["FlowLayoutPanel"] = Live(BuildFlowLayoutPanel),
            ["TableLayoutPanel"] = Live(BuildTableLayoutPanel),

            ["MenuStrip"] = Live(BuildMenuStrip),
            ["ContextMenuStrip"] = Sim(BuildContextMenuMock, "Simulated: a ContextMenuStrip pops up at the mouse position when it is shown, usually after a right-click."),
            ["ToolStrip"] = Live(BuildToolStrip),
            ["StatusStrip"] = Live(BuildStatusStrip),
            ["ToolStripButton"] = Live(() => HostedStrip(260, s =>
            {
                s.Items.Add(new ToolStripButton("New"));
                s.Items.Add(new ToolStripButton("Open"));
                s.Items.Add(new ToolStripButton("Save") { Checked = true });
            }), caption: "Three ToolStripButton items on a ToolStrip. The last one has Checked = true."),
            ["ToolStripLabel"] = Live(() => HostedStrip(260, s =>
            {
                s.Items.Add(new ToolStripLabel("Zoom:"));
                s.Items.Add(new ToolStripLabel("Learn more") { IsLink = true });
            }), caption: "A plain ToolStripLabel, and one with IsLink = true."),
            ["ToolStripComboBox"] = Live(() => HostedStrip(260, s =>
            {
                var combo = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                combo.Items.AddRange(new object[] { "50%", "100%", "150%" });
                combo.SelectedIndex = 1;
                s.Items.Add(new ToolStripLabel("Zoom:"));
                s.Items.Add(combo);
            }), caption: "A ToolStripComboBox hosted on a ToolStrip."),

            ["DataGridView"] = Live(BuildDataGridView, disabled: true),
            ["BindingNavigator"] = Live(BuildBindingNavigator),

            ["BindingSource"] = None(),
            ["OpenFileDialog"] = None(),
            ["SaveFileDialog"] = None(),
            ["FolderBrowserDialog"] = None(),
            ["ColorDialog"] = None(),
            ["FontDialog"] = None(),
            ["Timer"] = None(),
            ["BackgroundWorker"] = None(),
            ["NotifyIcon"] = None(),
        };
    }

    /// <summary>The mode a control is previewed in (None = nothing to draw).</summary>
    public PreviewMode GetMode(string controlName) =>
        _specs.TryGetValue(controlName ?? "", out Spec spec) ? spec.Mode : PreviewMode.None;

    /// <summary>
    /// Renders the illustrations for a control. Returns an empty list for components without a user
    /// interface, and also if rendering fails (the guide must keep working on any machine).
    /// The caller owns the returned bitmaps.
    /// </summary>
    public IReadOnlyList<PreviewImage> Render(string controlName)
    {
        var result = new List<PreviewImage>();
        if (!_specs.TryGetValue(controlName ?? "", out Spec spec) || spec.Mode == PreviewMode.None)
            return result;

        try
        {
            string normalCaption = spec.Caption ??
                (spec.Mode == PreviewMode.Live
                    ? "Default appearance (the real " + controlName + " control, rendered live)."
                    : "Simulated illustration.");
            result.Add(new PreviewImage { Image = RenderOne(spec, false), Caption = normalCaption });

            if (spec.ShowDisabled)
            {
                result.Add(new PreviewImage
                {
                    Image = RenderOne(spec, true),
                    Caption = "The same control with Enabled = false."
                });
            }
        }
        catch (Exception)
        {
            foreach (PreviewImage image in result) image.Image?.Dispose();
            result.Clear();
        }
        return result;
    }

    // ------------------------------------------------------------------ rendering

    private static Bitmap RenderOne(Spec spec, bool disabled)
    {
        using (Control content = spec.Build())
        using (var host = new Panel { BackColor = SystemColors.Control })
        {
            if (disabled) content.Enabled = false;

            content.Location = new Point(Pad, Pad);
            host.Size = new Size(content.Width + Pad * 2, content.Height + Pad * 2);
            host.Controls.Add(content);

            // Make sure handles exist before drawing; native controls need them.
            _ = host.Handle;
            host.PerformLayout();

            var bitmap = new Bitmap(host.Width, host.Height);
            try
            {
                host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.Size));
                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }
    }

    // ------------------------------------------------------------------ spec helpers

    private static Spec Live(Func<Control> build, bool disabled = false, string caption = null) =>
        new Spec { Build = build, Mode = PreviewMode.Live, ShowDisabled = disabled, Caption = caption };

    private static Spec Sim(Func<Control> build, string caption) =>
        new Spec { Build = build, Mode = PreviewMode.Simulated, Caption = caption };

    private static Spec None() => new Spec { Mode = PreviewMode.None };

    /// <summary>Gives an auto-sized label its natural size without relying on layout events.</summary>
    private static T Auto<T>(T label) where T : Label
    {
        label.AutoSize = false;
        Size size = TextRenderer.MeasureText(label.Text, label.Font);
        label.Size = new Size(size.Width + 6, size.Height + 4);
        return label;
    }

    // ------------------------------------------------------------------ builders: common controls

    private static Control BuildRichTextBox()
    {
        var box = new RichTextBox { Size = new Size(240, 80), Text = "Rich text supports bold, italic and colors." };
        box.Select(0, 9);
        box.SelectionFont = new Font(box.Font, FontStyle.Bold);
        box.Select(box.TextLength - 7, 6);
        box.SelectionColor = Color.Firebrick;
        box.Select(0, 0);
        return box;
    }

    private static Control BuildCheckBoxes()
    {
        var host = new Panel { Size = new Size(200, 80) };
        host.Controls.Add(new CheckBox { Text = "Remember me", Checked = true, Location = new Point(0, 0), Width = 180 });
        host.Controls.Add(new CheckBox { Text = "Send newsletter", Location = new Point(0, 26), Width = 180 });
        host.Controls.Add(new CheckBox { Text = "Tri-state", ThreeState = true, CheckState = CheckState.Indeterminate, Location = new Point(0, 52), Width = 180 });
        return host;
    }

    private static Control BuildRadioButtons()
    {
        var host = new Panel { Size = new Size(200, 80) };
        host.Controls.Add(new RadioButton { Text = "Small", Location = new Point(0, 0), Width = 180 });
        host.Controls.Add(new RadioButton { Text = "Medium", Checked = true, Location = new Point(0, 26), Width = 180 });
        host.Controls.Add(new RadioButton { Text = "Large", Location = new Point(0, 52), Width = 180 });
        return host;
    }

    private static Control BuildComboBox()
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        combo.Items.AddRange(new object[] { "Red", "Green", "Blue" });
        combo.SelectedIndex = 1;
        return combo;
    }

    private static Control BuildListBox()
    {
        var list = new ListBox { Size = new Size(160, 90) };
        list.Items.AddRange(new object[] { "Apple", "Banana", "Cherry", "Grape", "Mango" });
        list.SelectedIndex = 1;
        return list;
    }

    private static Control BuildCheckedListBox()
    {
        var list = new CheckedListBox { Size = new Size(170, 90), CheckOnClick = true };
        list.Items.Add("Email", true);
        list.Items.Add("SMS", false);
        list.Items.Add("Push", true);
        list.Items.Add("Phone", false);
        return list;
    }

    private static Control BuildPictureBox()
    {
        var picture = new PictureBox
        {
            Size = new Size(160, 100),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        var sample = new Bitmap(160, 100);
        using (Graphics g = Graphics.FromImage(sample))
        using (var brush = new LinearGradientBrush(new Rectangle(0, 0, 160, 100), Color.SteelBlue, Color.Khaki, 45f))
        {
            g.FillRectangle(brush, 0, 0, 160, 100);
            using (var sun = new SolidBrush(Color.Gold))
                g.FillEllipse(sun, 110, 14, 30, 30);
            using (var hill = new SolidBrush(Color.SeaGreen))
                g.FillPie(hill, -40, 55, 200, 120, 180, 180);
        }
        picture.Image = sample;   // disposed with the PictureBox below
        picture.Disposed += (s, e) => sample.Dispose();
        return picture;
    }

    private static Control BuildMonthCalendar()
    {
        var calendar = new MonthCalendar();
        calendar.SetDate(new DateTime(2026, 1, 15));
        return calendar;
    }

    private static Control BuildToolTipMock()
    {
        var host = new Panel { Size = new Size(240, 80) };
        host.Controls.Add(new Button { Text = "Save", Size = new Size(90, 30), Location = new Point(0, 0) });
        var tip = new Label
        {
            Text = "Saves the document",
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.Info,
            ForeColor = SystemColors.InfoText,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(140, 24),
            Location = new Point(40, 44)
        };
        host.Controls.Add(tip);
        return host;
    }

    private static Control BuildErrorProviderMock()
    {
        var host = new Panel { Size = new Size(260, 56) };
        host.Controls.Add(new TextBox { Text = "not-an-email", Width = 170, Location = new Point(0, 4) });
        var icon = new PictureBox
        {
            Size = new Size(16, 16),
            Location = new Point(176, 8),
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        var bitmap = SystemIcons.Error.ToBitmap();
        icon.Image = bitmap;
        icon.Disposed += (s, e) => bitmap.Dispose();
        host.Controls.Add(icon);
        host.Controls.Add(new Label { Text = "Enter a valid email address.", AutoSize = false, Size = new Size(240, 20), Location = new Point(0, 34), ForeColor = Color.Firebrick });
        return host;
    }

    // ------------------------------------------------------------------ builders: containers

    private static Control BuildPanel()
    {
        var panel = new Panel { Size = new Size(240, 100), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        panel.Controls.Add(new Label { Text = "A Panel groups controls.", AutoSize = false, Size = new Size(200, 20), Location = new Point(10, 10), BackColor = Color.White });
        panel.Controls.Add(new Button { Text = "OK", Size = new Size(80, 28), Location = new Point(10, 40) });
        return panel;
    }

    private static Control BuildGroupBox()
    {
        var group = new GroupBox { Text = "Delivery", Size = new Size(200, 100) };
        group.Controls.Add(new RadioButton { Text = "Standard", Checked = true, Location = new Point(14, 24), Width = 160 });
        group.Controls.Add(new RadioButton { Text = "Express", Location = new Point(14, 52), Width = 160 });
        return group;
    }

    private static Control BuildTabControl(bool selectSecond)
    {
        var tabs = new TabControl { Size = new Size(260, 130) };
        var general = new TabPage("General");
        general.Controls.Add(new CheckBox { Text = "Start with Windows", Location = new Point(10, 12), Width = 180 });
        var advanced = new TabPage("Advanced");
        advanced.Controls.Add(new Label { Text = "Advanced options", AutoSize = false, Size = new Size(160, 20), Location = new Point(10, 12) });
        tabs.TabPages.Add(general);
        tabs.TabPages.Add(advanced);
        tabs.TabPages.Add(new TabPage("About"));
        if (selectSecond) tabs.SelectedIndex = 1;
        return tabs;
    }

    private static Control BuildSplitContainer()
    {
        var split = new SplitContainer
        {
            Size = new Size(280, 110),
            BorderStyle = BorderStyle.FixedSingle,
            SplitterDistance = 100
        };
        split.Panel1.BackColor = Color.AliceBlue;
        split.Panel2.BackColor = Color.Honeydew;
        split.Panel1.Controls.Add(new Label { Text = "Panel1", Location = new Point(8, 8), AutoSize = true, BackColor = Color.Transparent });
        split.Panel2.Controls.Add(new Label { Text = "Panel2", Location = new Point(8, 8), AutoSize = true, BackColor = Color.Transparent });
        return split;
    }

    private static Control BuildFlowLayoutPanel()
    {
        var flow = new FlowLayoutPanel { Size = new Size(260, 100), BorderStyle = BorderStyle.FixedSingle, WrapContents = true };
        for (int i = 1; i <= 6; i++)
            flow.Controls.Add(new Button { Text = "Item " + i, Size = new Size(72, 28) });
        return flow;
    }

    private static Control BuildTableLayoutPanel()
    {
        var table = new TableLayoutPanel
        {
            Size = new Size(260, 90),
            ColumnCount = 2,
            RowCount = 2,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        table.Controls.Add(new Label { Text = "Name", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
        table.Controls.Add(new TextBox { Dock = DockStyle.Fill }, 1, 0);
        table.Controls.Add(new Label { Text = "City", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
        table.Controls.Add(new TextBox { Dock = DockStyle.Fill }, 1, 1);
        return table;
    }

    // ------------------------------------------------------------------ builders: menus and toolbars

    /// <summary>A strip docked to the top of a fixed-size host so its width is predictable.</summary>
    private static Control HostedStrip(int width, Action<ToolStrip> fill)
    {
        var host = new Panel { Size = new Size(width, 32) };
        var strip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        fill(strip);
        host.Controls.Add(strip);
        return host;
    }

    private static Control BuildMenuStrip()
    {
        var host = new Panel { Size = new Size(300, 26) };
        var menu = new MenuStrip { Dock = DockStyle.Top };
        var file = new ToolStripMenuItem("File");
        file.DropDownItems.Add("New");
        file.DropDownItems.Add("Open...");
        file.DropDownItems.Add("Exit");
        menu.Items.Add(file);
        menu.Items.Add(new ToolStripMenuItem("Edit"));
        menu.Items.Add(new ToolStripMenuItem("View"));
        menu.Items.Add(new ToolStripMenuItem("Help"));
        host.Controls.Add(menu);
        return host;
    }

    private static Control BuildContextMenuMock()
    {
        var host = new Panel { Size = new Size(150, 120), BackColor = SystemColors.Control };
        var menu = new ToolStrip
        {
            Dock = DockStyle.None,
            LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow,
            GripStyle = ToolStripGripStyle.Hidden,
            AutoSize = false,
            Size = new Size(140, 112),
            Location = new Point(0, 0),
            RenderMode = ToolStripRenderMode.System,
            BackColor = SystemColors.Menu
        };
        menu.Items.Add(new ToolStripButton("Cut") { TextAlign = ContentAlignment.MiddleLeft });
        menu.Items.Add(new ToolStripButton("Copy") { TextAlign = ContentAlignment.MiddleLeft });
        menu.Items.Add(new ToolStripButton("Paste") { TextAlign = ContentAlignment.MiddleLeft });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripButton("Delete") { TextAlign = ContentAlignment.MiddleLeft });
        host.Controls.Add(menu);
        return host;
    }

    private static Control BuildToolStrip()
    {
        return HostedStrip(340, s =>
        {
            s.Items.Add(new ToolStripButton("New"));
            s.Items.Add(new ToolStripButton("Open"));
            s.Items.Add(new ToolStripSeparator());
            s.Items.Add(new ToolStripButton("Save"));
            s.Items.Add(new ToolStripSeparator());
            s.Items.Add(new ToolStripLabel("Zoom:"));
            var combo = new ToolStripComboBox { Width = 70 };
            combo.Items.AddRange(new object[] { "100%", "150%" });
            combo.SelectedIndex = 0;
            s.Items.Add(combo);
        });
    }

    private static Control BuildStatusStrip()
    {
        var host = new Panel { Size = new Size(340, 60) };
        var status = new StatusStrip { Dock = DockStyle.Bottom };
        status.Items.Add(new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft });
        status.Items.Add(new ToolStripProgressBar { Value = 60 });
        status.Items.Add(new ToolStripStatusLabel("Line 12, Col 4"));
        host.Controls.Add(status);
        return host;
    }

    // ------------------------------------------------------------------ builders: data

    private static Control BuildDataGridView()
    {
        var grid = new DataGridView
        {
            Size = new Size(320, 120),
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = true
        };
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Name", "Name");
        grid.Columns.Add("Country", "Country");
        grid.Rows.Add(1, "Amina", "Kenya");
        grid.Rows.Add(2, "Lucas", "Brazil");
        grid.Rows.Add(3, "Mei", "Japan");
        grid.ClearSelection();
        return grid;
    }

    private static Control BuildBindingNavigator()
    {
        var host = new Panel { Size = new Size(420, 28) };
        var source = new BindingSource { DataSource = new List<string> { "A", "B", "C", "D", "E" } };
        var navigator = new BindingNavigator(true) { Dock = DockStyle.Top, BindingSource = source };
        host.Controls.Add(navigator);
        host.Disposed += (s, e) => source.Dispose();
        return host;
    }
}
