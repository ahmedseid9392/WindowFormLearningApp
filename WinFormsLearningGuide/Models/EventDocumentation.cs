namespace WinFormsLearningGuide.Models;

/// <summary>
/// Documentation for one event (Phase 4). Loaded from Data/Events/*.events.json.
/// Events shared by a whole base class (Control, ToolStripItem) live in Data/Events/Inherited.json.
/// </summary>
public class EventDocumentation
{
    public string Name { get; set; } = "";

    /// <summary>When the event is raised.</summary>
    public string Description { get; set; } = "";

    /// <summary>The delegate type, for example EventHandler or CancelEventHandler.</summary>
    public string Delegate { get; set; } = "EventHandler";

    /// <summary>The event argument class, for example EventArgs or MouseEventArgs.</summary>
    public string EventArgsType { get; set; } = "EventArgs";

    /// <summary>What the event argument object gives you.</summary>
    public string ArgumentsNote { get; set; } = "";

    /// <summary>A real-world situation where you handle the event.</summary>
    public string UseCase { get; set; } = "";

    /// <summary>A complete C# subscription plus handler method.</summary>
    public string Example { get; set; } = "";
}
