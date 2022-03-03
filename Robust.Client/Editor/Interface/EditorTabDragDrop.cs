using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.Editor.Interface;

public sealed class EditorTabDragDrop : DragDropOperation
{
    public EditorTabDragDrop(EditorPanel panel)
    {
        Panel = panel;
    }

    public EditorPanel Panel { get; }

    public override void Drop()
    {
        var window = new OSWindow();
        var docker = new EditorWindowDocker(window);
        docker.AddPanel(Panel);
        window.AddChild(docker);
        window.Show();
    }
}
