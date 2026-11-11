using System.Collections.Generic;

namespace WinFormsLearningGuide.Models;

/// <summary>Events of one control (Data/Events/*.events.json).</summary>
public class ControlEvents
{
    public string Name { get; set; } = "";
    public List<EventDocumentation> Events { get; set; } = new List<EventDocumentation>();
}
