using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Robust.Client.Editor.Interface;

public abstract class EditorTabBaseDragDrop : DragDropOperation
{
    private protected EditorTabBaseDragDrop(EditorPanel panel, EditorTabPanel originatingTabPanel)
    {
        Panel = panel;
        OriginatingTabPanel = originatingTabPanel;
    }

    public EditorPanel Panel { get; }
    public EditorTabPanel OriginatingTabPanel { get; }
}

public sealed class EditorTabOnlyReorderDragDrop : EditorTabBaseDragDrop
{
    internal EditorTabOnlyReorderDragDrop(EditorPanel panel, EditorTabPanel originatingTabPanel)
        : base(panel, originatingTabPanel)
    {
    }
}

public sealed class EditorTabDragDrop : EditorTabBaseDragDrop
{
    internal EditorTabDragDrop(EditorPanel panel, EditorTabPanel originatingTabPanel) : base(panel, originatingTabPanel)
    {
    }

    public override void Drop()
    {
        var (window, docker) = EditorWindowDocker.Create();
        docker.AddPanel(Panel);
        window.Show();
    }

    public override void AfterDrop()
    {
        OriginatingTabPanel.ConfirmRemoval();
    }
}
