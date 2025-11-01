using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.Editor.Interface;

public sealed class EditorTabDragDrop : DragDropOperation
{
    internal EditorTabDragDrop(EditorPanel panel, EditorTabPanel originatingTabPanel)
    {
        Panel = panel;
        OriginatingTabPanel = originatingTabPanel;
    }

    public EditorPanel Panel { get; }
    public EditorTabPanel OriginatingTabPanel { get; }

    public override void Drop()
    {
        var window = new OSWindow();
        var docker = new EditorWindowDocker(window);
        docker.AddPanel(Panel);
        window.AddChild(docker);
        window.Show();
    }

    public override void AfterDrop()
    {
        OriginatingTabPanel.ConfirmRemoval();
    }
}
