using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsLearningGuide.Helpers;
using WinFormsLearningGuide.Models;
using WinFormsLearningGuide.Services;

namespace WinFormsLearningGuide.Controls;

public enum ToolboxMode
{
    Categories,
    Alphabetical,
    Recent,
    Favorites
}

/// <summary>
/// Left sidebar: expandable categories, A-Z list, recently viewed, favorites and a filter.
/// </summary>
public class ToolboxNavigation : UserControl, IThemeable
{
    private sealed class GroupInfo
    {
        public string Title;
        public int Count;
    }

    /// <summary>TreeView that treats a double-click like a normal click, so groups toggle once per click.</summary>
    private sealed class FlatTreeView : TreeView
    {
        public FlatTreeView()
        {
            DoubleBuffered = true;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0203) // WM_LBUTTONDBLCLK -> WM_LBUTTONDOWN
                m.Msg = 0x0201;
            base.WndProc(ref m);
        }
    }

    private DocumentationService _docs;
    private UserDataService _user;
    private SearchService _search;

    private readonly Label _caption = new Label();
    private readonly TableLayoutPanel _tabs = new TableLayoutPanel();
    private readonly Button[] _tabButtons = new Button[4];
    private readonly FlatTreeView _tree = new FlatTreeView();
    private readonly HashSet<string> _expanded = new HashSet<string>();

    private HashSet<string> _matches;           // null = no filter active
    private ToolboxMode _mode = ToolboxMode.Categories;
    private string _selectedName = "";
    private bool _suppress;

    public event EventHandler<ControlDocumentation> ControlSelected;

    public ToolboxNavigation()
    {
        Dock = DockStyle.Fill;
        Tag = "sidebar";

        _tree.Dock = DockStyle.Fill;
        _tree.BorderStyle = BorderStyle.None;
        _tree.ShowLines = false;
        _tree.ShowPlusMinus = false;
        _tree.ShowRootLines = false;
        _tree.HideSelection = false;
        _tree.FullRowSelect = true;
        _tree.Indent = 16;
        _tree.DrawMode = TreeViewDrawMode.OwnerDrawAll;
        _tree.DrawNode += Tree_DrawNode;
        _tree.AfterSelect += Tree_AfterSelect;
        _tree.AfterExpand += Tree_AfterExpandCollapse;
        _tree.AfterCollapse += Tree_AfterExpandCollapse;
        _tree.NodeMouseClick += Tree_NodeMouseClick;
        _tree.KeyDown += Tree_KeyDown;
        _tree.HandleCreated += (s, e) =>
        {
            _tree.ItemHeight = ThemeHelper.Scale(_tree, 26);
            _tree.Indent = ThemeHelper.Scale(_tree, 16);
            ThemeHelper.ApplyNativeTheme(_tree, ThemeHelper.Current);
        };

        string[] labels = { "Categories", "A\u2013Z", "Recent", "Favorites" };
        _tabs.Dock = DockStyle.Top;
        _tabs.Height = 40;
        _tabs.Padding = new Padding(8, 0, 8, 8);
        _tabs.Tag = "sidebar";
        _tabs.ColumnCount = 4;
        _tabs.RowCount = 1;
        for (int i = 0; i < 4; i++)
        {
            _tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            int index = i;
            var button = new Button
            {
                Text = labels[i],
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 2, 0),
                Font = new Font("Segoe UI", 8.5F)
            };
            button.Click += (s, e) => SetMode((ToolboxMode)index);
            _tabButtons[i] = button;
            _tabs.Controls.Add(button, i, 0);
        }
        _tabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _caption.Dock = DockStyle.Top;
        _caption.Height = 36;
        _caption.Text = "TOOLBOX";
        _caption.Tag = "caption";
        _caption.TextAlign = ContentAlignment.MiddleLeft;
        _caption.Padding = new Padding(14, 4, 0, 0);
        _caption.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

        // Fill first, then Top controls (the last added is docked first).
        Controls.Add(_tree);
        Controls.Add(_tabs);
        Controls.Add(_caption);
    }

    public void Initialize(DocumentationService docs, UserDataService user, SearchService search)
    {
        _docs = docs;
        _user = user;
        _search = search;
        if (_docs.Categories.Count > 0)
            _expanded.Add(_docs.Categories[0]);
        Rebuild();
    }

    public ToolboxMode Mode => _mode;

    // ------------------------------------------------------------------ public API

    public void ApplyFilter(string text)
    {
        text = (text ?? "").Trim();
        _matches = text.Length == 0
            ? null
            : new HashSet<string>(_search.Search(text).Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
        Rebuild();
    }

    /// <summary>Selects (and raises the event for) the first control currently shown.</summary>
    public bool SelectFirstResult()
    {
        foreach (TreeNode node in _tree.Nodes)
        {
            if (node.Tag is ControlDocumentation)
            {
                _tree.SelectedNode = node;
                return true;
            }
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Tag is ControlDocumentation)
                {
                    _tree.SelectedNode = child;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Highlights a control without raising ControlSelected.</summary>
    public void SelectControl(ControlDocumentation doc)
    {
        if (doc == null) return;
        _selectedName = doc.Name;
        TreeNode node = FindLeaf(doc.Name);

        _suppress = true;
        try
        {
            if (node == null)
            {
                _tree.SelectedNode = null;
            }
            else
            {
                if (node.Parent != null && !node.Parent.IsExpanded)
                {
                    node.Parent.Expand();
                    _expanded.Add(node.Parent.Name);
                }
                _tree.SelectedNode = node;
                node.EnsureVisible();
            }
        }
        finally
        {
            _suppress = false;
        }
    }

    /// <summary>Rebuilds the list (call after favorites or recent items change).</summary>
    public void RefreshLists() => Rebuild();

    // ------------------------------------------------------------------ building the tree

    private void SetMode(ToolboxMode mode)
    {
        _mode = mode;
        UpdateTabStyles(ThemeHelper.Current);
        Rebuild();
    }

    private void Rebuild()
    {
        if (_docs == null) return;

        _suppress = true;
        _tree.BeginUpdate();
        try
        {
            _tree.Nodes.Clear();
            List<ControlDocumentation> items = GetItems();
            bool filtering = _matches != null;

            switch (_mode)
            {
                case ToolboxMode.Categories:
                    foreach (string category in _docs.Categories)
                    {
                        List<ControlDocumentation> inCategory = items.Where(d => d.Category == category).ToList();
                        if (inCategory.Count > 0)
                            AddGroup(category, category, inCategory, filtering);
                    }
                    break;

                case ToolboxMode.Alphabetical:
                    foreach (var group in items.GroupBy(d => char.ToUpperInvariant(d.Name[0])).OrderBy(g => g.Key))
                    {
                        List<ControlDocumentation> sorted =
                            group.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList();
                        AddGroup("az:" + group.Key, group.Key.ToString(), sorted, filtering);
                    }
                    break;

                default:
                    foreach (ControlDocumentation doc in items)
                        _tree.Nodes.Add(CreateLeaf(doc));
                    break;
            }

            if (_tree.Nodes.Count == 0)
                _tree.Nodes.Add(new TreeNode(EmptyMessage()) { Tag = "placeholder" });

            TreeNode selected = FindLeaf(_selectedName);
            if (selected != null)
            {
                if (selected.Parent != null)
                {
                    selected.Parent.Expand();
                    _expanded.Add(selected.Parent.Name);
                }
                _tree.SelectedNode = selected;
                selected.EnsureVisible();
            }
        }
        finally
        {
            _tree.EndUpdate();
            _suppress = false;
        }
    }

    private string EmptyMessage()
    {
        if (_matches != null) return "No controls match your search.";
        if (_mode == ToolboxMode.Recent) return "No recently viewed controls yet.";
        if (_mode == ToolboxMode.Favorites) return "No favorites yet. Use the Favorite button on a page.";
        return "Nothing to show.";
    }

    private List<ControlDocumentation> GetItems()
    {
        IEnumerable<ControlDocumentation> source;
        switch (_mode)
        {
            case ToolboxMode.Recent:
                source = _user.Data.Recent.Select(n => _docs.Find(n)).Where(d => d != null);
                break;
            case ToolboxMode.Favorites:
                source = _user.Data.Favorites.Select(n => _docs.Find(n)).Where(d => d != null);
                break;
            default:
                source = _docs.All;
                break;
        }

        if (_matches != null)
            source = source.Where(d => _matches.Contains(d.Name));

        return source.ToList();
    }

    private void AddGroup(string key, string title, List<ControlDocumentation> items, bool filtering)
    {
        var node = new TreeNode { Name = key, Tag = new GroupInfo { Title = title, Count = items.Count } };
        foreach (ControlDocumentation doc in items)
            node.Nodes.Add(CreateLeaf(doc));

        _tree.Nodes.Add(node);
        if (filtering || _expanded.Contains(key))
            node.Expand();
        UpdateGroupText(node);
    }

    private TreeNode CreateLeaf(ControlDocumentation doc)
    {
        string text = _user.IsFavorite(doc.Name) ? "\u2605 " + doc.Name : doc.Name;
        return new TreeNode(text) { Name = doc.Name, Tag = doc };
    }

    private static void UpdateGroupText(TreeNode node)
    {
        if (node.Tag is GroupInfo info)
            node.Text = (node.IsExpanded ? "\u25BE" : "\u25B8") + "  " + info.Title + "  (" + info.Count + ")";
    }

    private TreeNode FindLeaf(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (TreeNode node in _tree.Nodes)
        {
            if (IsLeafFor(node, name)) return node;
            foreach (TreeNode child in node.Nodes)
                if (IsLeafFor(child, name)) return child;
        }
        return null;
    }

    private static bool IsLeafFor(TreeNode node, string name) =>
        node.Tag is ControlDocumentation doc && string.Equals(doc.Name, name, StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------ tree events

    private void Tree_AfterSelect(object sender, TreeViewEventArgs e)
    {
        if (_suppress) return;
        if (e.Node?.Tag is ControlDocumentation doc)
        {
            _selectedName = doc.Name;
            ControlSelected?.Invoke(this, doc);
        }
    }

    private void Tree_AfterExpandCollapse(object sender, TreeViewEventArgs e)
    {
        UpdateGroupText(e.Node);
        if (_suppress || _matches != null) return;
        if (e.Node.IsExpanded) _expanded.Add(e.Node.Name);
        else _expanded.Remove(e.Node.Name);
    }

    private void Tree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Node.Tag is GroupInfo)
            e.Node.Toggle();
    }

    private void Tree_KeyDown(object sender, KeyEventArgs e)
    {
        if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) && _tree.SelectedNode?.Tag is GroupInfo)
        {
            _tree.SelectedNode.Toggle();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void Tree_DrawNode(object sender, DrawTreeNodeEventArgs e)
    {
        if (e.Node == null || e.Bounds.IsEmpty) return;

        Theme t = ThemeHelper.Current;
        bool selected = (e.State & TreeNodeStates.Selected) != 0;
        bool isLeaf = e.Node.Tag is ControlDocumentation;
        bool isGroup = e.Node.Tag is GroupInfo;

        Color back = _tree.BackColor;
        Color fore = t.Text;
        if (selected && isLeaf) { back = t.Selection; fore = t.SelectionText; }
        else if (selected && isGroup && _tree.Focused) { back = t.Hover; }
        if (isGroup) fore = t.Accent;
        else if (!isLeaf) fore = t.TextMuted;

        var row = new Rectangle(0, e.Bounds.Y, _tree.ClientSize.Width, e.Bounds.Height);
        using (var brush = new SolidBrush(back))
            e.Graphics.FillRectangle(brush, row);

        if (selected && isLeaf)
        {
            using (var accent = new SolidBrush(t.Accent))
                e.Graphics.FillRectangle(accent, 0, row.Y, ThemeHelper.Scale(_tree, 3), row.Height);
        }

        int x = ThemeHelper.Scale(_tree, 14) + e.Node.Level * _tree.Indent;
        var textRect = new Rectangle(x, row.Y, Math.Max(1, row.Width - x), row.Height);
        TextRenderer.DrawText(e.Graphics, e.Node.Text, _tree.Font, textRect, fore,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    // ------------------------------------------------------------------ theming

    public void ApplyTheme(Theme theme)
    {
        _tree.BackColor = theme.Sidebar;
        _tree.ForeColor = theme.Text;
        ThemeHelper.ApplyNativeTheme(_tree, theme);
        UpdateTabStyles(theme);
        _tree.Invalidate();
    }

    private void UpdateTabStyles(Theme t)
    {
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            bool active = i == (int)_mode;
            Button b = _tabButtons[i];
            b.BackColor = active ? t.Selection : t.Sidebar;
            b.ForeColor = active ? t.SelectionText : t.TextMuted;
            b.FlatAppearance.BorderColor = active ? t.Accent : t.Border;
        }
    }
}
