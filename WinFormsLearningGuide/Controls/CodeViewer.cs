using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinFormsLearningGuide.Helpers;
using WinFormsLearningGuide.Models;
using WinFormsLearningGuide.Services;

namespace WinFormsLearningGuide.Controls;

/// <summary>
/// The source code viewer (Phase 5) for section 6 of a control page. It shows the complete example program with
/// syntax highlighting and line numbers, a step-by-step walkthrough that explains the code, and alternative
/// implementations. Buttons copy the code to the clipboard or save it as a .cs file.
/// The control is custom painted and sizes itself to its content, so it can sit in the scrolling stack of cards.
/// </summary>
public class CodeViewer : Control, IThemeable
{
    private enum ViewTab { Code, Walkthrough, Alternatives }
    private enum Role { Normal, Muted, Accent }
    private enum ItemKind { Text, Code }

    private sealed class Item
    {
        public ItemKind Kind;
        public string Text;
        public Font Font;
        public Role Role;
        public string[] Lines;
        public List<CodeToken>[] Tokens;
        public int FirstLine;
        public int Gutter;
        public int GapAfter;
        public int Y;
        public int Height;
    }

    private const string HeadingText = "6. Complete C# Example";

    private const TextFormatFlags TextFlags =
        TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.Left |
        TextFormatFlags.Top | TextFormatFlags.NoPadding;

    private const TextFormatFlags RunFlags =
        TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding;

    private readonly Font _headFont = new Font("Segoe UI Semibold", 12.5F);
    private readonly Font _bodyFont = new Font("Segoe UI", 10F);
    private readonly Font _boldFont = new Font("Segoe UI Semibold", 10F);
    private readonly Font _codeFont = new Font("Consolas", 10F);

    private readonly Button _tabCode = new Button();
    private readonly Button _tabWalkthrough = new Button();
    private readonly Button _tabAlternatives = new Button();
    private readonly Button _btnCopy = new Button();
    private readonly Button _btnDownload = new Button();
    private readonly HScrollBar _hScroll = new HScrollBar();
    private readonly System.Windows.Forms.Timer _feedbackTimer = new System.Windows.Forms.Timer();
    private readonly CodeExampleService _service = new CodeExampleService();
    private readonly List<Item> _items = new List<Item>();

    private ControlDocumentation _doc;
    private ControlExample _example;
    private ViewTab _tab = ViewTab.Code;
    private float _charWidth = 8F;
    private int _lineHeight = 17;
    private int _hOffset;
    private int _hMax;

    /// <summary>Raised when the viewer's height changed (for example after switching tabs).</summary>
    public event EventHandler LayoutChanged;

    public CodeViewer()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        Font = _bodyFont;
        Height = 140;

        MeasureCodeFont();

        ConfigureButton(_tabCode, "Code", () => SelectTab(ViewTab.Code));
        ConfigureButton(_tabWalkthrough, "Explain line by line", () => SelectTab(ViewTab.Walkthrough));
        ConfigureButton(_tabAlternatives, "Alternatives", () => SelectTab(ViewTab.Alternatives));
        ConfigureButton(_btnCopy, "Copy code", CopyCode);
        ConfigureButton(_btnDownload, "Download .cs", DownloadCode);

        _hScroll.Visible = false;
        _hScroll.Scroll += (s, e) =>
        {
            _hOffset = _hScroll.Value;
            Invalidate();
        };

        _feedbackTimer.Interval = 1500;
        _feedbackTimer.Tick += (s, e) =>
        {
            _feedbackTimer.Stop();
            _btnCopy.Text = "Copy code";
        };

