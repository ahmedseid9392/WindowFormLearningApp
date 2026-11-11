using System.Collections.Generic;

namespace WinFormsLearningGuide.Models;

/// <summary>Methods of one control (Data/Methods/*.methods.json) or of a base class (Data/Methods/Inherited.json).</summary>
public class ControlMethods
{
    public string Name { get; set; } = "";
    public List<MethodDocumentation> Methods { get; set; } = new List<MethodDocumentation>();
}
