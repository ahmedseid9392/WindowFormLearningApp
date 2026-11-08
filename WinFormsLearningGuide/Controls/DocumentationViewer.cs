using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WinFormsLearningGuide.Helpers;
using WinFormsLearningGuide.Models;
using WinFormsLearningGuide.Services;

namespace WinFormsLearningGuide.Controls;

/// <summary>
/// Center area: a header (title, favorite, previous/next) above a scrolling stack of section cards.
/// Sections up to DocumentationSection.CurrentPhase show real content; later ones show placeholders.
/// </summary>
public class DocumentationViewer : UserControl, IThemeable
{
    private readonly Panel _header = new Panel();
    private readonly Label _title = new Label();
    private readonly Label _subtitle = new Label();
    private readonly FlowLayoutPanel _buttons = new FlowLayoutPanel();
    private readonly Button _btnPrevious = new Button();
    private readonly Button _btnNext = new Button();
    private readonly Button _btnFavorite = new Button();
    private readonly ScrollPanel _scroll = new ScrollPanel();
    private readonly List<SectionCard> _cards = new List<SectionCard>();
    private readonly SectionContentBuilder _contentBuilder = new SectionContentBuilder(new ControlPreviewService());
    private bool _layouting;

    public event EventHandler PreviousRequested;
    public event EventHandler NextRequested;
    public event EventHandler FavoriteToggled;

    public DocumentationViewer()
    {
        Dock = DockStyle.Fill;

        _scroll.Dock = DockStyle.Fill;
        foreach (DocumentationSection section in DocumentationSection.All)
        {
            var card = new SectionCard
            {
                Heading = section.Number + ". " + section.Title,
                Badge = section.IsComplete ? "" : "Phase " + section.Phase
            };
            card.MouseEnter += (s, e) => FocusContent(true);
            _cards.Add(card);
            _scroll.Controls.Add(card);
        }
        _scroll.ClientSizeChanged += (s, e) => LayoutCards();
        _scroll.MouseEnter += (s, e) => FocusContent(true);

        _header.Dock = DockStyle.Top;
        _header.Height = 86;
        _header.Tag = "topbar";

        _subtitle.Dock = DockStyle.Top;
        _subtitle.Height = 30;
        _subtitle.Tag = "muted";
        _subtitle.TextAlign = ContentAlignment.TopLeft;
        _subtitle.Padding = new Padding(26, 0, 0, 0);

        _title.Dock = DockStyle.Top;
        _title.Height = 52;
        _title.Font = new Font("Segoe UI Semibold", 20F);
        _title.TextAlign = ContentAlignment.BottomLeft;
        _title.Padding = new Padding(24, 0, 0, 0);

        _buttons.Dock = DockStyle.Right;
        _buttons.AutoSize = true;
        _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttons.WrapContents = false;
        _buttons.FlowDirection = FlowDirection.LeftToRight;
        _buttons.Padding = new Padding(0, 24, 20, 0);
        _buttons.Tag = "topbar";

        ConfigureButton(_btnFavorite, "\u2606  Favorite", 120);
        ConfigureButton(_btnPrevious, "\u2190  Previous", 112);
        ConfigureButton(_btnNext, "Next  \u2192", 100);
        _btnFavorite.Click += (s, e) => FavoriteToggled?.Invoke(this, EventArgs.Empty);
        _btnPrevious.Click += (s, e) => PreviousRequested?.Invoke(this, EventArgs.Empty);
        _btnNext.Click += (s, e) => NextRequested?.Invoke(this, EventArgs.Empty);
        _buttons.Controls.Add(_btnFavorite);
        _buttons.Controls.Add(_btnPrevious);
        _buttons.Controls.Add(_btnNext);

        var border = new Panel { Dock = DockStyle.Bottom, Height = 1, Tag = "border" };

        // Docking order: the last control added is docked first.
        _header.Controls.Add(_subtitle);
        _header.Controls.Add(_title);
        _header.Controls.Add(_buttons);
        _header.Controls.Add(border);

        Controls.Add(_scroll);
        Controls.Add(_header);
    }

    private static void ConfigureButton(Button button, string text, int width)
    {
        button.Text = text;
        button.Size = new Size(width, 34);
        button.Margin = new Padding(0, 0, 8, 0);
    }

    // ------------------------------------------------------------------ public API

    public void ShowControl(ControlDocumentation doc, bool isFavorite, bool hasPrevious, bool hasNext)
    {
        _title.Text = doc.Name;
        _subtitle.Text = doc.Category + "   \u00B7   " + doc.FullName;
        SetFavorite(isFavorite);
        _btnPrevious.Enabled = hasPrevious;
        _btnNext.Enabled = hasNext;

        for (int i = 0; i < _cards.Count; i++)
            _cards[i].SetBlocks(_contentBuilder.Build(doc, DocumentationSection.All[i]));

        _scroll.AutoScrollPosition = Point.Empty;
        LayoutCards();
    }

    public void SetFavorite(bool isFavorite)
    {
        _btnFavorite.Text = isFavorite ? "\u2605  Favorited" : "\u2606  Favorite";
    }

    public void ScrollToSection(int index)
    {
        if (index < 0 || index >= _cards.Count) return;
        int y = _cards[index].Top - _scroll.AutoScrollPosition.Y - ThemeHelper.Scale(this, 12);
        _scroll.AutoScrollPosition = new Point(0, Math.Max(0, y));
        _scroll.Focus();
    }

    public void FocusContent() => _scroll.Focus();

    public void ApplyTheme(Theme theme)
    {
        foreach (SectionCard card in _cards)
            card.Invalidate();
    }

    // ------------------------------------------------------------------ internals

    private void FocusContent(bool onlyIfNotTyping)
    {
        if (!onlyIfNotTyping) { _scroll.Focus(); return; }

        // Do not steal focus from a text box the user is typing in.
        Control focused = FindForm()?.ActiveControl;
        while (focused is ContainerControl container && container.ActiveControl != null)
            focused = container.ActiveControl;
        if (focused is TextBoxBase) return;

        if (!_scroll.Focused) _scroll.Focus();
    }

    private void LayoutCards()
    {
        if (_layouting || _cards.Count == 0) return;
        _layouting = true;
        try
        {
            int savedScroll = -_scroll.AutoScrollPosition.Y;
            _scroll.AutoScrollPosition = Point.Empty;

            for (int pass = 0; pass < 2; pass++)
            {
                int basis = _scroll.ClientSize.Width;
                int pad = ThemeHelper.Scale(this, 24);
                int gap = ThemeHelper.Scale(this, 14);
                int width = Math.Max(ThemeHelper.Scale(this, 260), basis - pad * 2);

                int y = pad;
                foreach (SectionCard card in _cards)
                {
                    card.Width = width;
                    card.Location = new Point(pad, y);
                    y += card.Height + gap;
                }

                _scroll.AutoScrollMinSize = new Size(0, y + pad - gap);
                if (_scroll.ClientSize.Width == basis) break;   // a scrollbar appeared -> lay out once more
            }

            _scroll.VerticalScroll.SmallChange = ThemeHelper.Scale(this, 30);
            _scroll.VerticalScroll.LargeChange = ThemeHelper.Scale(this, 200);
            _scroll.AutoScrollPosition = new Point(0, savedScroll);
        }
        finally
        {
            _layouting = false;
        }
    }
}
