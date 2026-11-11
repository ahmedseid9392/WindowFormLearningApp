using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WinFormsLearningGuide.Models;

public class ControlDocumentation
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Namespace { get; set; } = "System.Windows.Forms";
    public string ClassName { get; set; } = "";
    public List<string> Inheritance { get; set; } = new List<string>();
    public string Summary { get; set; } = "";
    public List<string> ImportantProperties { get; set; } = new List<string>();
    public List<string> ImportantEvents { get; set; } = new List<string>();
    public List<string> RelatedControls { get; set; } = new List<string>();

    /// <summary>Phase 2 content (overview, appearance, mistakes, related notes). Null if none was found.</summary>
    [JsonIgnore]
    public ControlContent Content { get; set; }

    /// <summary>Phase 3: the properties specific to this control.</summary>
    public List<PropertyDocumentation> Properties { get; set; } = new List<PropertyDocumentation>();

    /// <summary>Phase 3: shared properties inherited from a base class (Control or ToolStripItem), without the ones listed in Properties.</summary>
    [JsonIgnore]
    public List<PropertyDocumentation> InheritedProperties { get; set; } = new List<PropertyDocumentation>();

    /// <summary>Phase 3: name of the base class the InheritedProperties come from ("" if none).</summary>
    [JsonIgnore]
    public string InheritedFrom { get; set; } = "";

    /// <summary>Phase 4: the important events of this control.</summary>
    public List<EventDocumentation> Events { get; set; } = new List<EventDocumentation>();

    /// <summary>Phase 4: the important methods of this control itself.</summary>
    public List<MethodDocumentation> Methods { get; set; } = new List<MethodDocumentation>();

    /// <summary>Phase 4: shared methods inherited from Control or ToolStripItem (see InheritedFrom).</summary>
    [JsonIgnore]
    public List<MethodDocumentation> InheritedMethods { get; set; } = new List<MethodDocumentation>();

    /// <summary>Phase 5: the complete example program. Null if none was found.</summary>
    [JsonIgnore]
    public ControlExample Example { get; set; }

    [JsonIgnore]
    public string FullName => Namespace + "." + ClassName;

    public override string ToString() => Name;
}
