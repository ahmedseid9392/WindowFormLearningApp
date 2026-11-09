using System.Collections.Generic;

namespace WinFormsLearningGuide.Models;

/// <summary>
/// Documentation for one method (Phase 4). Loaded from Data/Methods/*.methods.json.
/// Methods shared by a whole base class (Control, ToolStripItem) live in Data/Methods/Inherited.json.
/// </summary>
public class MethodDocumentation
{
    public string Name { get; set; } = "";

    /// <summary>The full signature, for example "void Focus()" or "bool Contains(Control ctl)".</summary>
    public string Signature { get; set; } = "";

    public string Description { get; set; } = "";
    public List<ParameterDocumentation> Parameters { get; set; } = new List<ParameterDocumentation>();
    public string ReturnType { get; set; } = "void";
    public string ReturnDescription { get; set; } = "";

    /// <summary>A C# snippet showing the call.</summary>
    public string Example { get; set; } = "";

    /// <summary>What you will see or what will happen after the example runs.</summary>
    public string ExpectedEffect { get; set; } = "";
}
