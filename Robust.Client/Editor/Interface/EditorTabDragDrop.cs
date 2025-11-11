using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

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
        var clyde = IoCManager.Resolve<IClyde>();
        var window = new OSWindow { Owner = clyde.MainWindow };
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
