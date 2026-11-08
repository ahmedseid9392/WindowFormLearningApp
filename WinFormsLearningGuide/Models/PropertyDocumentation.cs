namespace WinFormsLearningGuide.Models;

/// <summary>
/// Documentation for one property (Phase 3). Loaded from Data/Properties/*.properties.json.
/// Properties shared by a whole base class (Control, ToolStripItem) live in Data/Properties/Inherited.json
/// and only use Name, DataType, DefaultValue, Description and Example.
/// </summary>
public class PropertyDocumentation
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";

    /// <summary>What the property means (one or two sentences, also shown in the table).</summary>
    public string Description { get; set; } = "";

    public string DefaultValue { get; set; } = "";
    public string AcceptedValues { get; set; } = "";

    /// <summary>Practical usage: when and why you would set it.</summary>
    public string Usage { get; set; } = "";

    /// <summary>A C# statement showing how to set or read it.</summary>
    public string Example { get; set; } = "";

    /// <summary>What you will see or what will happen after the example runs.</summary>
    public string ExpectedEffect { get; set; } = "";
}
