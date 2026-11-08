using System.Windows.Forms;

namespace WinFormsLearningGuide.Controls;

/// <summary>An auto-scrolling panel that can receive focus so the mouse wheel works.</summary>
public class ScrollPanel : Panel
{
    public ScrollPanel()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        DoubleBuffered = true;
        AutoScroll = true;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        base.OnMouseDown(e);
    }
}
