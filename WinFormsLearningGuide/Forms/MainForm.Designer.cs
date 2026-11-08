using System.Drawing;
using System.Windows.Forms;
using WinFormsLearningGuide.Controls;

namespace WinFormsLearningGuide.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel topBar;
    private Panel topBorder;
    private Panel searchHost;
    private TextBox txtSearch;
    private Label lblTitle;
    private Panel themeHost;
    private Button btnTheme;

    private SplitContainer splitOuter;
    private SplitContainer splitInner;
    private ToolboxNavigation toolbox;
    private DocumentationViewer docViewer;
    private QuickReferencePanel quickRef;

    private Panel statusBar;
    private Panel statusBorder;
    private Label lblStatus;
    private Label lblProgress;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        topBar = new Panel();
        topBorder = new Panel();
        searchHost = new Panel();
        txtSearch = new TextBox();
        lblTitle = new Label();
        themeHost = new Panel();
        btnTheme = new Button();
        splitOuter = new SplitContainer();
        splitInner = new SplitContainer();
        toolbox = new ToolboxNavigation();
        docViewer = new DocumentationViewer();
        quickRef = new QuickReferencePanel();
        statusBar = new Panel();
        statusBorder = new Panel();
        lblStatus = new Label();
        lblProgress = new Label();

        ((System.ComponentModel.ISupportInitialize)splitOuter).BeginInit();
        ((System.ComponentModel.ISupportInitialize)splitInner).BeginInit();
        SuspendLayout();

        // ---- top bar -------------------------------------------------------
        txtSearch.Dock = DockStyle.Top;
        txtSearch.Font = new Font("Segoe UI", 10.5F);
        txtSearch.PlaceholderText = "Search controls, properties and events   (Ctrl+F)";
        txtSearch.Name = "txtSearch";

        searchHost.Dock = DockStyle.Fill;
        searchHost.Padding = new Padding(16, 14, 16, 0);
        searchHost.Controls.Add(txtSearch);

        lblTitle.Dock = DockStyle.Left;
        lblTitle.AutoSize = false;
        lblTitle.Width = 280;
        lblTitle.Text = "WinForms Learning Guide";
        lblTitle.Font = new Font("Segoe UI Semibold", 12.5F);
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        lblTitle.Padding = new Padding(16, 0, 0, 0);

        btnTheme.Dock = DockStyle.Fill;
        btnTheme.Text = "Switch to Light";
        btnTheme.Name = "btnTheme";

        themeHost.Dock = DockStyle.Right;
        themeHost.Width = 160;
        themeHost.Padding = new Padding(8, 11, 16, 11);
        themeHost.Controls.Add(btnTheme);

        topBorder.Dock = DockStyle.Bottom;
        topBorder.Height = 1;
        topBorder.Tag = "border";

        topBar.Dock = DockStyle.Top;
        topBar.Height = 56;
        topBar.Tag = "topbar";
        // Docking order: the last control added is docked first.
        topBar.Controls.Add(searchHost);
        topBar.Controls.Add(lblTitle);
        topBar.Controls.Add(themeHost);
        topBar.Controls.Add(topBorder);

        // ---- status bar ----------------------------------------------------
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Padding = new Padding(14, 0, 0, 0);
        lblStatus.Tag = "muted";

        lblProgress.Dock = DockStyle.Right;
        lblProgress.AutoSize = false;
        lblProgress.Width = 280;
        lblProgress.TextAlign = ContentAlignment.MiddleRight;
        lblProgress.Padding = new Padding(0, 0, 14, 0);
        lblProgress.Tag = "muted";

        statusBorder.Dock = DockStyle.Top;
        statusBorder.Height = 1;
        statusBorder.Tag = "border";

        statusBar.Dock = DockStyle.Bottom;
        statusBar.Height = 28;
        statusBar.Tag = "topbar";
        statusBar.Controls.Add(lblStatus);
        statusBar.Controls.Add(lblProgress);
        statusBar.Controls.Add(statusBorder);

        // ---- three resizable panels ---------------------------------------
        docViewer.Dock = DockStyle.Fill;
        quickRef.Dock = DockStyle.Fill;
        toolbox.Dock = DockStyle.Fill;

        splitInner.Dock = DockStyle.Fill;
        splitInner.FixedPanel = FixedPanel.Panel2;
        splitInner.SplitterWidth = 4;
        splitInner.Panel1.Tag = "content";
        splitInner.Panel2.Tag = "sidebar";
        splitInner.Panel1.Controls.Add(docViewer);
        splitInner.Panel2.Controls.Add(quickRef);

        splitOuter.Dock = DockStyle.Fill;
        splitOuter.FixedPanel = FixedPanel.Panel1;
        splitOuter.SplitterWidth = 4;
        splitOuter.Panel1.Tag = "sidebar";
        splitOuter.Panel2.Tag = "content";
        splitOuter.Panel1.Controls.Add(toolbox);
        splitOuter.Panel2.Controls.Add(splitInner);

        // ---- form ----------------------------------------------------------
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1366, 820);
        MinimumSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Text = "Windows Forms Learning & Reference Guide";
        Name = "MainForm";

        // Fill control first, then bottom/top bars (the last added is docked first).
        Controls.Add(splitOuter);
        Controls.Add(statusBar);
        Controls.Add(topBar);

        ((System.ComponentModel.ISupportInitialize)splitOuter).EndInit();
        ((System.ComponentModel.ISupportInitialize)splitInner).EndInit();
        ResumeLayout(false);
    }
}
