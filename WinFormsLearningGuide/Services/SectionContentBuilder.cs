using System;
using System.Collections.Generic;
using WinFormsLearningGuide.Models;

namespace WinFormsLearningGuide.Services;

/// <summary>Turns a control's documentation into the content blocks of each section card.</summary>
public class SectionContentBuilder
{
    private readonly ControlPreviewService _previews;

    public SectionContentBuilder(ControlPreviewService previews)
    {
        _previews = previews ?? throw new ArgumentNullException(nameof(previews));
    }

    public List<ContentBlock> Build(ControlDocumentation doc, DocumentationSection section)
    {
        if (!section.IsComplete)
            return Planned(section);

        switch (section.Number)
        {
            case 1: return BuildOverview(doc);
            case 2: return BuildAppearance(doc);
            case 3: return BuildProperties(doc);
            case 7: return BuildMistakes(doc);
            case 8: return BuildRelated(doc);
            default: return Planned(section);
        }
    }

    private static List<ContentBlock> Planned(DocumentationSection section) => new List<ContentBlock>
    {
        ContentBlock.Note("Planned for Phase " + section.Phase + ": " + section.Description)
    };

    private static List<ContentBlock> MissingContent(ControlDocumentation doc, string what)
    {
        return new List<ContentBlock>
        {
            ContentBlock.Note("The " + what + " for " + doc.Name + " is not available (no entry in Data/Content).")
        };
    }

    // ------------------------------------------------------------------ 1. Overview

    private static List<ContentBlock> BuildOverview(ControlDocumentation doc)
    {
        var blocks = new List<ContentBlock> { ContentBlock.Paragraph(doc.Summary) };
        ControlContent c = doc.Content;
        if (c == null) return blocks;

        if (!string.IsNullOrWhiteSpace(c.Purpose))
        {
            blocks.Add(ContentBlock.SubHeading("Purpose"));
            blocks.Add(ContentBlock.Paragraph(c.Purpose));
        }
        if (c.WhenToUse.Count > 0)
        {
            blocks.Add(ContentBlock.SubHeading("When to use it"));
            foreach (string item in c.WhenToUse) blocks.Add(ContentBlock.Bullet(item));
        }
        if (c.RealWorldExamples.Count > 0)
        {
            blocks.Add(ContentBlock.SubHeading("Real-world examples"));
            foreach (string item in c.RealWorldExamples) blocks.Add(ContentBlock.Bullet(item));
        }
        return blocks;
    }

    // ------------------------------------------------------------------ 2. Appearance

    private List<ContentBlock> BuildAppearance(ControlDocumentation doc)
    {
        ControlContent c = doc.Content;
        if (c == null || string.IsNullOrWhiteSpace(c.DefaultAppearance))
            return MissingContent(doc, "appearance description");

        var blocks = new List<ContentBlock> { ContentBlock.Paragraph(c.DefaultAppearance) };

        IReadOnlyList<PreviewImage> images = _previews.Render(doc.Name);
        if (images.Count > 0)
        {
            blocks.Add(ContentBlock.SubHeading("Illustration"));
            foreach (PreviewImage image in images)
            {
                blocks.Add(ContentBlock.Picture(image.Image));
                blocks.Add(ContentBlock.Caption(image.Caption));
            }
        }
        else if (_previews.GetMode(doc.Name) == PreviewMode.None)
        {
            blocks.Add(ContentBlock.Note(doc.Name + " has no visible user interface of its own, so there is nothing to draw here."));
        }
        else
        {
            blocks.Add(ContentBlock.Note("The live illustration could not be rendered on this computer."));
        }
        return blocks;
    }

    // ------------------------------------------------------------------ 3. Properties reference

    private static readonly int[] OwnTableWeights = { 19, 17, 36, 28 };
    private static readonly bool[] OwnTableMono = { true, true, false, true };
    private static readonly int[] InheritedTableWeights = { 15, 14, 18, 33, 24 };
    private static readonly bool[] InheritedTableMono = { true, true, false, false, true };

