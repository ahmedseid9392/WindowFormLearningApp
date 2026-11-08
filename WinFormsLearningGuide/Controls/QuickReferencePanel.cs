using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsLearningGuide.Helpers;
using WinFormsLearningGuide.Models;

namespace WinFormsLearningGuide.Controls;

/// <summary>
/// Right sidebar: namespace, class, inheritance, important members, related controls
/// and links that jump to a documentation section.
/// </summary>
public class QuickReferencePanel : UserControl, IThemeable
{
    private readonly FlowLayoutPanel _flow = new FlowLayoutPanel();
    private readonly Font _captionFont;
    private readonly Font _titleFont;
    private readonly Font _monoFont;

    public event EventHandler<string> RelatedControlClicked;
    public event EventHandler<int> SectionClicked;

    public QuickReferencePanel()
    {
        Dock = DockStyle.Fill;
        Tag = "sidebar";

        _captionFont = new Font("Segoe UI", 8F, FontStyle.Bold);
        _titleFont = new Font("Segoe UI Semibold", 15F);
        _monoFont = new Font("Consolas", 9F);

        _flow.Dock = DockStyle.Fill;
        _flow.FlowDirection = FlowDirection.TopDown;
        _flow.WrapContents = false;
        _flow.AutoScroll = true;
        _flow.Padding = new Padding(16, 12, 8, 12);
        _flow.Tag = "sidebar";
        _flow.SizeChanged += (s, e) => UpdateWidths();
        Controls.Add(_flow);
    }

    public void ApplyTheme(Theme theme)
    {
        // Labels are themed through their Tag when created and by ThemeHelper.Apply afterwards.
    }

    public void ShowControl(ControlDocumentation doc, IReadOnlyList<DocumentationSection> sections)
    {
        _flow.SuspendLayout();
        foreach (Control old in _flow.Controls.Cast<Control>().ToList())
            old.Dispose();

        AddLabel("QUICK REFERENCE", "caption", _captionFont, 0, 6, 0);
        AddLabel(doc.Name, "title", _titleFont, 0, 8, 0);

        AddCaption("Namespace");
        AddLabel(doc.Namespace, "mono", _monoFont);

        AddCaption("Class");
        AddLabel(doc.ClassName, "mono", _monoFont);

        AddCaption("Inheritance");
        for (int i = 0; i < doc.Inheritance.Count; i++)
        {
            bool last = i == doc.Inheritance.Count - 1;
            string prefix = i > 0 ? "\u2514 " : "";
            AddLabel(prefix + doc.Inheritance[i], last ? "accent" : "mono", _monoFont, Math.Min(i, 6) * 8);
        }

        AddCaption("Important properties");
        AddList(doc.ImportantProperties);

        AddCaption("Important events");
        AddList(doc.ImportantEvents);

        AddCaption("Related controls");
        if (doc.RelatedControls.Count == 0)
            AddLabel("None", "muted", _monoFont);
        foreach (string name in doc.RelatedControls)
        {
            string target = name;
            AddLink(name, () => RelatedControlClicked?.Invoke(this, target));
        }

        AddCaption("Jump to section");
        for (int i = 0; i < sections.Count; i++)
        {
            int index = i;
            AddLink(sections[i].Number + ". " + sections[i].Title, () => SectionClicked?.Invoke(this, index));
        }

        _flow.ResumeLayout(true);
        _flow.AutoScrollPosition = Point.Empty;
        UpdateWidths();
    }

    // ------------------------------------------------------------------ helpers

    private int ContentWidth =>
        Math.Max(80, _flow.ClientSize.Width - _flow.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);

    private void AddCaption(string text) => AddLabel(text.ToUpperInvariant(), "caption", _captionFont, 0, 4, 16);

    private void AddList(IList<string> items)
    {
        if (items.Count == 0)
        {
            AddLabel("None commonly used", "muted", _monoFont);
            return;
        }
        foreach (string item in items)
            AddLabel(item, "mono", _monoFont);
    }

    private Label AddLabel(string text, string tag, Font font, int indent = 0, int bottom = 3, int top = 0)
    {
        var label = new Label
        {
            AutoSize = true,
            Text = text,
            Tag = tag,
            Font = font,
            UseMnemonic = false,
            Margin = new Padding(indent, top, 0, bottom),
            MaximumSize = new Size(Math.Max(60, ContentWidth - indent), 0)
        };
        _flow.Controls.Add(label);
        ThemeHelper.Apply(label);
        return label;
    }

    private void AddLink(string text, Action onClick)
    {
        Label label = AddLabel(text, "link", _monoFont, 0, 4);
        label.Cursor = Cursors.Hand;
        label.Click += (s, e) => onClick();
    }

    private void UpdateWidths()
    {
        int width = ContentWidth;
        foreach (Label label in _flow.Controls.OfType<Label>())
        {
            int max = Math.Max(60, width - label.Margin.Left);
            if (label.MaximumSize.Width != max)
                label.MaximumSize = new Size(max, 0);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _captionFont.Dispose();
            _titleFont.Dispose();
            _monoFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