        Controls.Add(_tabCode);
        Controls.Add(_tabWalkthrough);
        Controls.Add(_tabAlternatives);
        Controls.Add(_btnCopy);
        Controls.Add(_btnDownload);
        Controls.Add(_hScroll);
        StyleTabs();
    }

    // ------------------------------------------------------------------ public API

    /// <summary>Shows the complete example of a control (or a "no example" message when it has none).</summary>
    public void ShowExample(ControlDocumentation doc)
    {
        _doc = doc;
        _example = doc?.Example;
        _tab = ViewTab.Code;
        _hOffset = 0;
        StyleTabs();
        RebuildItems();
        Invalidate();
    }

    public void ApplyTheme(Theme theme)
    {
        StyleTabs();
        ThemeHelper.ApplyNativeTheme(_hScroll, theme);
        Invalidate();
    }

    // ------------------------------------------------------------------ buttons

    private void ConfigureButton(Button button, string text, Action onClick)
    {
        button.Text = text;
        button.TabStop = false;
        button.Click += (s, e) =>
        {
            onClick();
            Parent?.Focus();   // hand the keyboard focus (and the mouse wheel) back to the scrolling page
        };
    }

    private void StyleTabs()
    {
        _tabCode.Tag = _tab == ViewTab.Code ? "accent" : null;
        _tabWalkthrough.Tag = _tab == ViewTab.Walkthrough ? "accent" : null;
        _tabAlternatives.Tag = _tab == ViewTab.Alternatives ? "accent" : null;
        ThemeHelper.Apply(_tabCode);
        ThemeHelper.Apply(_tabWalkthrough);
        ThemeHelper.Apply(_tabAlternatives);
        ThemeHelper.Apply(_btnCopy);
        ThemeHelper.Apply(_btnDownload);
    }

    private void SelectTab(ViewTab tab)
    {
        if (_tab == tab) return;
        _tab = tab;
        _hOffset = 0;
        StyleTabs();
        RebuildItems();
        Invalidate();
    }

    private void CopyCode()
    {
        if (_example == null) return;
        try
        {
            Clipboard.SetText(_service.GetText(_example));
            _btnCopy.Text = "Copied!";
            _feedbackTimer.Stop();
            _feedbackTimer.Start();
        }
        catch (ExternalException)
        {
            MessageBox.Show(FindForm(), "The clipboard is in use by another program. Please try again.",
                "Copy code", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void DownloadCode()
    {
        if (_example == null || _doc == null) return;
        using (var dialog = new SaveFileDialog())
        {
            dialog.Title = "Save the example";
            dialog.Filter = "C# file (*.cs)|*.cs|All files (*.*)|*.*";
            dialog.FileName = _service.GetFileName(_doc);
            dialog.DefaultExt = "cs";
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                _service.Save(dialog.FileName, _example);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(FindForm(), "The file could not be saved:\n\n" + ex.Message,
                    "Download", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ------------------------------------------------------------------ layout

    private int Scale(int value) => ThemeHelper.Scale(this, value);

    private void MeasureCodeFont()
    {
        Size big = new Size(int.MaxValue, int.MaxValue);
        int width = TextRenderer.MeasureText(new string('M', 100), _codeFont, big,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
        _charWidth = Math.Max(1F, width / 100F);
        _lineHeight = _codeFont.Height + 1;
    }

    private Item TextItem(string text, Font font, Role role, int gapAfter) =>
        new Item { Kind = ItemKind.Text, Text = text, Font = font, Role = role, GapAfter = gapAfter };

    private Item CodeItem(string code, int firstLine, int gapAfter)
    {
        string[] lines = (code ?? "").Replace("\r", "").Split('\n');
        var tokens = new List<CodeToken>[lines.Length];
        for (int i = 0; i < lines.Length; i++)
            tokens[i] = CSharpHighlighter.Tokenize(lines[i]);

        int digits = (firstLine + lines.Length - 1).ToString().Length;
        return new Item
        {
            Kind = ItemKind.Code,
            Lines = lines,
            Tokens = tokens,
            FirstLine = firstLine,
            Gutter = (int)Math.Ceiling(digits * _charWidth) + Scale(20),
            GapAfter = gapAfter
        };
    }

    private List<Item> BuildContent()
    {
        var list = new List<Item>();
        int small = Scale(8);
        int large = Scale(16);

        if (_example == null || _example.Segments.Count == 0)
        {
            string name = _doc != null ? _doc.Name : "this control";
            list.Add(TextItem("No complete example is available for " + name + ".", _bodyFont, Role.Muted, large));
            return list;
        }

        switch (_tab)
        {
            case ViewTab.Code:
                list.Add(TextItem(_example.Title + ": a complete program that creates and configures the control in code.",
                    _bodyFont, Role.Normal, small));
                list.Add(TextItem("To run it, create a new Windows Forms App project, delete Form1 and Program.cs, add this file " +
                    "(or use Download) and press F5. Warnings about nullable fields can be ignored.",
                    _bodyFont, Role.Muted, large));
                list.Add(CodeItem(_example.FullCode, 1, large));
                break;

            case ViewTab.Walkthrough:
                list.Add(TextItem("Each step shows part of the program and explains it. The line numbers match the Code tab.",
                    _bodyFont, Role.Muted, large));
                int line = 1;
                foreach (ExampleSegment segment in _example.Segments)
                {
                    int count = ControlExample.CountLines(segment.Code);
                    if (!string.IsNullOrWhiteSpace(segment.Explain) && !string.IsNullOrWhiteSpace(segment.Code))
                    {
                        list.Add(CodeItem(segment.Code, line, small));
                        list.Add(TextItem(segment.Explain, _bodyFont, Role.Normal, large));
                    }
                    line += count;
                }
                break;

            case ViewTab.Alternatives:
                if (_example.Alternatives.Count == 0)
                {
                    list.Add(TextItem("There are no alternative implementations for this control.", _bodyFont, Role.Muted, large));
                    break;
                }
                foreach (AlternativeExample alt in _example.Alternatives)
                {
                    list.Add(TextItem(alt.Title, _boldFont, Role.Accent, Scale(2)));
                    list.Add(TextItem(alt.Description, _bodyFont, Role.Normal, small));
                    list.Add(CodeItem(alt.Code, 1, large));
                }
                break;
        }

        return list;
    }

    private void RebuildItems()
    {
        _items.Clear();
        if (Width <= 0) return;

        int padX = Scale(22);
        int contentWidth = Math.Max(Scale(160), Width - padX * 2);
        int codePad = Scale(10);

        int top = Scale(14) + _headFont.Height + Scale(12);
        int toolbarBottom = PositionToolbar(padX, top);
        int y = toolbarBottom + Scale(14);

        List<Item> content = BuildContent();

        // Horizontal overflow: how much wider than the block is the widest line of code?
        int overflow = 0;
        foreach (Item item in content)
        {
            if (item.Kind != ItemKind.Code) continue;
            int maxChars = 0;
            foreach (string codeLine in item.Lines)
                maxChars = Math.Max(maxChars, codeLine.Length);
            int needed = item.Gutter + codePad * 2 + (int)Math.Ceiling(maxChars * _charWidth);
            overflow = Math.Max(overflow, needed - contentWidth);
        }

        if (overflow > 0)
        {
            _hScroll.SetBounds(padX, y, contentWidth, SystemInformation.HorizontalScrollBarHeight);
            _hScroll.Minimum = 0;
            _hScroll.SmallChange = Math.Max(1, (int)_charWidth * 4);
            _hScroll.LargeChange = Math.Max(1, Math.Min(Scale(160), overflow));
            _hScroll.Maximum = overflow + _hScroll.LargeChange - 1;
            _hMax = overflow;
            _hOffset = Math.Min(_hOffset, _hMax);
            _hScroll.Value = _hOffset;
            _hScroll.Visible = true;
            y += _hScroll.Height + Scale(10);
        }
        else
        {
            _hScroll.Visible = false;
            _hMax = 0;
            _hOffset = 0;
        }

        int pad = Scale(8);
        foreach (Item item in content)
        {
            item.Y = y;
            if (item.Kind == ItemKind.Text)
            {
                item.Height = string.IsNullOrEmpty(item.Text)
                    ? 0
                    : TextRenderer.MeasureText(item.Text, item.Font, new Size(contentWidth, int.MaxValue), TextFlags).Height;
            }
            else
            {
                item.Height = item.Lines.Length * _lineHeight + pad * 2;
            }
            y += item.Height + item.GapAfter;
            _items.Add(item);
        }

        int newHeight = y + Scale(6);
        if (newHeight != Height)
        {
            Height = newHeight;      // may call OnResize -> RebuildItems again, which ends with the same height
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Places the tab buttons and the Copy/Download buttons. Returns the bottom edge of the toolbar.</summary>
    private int PositionToolbar(int padX, int top)
    {
        int h = Scale(30);
        int gap = Scale(6);
        int x = padX;

        foreach (Button tab in new[] { _tabCode, _tabWalkthrough, _tabAlternatives })
        {
            int w = tab == _tabWalkthrough ? Scale(150) : Scale(100);
            tab.SetBounds(x, top, w, h);
            x += w + gap;
        }

        int downloadWidth = Scale(120);
        int copyWidth = Scale(100);
        bool sameRow = Width >= x + downloadWidth + copyWidth + gap + padX;
        int rowY = sameRow ? top : top + h + gap;
        int right = Width - padX;
        _btnDownload.SetBounds(right - downloadWidth, rowY, downloadWidth, h);
        _btnCopy.SetBounds(right - downloadWidth - gap - copyWidth, rowY, copyWidth, h);

        return rowY + h;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RebuildItems();
    }

    // ------------------------------------------------------------------ painting

    protected override void OnPaint(PaintEventArgs e)
    {
        Theme t = ThemeHelper.Current;
        Graphics g = e.Graphics;
        int padX = Scale(22);

        using (var bg = new SolidBrush(t.Surface))
            g.FillRectangle(bg, ClientRectangle);
        using (var pen = new Pen(t.Border))
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        using (var accent = new SolidBrush(t.Accent))
            g.FillRectangle(accent, 0, 0, Scale(4), Height);

        var headRect = new Rectangle(padX, Scale(14), Width - padX * 2, _headFont.Height);
        TextRenderer.DrawText(g, HeadingText, _headFont, headRect, t.Text,
            TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        int clipTop = e.ClipRectangle.Top;
        int clipBottom = e.ClipRectangle.Bottom;
        foreach (Item item in _items)
        {
            if (item.Y + item.Height < clipTop || item.Y > clipBottom) continue;

            if (item.Kind == ItemKind.Text)
            {
                if (item.Height == 0) continue;
                var rect = new Rectangle(padX, item.Y, Width - padX * 2, item.Height);
                TextRenderer.DrawText(g, item.Text, item.Font, rect, RoleColor(item.Role, t), TextFlags);
            }
            else
            {
                DrawCode(g, item, t, clipTop, clipBottom);
            }
        }
    }

    private static Color RoleColor(Role role, Theme t)
    {
        switch (role)
        {
            case Role.Muted: return t.TextMuted;
            case Role.Accent: return t.Accent;
            default: return t.Text;
        }
    }

    private static Color SyntaxColor(TokenKind kind, Theme t)
    {
        if (t.IsDark)
        {
            switch (kind)
            {
                case TokenKind.Keyword: return Color.FromArgb(0x56, 0x9C, 0xD6);
                case TokenKind.Type: return Color.FromArgb(0x4E, 0xC9, 0xB0);
                case TokenKind.String: return Color.FromArgb(0xCE, 0x91, 0x78);
                case TokenKind.Comment: return Color.FromArgb(0x6A, 0x99, 0x55);
                case TokenKind.Number: return Color.FromArgb(0xB5, 0xCE, 0xA8);
                default: return t.Text;
            }
        }

        switch (kind)
        {
            case TokenKind.Keyword: return Color.FromArgb(0x00, 0x00, 0xFF);
            case TokenKind.Type: return Color.FromArgb(0x2B, 0x91, 0xAF);
            case TokenKind.String: return Color.FromArgb(0xA3, 0x15, 0x15);
            case TokenKind.Comment: return Color.FromArgb(0x00, 0x80, 0x00);
            case TokenKind.Number: return Color.FromArgb(0x09, 0x86, 0x58);
            default: return t.Text;
        }
    }

    private void DrawCode(Graphics g, Item item, Theme t, int clipTop, int clipBottom)
    {
        int padX = Scale(22);
        int pad = Scale(8);
        int codePad = Scale(10);
        var block = new Rectangle(padX, item.Y, Width - padX * 2, item.Height);

        using (var back = new SolidBrush(t.InputBack))
            g.FillRectangle(back, block);
        using (var gutterBrush = new SolidBrush(t.Background))
            g.FillRectangle(gutterBrush, block.X + 1, block.Y + 1, item.Gutter, block.Height - 2);
        using (var pen = new Pen(t.Border))
        {
            g.DrawRectangle(pen, block.X, block.Y, block.Width - 1, block.Height - 1);
            g.DrawLine(pen, block.X + item.Gutter + 1, block.Y + 1, block.X + item.Gutter + 1, block.Bottom - 2);
        }

        // code (clipped so horizontal scrolling never covers the gutter)
        var codeArea = new Rectangle(block.X + item.Gutter + 2, block.Y + 1, block.Width - item.Gutter - 3, block.Height - 2);
        int textX = block.X + item.Gutter + 1 + codePad - _hOffset;
        GraphicsState state = g.Save();
        g.SetClip(codeArea, CombineMode.Intersect);
        for (int i = 0; i < item.Lines.Length; i++)
        {
            int y = block.Y + pad + i * _lineHeight;
            if (y + _lineHeight < clipTop) continue;
            if (y > clipBottom) break;
            DrawLine(g, item.Lines[i], item.Tokens[i], textX, y, t);
        }
        g.Restore(state);

        // line numbers
        for (int i = 0; i < item.Lines.Length; i++)
        {
            int y = block.Y + pad + i * _lineHeight;
            if (y + _lineHeight < clipTop) continue;
            if (y > clipBottom) break;
            var numberRect = new Rectangle(block.X + 1, y, item.Gutter - Scale(10), _lineHeight);
            TextRenderer.DrawText(g, (item.FirstLine + i).ToString(), _codeFont, numberRect, t.TextMuted,
                TextFormatFlags.Right | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.Top);
        }
    }

    private void DrawLine(Graphics g, string line, List<CodeToken> tokens, int textX, int y, Theme t)
    {
        int position = 0;
        foreach (CodeToken token in tokens)
        {
            if (token.Start > position)
                DrawRun(g, line.Substring(position, token.Start - position), position, textX, y, t.Text);
            DrawRun(g, line.Substring(token.Start, token.Length), token.Start, textX, y, SyntaxColor(token.Kind, t));
            position = token.Start + token.Length;
        }
        if (position < line.Length)
            DrawRun(g, line.Substring(position), position, textX, y, t.Text);
    }

    private void DrawRun(Graphics g, string text, int column, int textX, int y, Color color)
    {
        string trimmed = text.TrimStart();
        if (trimmed.Length == 0) return;
        column += text.Length - trimmed.Length;      // leading spaces only move the start
        int x = textX + (int)Math.Round(column * _charWidth);
        TextRenderer.DrawText(g, trimmed, _codeFont, new Point(x, y), color, RunFlags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _feedbackTimer.Dispose();
            _headFont.Dispose();
            _bodyFont.Dispose();
            _boldFont.Dispose();
            _codeFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
