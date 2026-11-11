using System.Collections.Generic;

namespace WinFormsLearningGuide.Models;

/// <summary>The eight sections every control page contains.</summary>
public sealed class DocumentationSection
{
    /// <summary>The newest phase whose content is implemented. Sections for later phases show placeholders.</summary>
    public const int CurrentPhase = 5;

    public bool IsComplete => Phase <= CurrentPhase;

    public int Number { get; }
    public string Title { get; }
    public int Phase { get; }
    public string Description { get; }

    public DocumentationSection(int number, string title, int phase, string description)
    {
        Number = number;
        Title = title;
        Phase = phase;
        Description = description;
    }

    public static readonly IReadOnlyList<DocumentationSection> All = new List<DocumentationSection>
    {
        new DocumentationSection(1, "Overview", 2,
            "What the control is, its purpose, when to use it, and real-world examples."),
        new DocumentationSection(2, "Appearance", 2,
            "Default appearance and visual illustrations of the control."),
        new DocumentationSection(3, "Properties Reference", 3,
            "A structured table of properties with data type, default value, accepted values, a C# example and the expected effect."),
        new DocumentationSection(4, "Events Reference", 4,
            "Every important event with its arguments, a real-world use case and a complete event-handler example."),
        new DocumentationSection(5, "Methods Reference", 4,
            "Important methods with parameters, return types and examples."),
        new DocumentationSection(6, "Complete C# Example", 5,
            "A compilable program that creates and configures the control in code, shown in the source code viewer with syntax highlighting, line numbers, copy and download."),
        new DocumentationSection(7, "Common Mistakes", 2,
            "Typical beginner mistakes and how to avoid them."),
        new DocumentationSection(8, "Related Controls", 2,
            "Similar controls and how they differ."),
    }.AsReadOnly();
}
