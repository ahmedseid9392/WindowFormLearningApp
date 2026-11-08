using System.Collections.Generic;

namespace WinFormsLearningGuide.Models;

/// <summary>A typical beginner mistake and how to avoid it.</summary>
public class MistakeDocumentation
{
    public string Mistake { get; set; } = "";
    public string Fix { get; set; } = "";
}

/// <summary>How a related control differs from the one being documented.</summary>
public class RelatedControlNote
{
    public string Name { get; set; } = "";
    public string Difference { get; set; } = "";
}

/// <summary>
/// Phase 2 learning content for one control (sections 1, 2, 7 and 8).
/// Loaded from Data/Content/*.json and matched to a <see cref="ControlDocumentation"/> by name.
/// </summary>
public class ControlContent
{
    public string Name { get; set; } = "";
    public string Purpose { get; set; } = "";
    public List<string> WhenToUse { get; set; } = new List<string>();
    public List<string> RealWorldExamples { get; set; } = new List<string>();
    public string DefaultAppearance { get; set; } = "";
    public List<MistakeDocumentation> CommonMistakes { get; set; } = new List<MistakeDocumentation>();
    public List<RelatedControlNote> RelatedNotes { get; set; } = new List<RelatedControlNote>();
}
