using System.Collections.Generic;

namespace WinFormsLearningGuide.Models;

/// <summary>
/// A named list of properties as stored in Data/Properties/*.json. In *.properties.json files the name is
/// a control name; in Inherited.json it is the name of a base class ("Control" or "ToolStripItem").
/// </summary>
public class ControlProperties
{
    public string Name { get; set; } = "";
    public List<PropertyDocumentation> Properties { get; set; } = new List<PropertyDocumentation>();
}
