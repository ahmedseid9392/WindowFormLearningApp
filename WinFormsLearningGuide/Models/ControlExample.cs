using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace WinFormsLearningGuide.Models;

/// <summary>A piece of the example program and the explanation shown for it in the walkthrough.</summary>
public class ExampleSegment
{
    /// <summary>One or more lines of code (separated by \n). An empty string is one blank line.</summary>
    public string Code { get; set; } = "";

    /// <summary>The explanation. Segments without one (blank lines, closing braces) are not listed in the walkthrough.</summary>
    public string Explain { get; set; } = "";
}

/// <summary>A different way to do the same thing, shown on the Alternatives tab.</summary>
public class AlternativeExample
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Code { get; set; } = "";
}

/// <summary>Phase 5: a complete, compilable program for one control, split into explained segments.</summary>
public class ControlExample
{
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public List<ExampleSegment> Segments { get; set; } = new List<ExampleSegment>();
    public List<AlternativeExample> Alternatives { get; set; } = new List<AlternativeExample>();

    /// <summary>The whole program: all segments joined by line breaks.</summary>
    [JsonIgnore]
    public string FullCode => string.Join("\n", Segments.Select(s => s.Code));

    public static int CountLines(string code) => string.IsNullOrEmpty(code) ? 1 : code.Split('\n').Length;
}
