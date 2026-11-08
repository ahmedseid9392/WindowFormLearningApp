using System;
using System.Windows.Forms;
using WinFormsLearningGuide.Helpers;
using WinFormsLearningGuide.Models;
using WinFormsLearningGuide.Services;

namespace WinFormsLearningGuide.Forms;

public partial class MainForm : Form
{
    private readonly DocumentationService _docs;
    private readonly UserDataService _user;
    private readonly SearchService _search;
    private ControlDocumentation _current;

    public MainForm(DocumentationService docs, UserDataService user)
    {
        _docs = docs;
        _user = user;
        _search = new SearchService(docs);

        InitializeComponent();

        toolbox.Initialize(_docs, _user, _search);
        toolbox.ControlSelected += (s, doc) => ShowControl(doc, true);

        docViewer.PreviousRequested += (s, e) => Navigate(-1);
        docViewer.NextRequested += (s, e) => Navigate(1);
        docViewer.FavoriteToggled += (s, e) => ToggleFavorite();

        quickRef.RelatedControlClicked += (s, name) =>
        {
            ControlDocumentation doc = _docs.Find(name);
            if (doc != null) ShowControl(doc, false);
        };
        quickRef.SectionClicked += (s, index) => docViewer.ScrollToSection(index);

        txtSearch.TextChanged += (s, e) => toolbox.ApplyFilter(txtSearch.Text);
        txtSearch.KeyDown += TxtSearch_KeyDown;
        btnTheme.Click += (s, e) => ToggleTheme();

        ThemeHelper.SetTheme(_user.Data.DarkTheme);
        ApplyTheme();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ThemeHelper.ApplyTitleBar(this);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ConfigureSplitters();

        ControlDocumentation start = _docs.Find(_user.Data.LastControl)
                                     ?? (_docs.All.Count > 0 ? _docs.All[0] : null);
        if (start != null) ShowControl(start, false);
    }

    private void ConfigureSplitters()
    {
        try
        {
            splitOuter.SplitterDistance = ThemeHelper.Scale(this, 300);
            splitOuter.Panel1MinSize = ThemeHelper.Scale(this, 220);
            splitOuter.Panel2MinSize = ThemeHelper.Scale(this, 500);

            splitInner.Panel1MinSize = ThemeHelper.Scale(this, 360);
            splitInner.Panel2MinSize = ThemeHelper.Scale(this, 220);
            splitInner.SplitterDistance = Math.Max(splitInner.Panel1MinSize,
                splitInner.Width - ThemeHelper.Scale(this, 300));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            // Keep the default splitter positions if the window is unusually small.
        }
    }

    // ------------------------------------------------------------------ navigation

    private void ShowControl(ControlDocumentation doc, bool fromToolbox)
    {
        if (doc == null) return;
        _current = doc;

        _user.MarkViewed(doc.Name);
        _user.Data.LastControl = doc.Name;
        _user.Save();

        int index = _docs.IndexOf(doc);
        docViewer.ShowControl(doc, _user.IsFavorite(doc.Name), index > 0, index < _docs.All.Count - 1);
        quickRef.ShowControl(doc, DocumentationSection.All);

        if (!fromToolbox)
        {
            toolbox.RefreshLists();
            toolbox.SelectControl(doc);
        }

        UpdateStatus();
    }

    private void Navigate(int delta)
    {
        if (_current == null) return;
        int index = _docs.IndexOf(_current) + delta;
        if (index < 0 || index >= _docs.All.Count) return;
        ShowControl(_docs.All[index], false);
    }

    private void ToggleFavorite()
    {
        if (_current == null) return;
        bool favorite = _user.ToggleFavorite(_current.Name);
        docViewer.SetFavorite(favorite);
        toolbox.RefreshLists();
        toolbox.SelectControl(_current);
    }

    private void UpdateStatus()
    {
        lblStatus.Text = _current == null ? "" : _current.Name + "   \u00B7   " + _current.Category;
        lblProgress.Text = "Viewed " + _user.ViewedCount + " of " + _docs.All.Count + " controls";
    }

    // ------------------------------------------------------------------ search box

    private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            toolbox.SelectFirstResult();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            txtSearch.Clear();
            docViewer.FocusContent();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F))
        {
            txtSearch.Focus();
            txtSearch.SelectAll();
            return true;
        }
        if (keyData == (Keys.Alt | Keys.Right)) { Navigate(1); return true; }
        if (keyData == (Keys.Alt | Keys.Left)) { Navigate(-1); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ------------------------------------------------------------------ theme

    private void ToggleTheme()
    {
        ThemeHelper.SetTheme(!ThemeHelper.Current.IsDark);
        _user.Data.DarkTheme = ThemeHelper.Current.IsDark;
        _user.Save();
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        SuspendLayout();
        ThemeHelper.Apply(this);
        btnTheme.Text = ThemeHelper.Current.IsDark ? "Switch to Light" : "Switch to Dark";
        ThemeHelper.ApplyTitleBar(this);
        ResumeLayout(true);
        Invalidate(true);
    }
}
