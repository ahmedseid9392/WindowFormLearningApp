using System.Collections.Generic;
using System.Drawing;

namespace WinFormsLearningGuide.Models;

public enum BlockKind
{
    Paragraph,
    SubHeading,
    Bullet,
    Mistake,
    Fix,
    Image,
    Caption,
    Note,
    Title,   // a name such as a property name, drawn in a monospaced bold font with a divider above it
    Field,   // "Label   value" on one line (the value wraps beside the label)
    Code,    // a monospaced code block
    Table    // a table with a header row
}

/// <summary>One piece of content inside a section card. A card owns (and disposes) any image it is given.</summary>
public sealed class ContentBlock
{
    public BlockKind Kind { get; }
    public string Text { get; }
    public string Label { get; }
    public Image Image { get; }

    /// <summary>Table only: the header cells.</summary>
    public string[] Header { get; }

    /// <summary>Table only: the body rows. Each row has one string per column.</summary>
    public IReadOnlyList<string[]> Rows { get; }

    /// <summary>Table only: relative column widths.</summary>
    public int[] Weights { get; }

    /// <summary>Table only: columns that are drawn in a monospaced font.</summary>
    public bool[] Mono { get; }

    private ContentBlock(BlockKind kind, string text, Image image, string label,
                         string[] header, IReadOnlyList<string[]> rows, int[] weights, bool[] mono)
    {
        Kind = kind;
        Text = text ?? "";
        Image = image;
        Label = label ?? "";
        Header = header;
        Rows = rows;
        Weights = weights;
        Mono = mono;
    }

    private static ContentBlock Simple(BlockKind kind, string text) =>
        new ContentBlock(kind, text, null, "", null, null, null, null);

    public static ContentBlock Paragraph(string text) => Simple(BlockKind.Paragraph, text);
    public static ContentBlock SubHeading(string text) => Simple(BlockKind.SubHeading, text);
    public static ContentBlock Bullet(string text) => Simple(BlockKind.Bullet, text);
    public static ContentBlock Mistake(string text) => Simple(BlockKind.Mistake, text);
    public static ContentBlock Fix(string text) => Simple(BlockKind.Fix, text);
    public static ContentBlock Caption(string text) => Simple(BlockKind.Caption, text);
    public static ContentBlock Note(string text) => Simple(BlockKind.Note, text);
    public static ContentBlock Title(string text) => Simple(BlockKind.Title, text);
    public static ContentBlock Code(string text) => Simple(BlockKind.Code, text);

    public static ContentBlock Picture(Image image) =>
        new ContentBlock(BlockKind.Image, "", image, "", null, null, null, null);

    public static ContentBlock Field(string label, string text) =>
        new ContentBlock(BlockKind.Field, text, null, label, null, null, null, null);

    public static ContentBlock Table(string[] header, IReadOnlyList<string[]> rows, int[] weights, bool[] mono) =>
        new ContentBlock(BlockKind.Table, "", null, "", header, rows, weights, mono);
}
