using System.Collections.Generic;

namespace WinFormsLearningGuide.Models;

/// <summary>
/// A named list of events as stored in Data/Events/*.json. In *.events.json files the name is a control
/// name; in Inherited.json it is the name of a base class ("Control" or "ToolStripItem").
/// </summary>
public class ControlEvents
{
    public string Name { get; set; } = "";
    public List<EventDocumentation> Events { get; set; } = new List<EventDocumentation>();
}

/// <summary>Same as <see cref="ControlEvents"/>, for methods (Data/Methods/*.json).</summary>
public class ControlMethods
{
    public string Name { get; set; } = "";
    public List<MethodDocumentation> Methods { get; set; } = new List<MethodDocumentation>();
}
