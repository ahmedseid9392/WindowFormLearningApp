using System.Collections.Generic;

namespace WinFormsLearningGuide.Models;

public class ParameterDocumentation
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>Documentation for one method (Phase 4).</summary>
public class MethodDocumentation
{
    public string Name { get; set; } = "";
    public string ReturnType { get; set; } = "void";

    /// <summary>For example "int Find(string str)".</summary>
    public string Signature { get; set; } = "";

    public string Description { get; set; } = "";
    public List<ParameterDocumentation> Parameters { get; set; } = new List<ParameterDocumentation>();
    public string Returns { get; set; } = "";
    public string Example { get; set; } = "";
}