    private static List<ContentBlock> BuildProperties(ControlDocumentation doc)
    {
        List<PropertyDocumentation> own = doc.Properties;
        List<PropertyDocumentation> inherited = doc.InheritedProperties;
        if (own.Count == 0 && inherited.Count == 0)
            return MissingContent(doc, "property reference");

        var blocks = new List<ContentBlock>();

        string intro = own.Count > 0
            ? "The table lists the key properties of " + doc.Name + ". Each one is explained below with its data type, " +
              "default value, accepted values, practical usage, a C# example and the effect you will see."
            : doc.Name + " adds no key properties of its own; everything it offers is inherited.";
        if (inherited.Count > 0)
            intro += " Properties shared with every " + doc.InheritedFrom + " are listed at the end.";
        blocks.Add(ContentBlock.Paragraph(intro));

        if (own.Count > 0)
        {
            blocks.Add(ContentBlock.SubHeading("Properties of " + doc.ClassName));

            var rows = new List<string[]>();
            foreach (PropertyDocumentation p in own)
                rows.Add(new[] { p.Name, p.DataType, p.Description, p.Example });
            blocks.Add(ContentBlock.Table(
                new[] { "Property", "Data Type", "Description", "Example" }, rows, OwnTableWeights, OwnTableMono));

            blocks.Add(ContentBlock.SubHeading("Property details"));
            foreach (PropertyDocumentation p in own)
            {
                blocks.Add(ContentBlock.Title(p.Name));
                AddIfNotEmpty(blocks, p.Description, ContentBlock.Paragraph);
                AddField(blocks, "Data type", p.DataType);
                AddField(blocks, "Default value", p.DefaultValue);
                AddField(blocks, "Accepted values", p.AcceptedValues);
                AddField(blocks, "Practical usage", p.Usage);
                AddIfNotEmpty(blocks, p.Example, ContentBlock.Code);
                AddField(blocks, "Expected effect", p.ExpectedEffect);
            }
        }

        if (inherited.Count > 0)
        {
            blocks.Add(ContentBlock.SubHeading("Inherited from " + doc.InheritedFrom));
            blocks.Add(ContentBlock.Note(
                doc.InheritedFrom == "ToolStripItem"
                    ? "Every item that can be placed on a ToolStrip derives from ToolStripItem, so " + doc.Name + " has these properties too."
                    : "Every Windows Forms control derives from Control, so " + doc.Name + " has these properties too."));

            var rows = new List<string[]>();
            foreach (PropertyDocumentation p in inherited)
                rows.Add(new[] { p.Name, p.DataType, p.DefaultValue, p.Description, p.Example });
            blocks.Add(ContentBlock.Table(
                new[] { "Property", "Data Type", "Default", "Description", "Example" }, rows,
                InheritedTableWeights, InheritedTableMono));
        }

        return blocks;
    }

    private static void AddField(List<ContentBlock> blocks, string label, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            blocks.Add(ContentBlock.Field(label, text));
    }

    private static void AddIfNotEmpty(List<ContentBlock> blocks, string text, Func<string, ContentBlock> create)
    {
        if (!string.IsNullOrWhiteSpace(text))
            blocks.Add(create(text));
    }

    // ------------------------------------------------------------------ 7. Common mistakes

    private static List<ContentBlock> BuildMistakes(ControlDocumentation doc)
    {
        ControlContent c = doc.Content;
        if (c == null || c.CommonMistakes.Count == 0)
            return MissingContent(doc, "common mistakes");

        var blocks = new List<ContentBlock>();
        foreach (MistakeDocumentation m in c.CommonMistakes)
        {
            blocks.Add(ContentBlock.Mistake(m.Mistake));
            blocks.Add(ContentBlock.Fix(m.Fix));
        }
        return blocks;
    }

    // ------------------------------------------------------------------ 8. Related controls

    private static List<ContentBlock> BuildRelated(ControlDocumentation doc)
    {
        var blocks = new List<ContentBlock>();
        if (doc.RelatedControls.Count == 0)
        {
            blocks.Add(ContentBlock.Note("No related controls are listed for " + doc.Name + "."));
            return blocks;
        }

        foreach (string name in doc.RelatedControls)
        {
            string difference = FindDifference(doc.Content, name);
            blocks.Add(ContentBlock.Bullet(difference.Length > 0 ? name + ": " + difference : name));
        }
        blocks.Add(ContentBlock.Note("Use the \"Related controls\" list in the quick reference panel to open any of these controls."));
        return blocks;
    }

    private static string FindDifference(ControlContent content, string name)
    {
        if (content == null) return "";
        foreach (RelatedControlNote note in content.RelatedNotes)
            if (string.Equals(note.Name, name, StringComparison.OrdinalIgnoreCase))
                return note.Difference ?? "";
        return "";
    }
}
