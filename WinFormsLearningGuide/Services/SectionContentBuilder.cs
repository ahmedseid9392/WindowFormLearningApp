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
            case 4: return BuildEvents(doc);
            case 5: return BuildMethods(doc);
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

    // ------------------------------------------------------------------ 4. Events reference

    private static readonly int[] EventTableWeights = { 20, 30, 50 };
    private static readonly bool[] EventTableMono = { true, true, false };

    private static List<ContentBlock> BuildEvents(ControlDocumentation doc)
    {
        var blocks = new List<ContentBlock>();

        if (doc.Events.Count == 0)
        {
            blocks.Add(ContentBlock.Paragraph(doc.Name + " has no commonly used events of its own."));
            if (doc.Name == "ErrorProvider")
                blocks.Add(ContentBlock.Note("Use it together with the Validating event of the controls it decorates (see TextBox): the handler calls SetError to show or clear the message."));
            else if (doc.InheritedFrom == "Control")
                blocks.Add(ContentBlock.Note("Every control inherits the common events of Control, such as Click, Enter, Leave, KeyDown, MouseEnter and Resize."));
            return blocks;
        }

        blocks.Add(ContentBlock.Paragraph(
            "These are the most useful events of " + doc.Name + ". You subscribe with += and the handler receives the sender " +
            "and an event-arguments object that carries the event data. In the examples, this is the form that contains the control."));

        blocks.Add(ContentBlock.SubHeading("Events of " + doc.ClassName));
        var rows = new List<string[]>();
        foreach (EventDocumentation ev in doc.Events)
            rows.Add(new[] { ev.Name, ev.EventArgsType, ev.Description });
        blocks.Add(ContentBlock.Table(new[] { "Event", "Event Args", "When it happens" }, rows, EventTableWeights, EventTableMono));

        blocks.Add(ContentBlock.SubHeading("Event details"));
        foreach (EventDocumentation ev in doc.Events)
        {
            blocks.Add(ContentBlock.Title(ev.Name));
            AddField(blocks, "When it happens", ev.Description);
            AddField(blocks, "Event arguments", ev.EventArgsType);
            AddField(blocks, "Real-world use", ev.UseCase);
            AddIfNotEmpty(blocks, ev.Example, ContentBlock.Code);
        }

        if (doc.InheritedFrom == "Control")
            blocks.Add(ContentBlock.Note("Every control also inherits the common events of Control, such as Click, Enter, Leave, KeyDown, MouseEnter and Resize."));

        return blocks;
    }

    // ------------------------------------------------------------------ 5. Methods reference

    private static readonly int[] MethodTableWeights = { 19, 13, 38, 30 };
    private static readonly bool[] MethodTableMono = { true, true, false, true };

    private static List<ContentBlock> BuildMethods(ControlDocumentation doc)
    {
        List<MethodDocumentation> own = doc.Methods;
        List<MethodDocumentation> inherited = doc.InheritedMethods;
        var blocks = new List<ContentBlock>();

        if (own.Count == 0 && inherited.Count == 0)
        {
            blocks.Add(ContentBlock.Paragraph(doc.Name + " has no commonly used methods."));
            return blocks;
        }

        blocks.Add(ContentBlock.Paragraph(own.Count > 0
            ? "The key methods of " + doc.Name + ", each with its parameters, return value and a C# example."
            : doc.Name + " has no methods of its own that beginners need. The methods it shares with every " +
              doc.InheritedFrom + " are the ones to know."));

        if (own.Count > 0)
        {
            blocks.Add(ContentBlock.SubHeading("Methods of " + doc.ClassName));
            var rows = new List<string[]>();
            foreach (MethodDocumentation m in own)
                rows.Add(new[] { m.Name, m.ReturnType, m.Description, m.Example });
            blocks.Add(ContentBlock.Table(new[] { "Method", "Returns", "Description", "Example" }, rows, MethodTableWeights, MethodTableMono));

            blocks.Add(ContentBlock.SubHeading("Method details"));
            foreach (MethodDocumentation m in own)
            {
                blocks.Add(ContentBlock.Title(m.Signature));
                AddIfNotEmpty(blocks, m.Description, ContentBlock.Paragraph);
                if (m.Parameters.Count == 0)
                    AddField(blocks, "Parameters", "None");
                foreach (ParameterDocumentation p in m.Parameters)
                    AddField(blocks, "Parameter", p.Name + " (" + p.Type + "): " + p.Description);
                AddField(blocks, "Returns", m.Returns);
                AddIfNotEmpty(blocks, m.Example, ContentBlock.Code);
            }
        }

        if (inherited.Count > 0)
        {
            blocks.Add(ContentBlock.SubHeading("Inherited from " + doc.InheritedFrom));
            blocks.Add(ContentBlock.Note(
                doc.InheritedFrom == "ToolStripItem"
                    ? "These methods are shared by every item that can be placed on a ToolStrip."
                    : "These methods are shared by every Windows Forms control."));
            var rows = new List<string[]>();
            foreach (MethodDocumentation m in inherited)
                rows.Add(new[] { m.Name, m.ReturnType, m.Description, m.Example });
            blocks.Add(ContentBlock.Table(new[] { "Method", "Returns", "Description", "Example" }, rows, MethodTableWeights, MethodTableMono));
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
