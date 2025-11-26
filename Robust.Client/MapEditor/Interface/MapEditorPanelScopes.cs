using Robust.Client.Editor.Interface;

namespace Robust.Client.MapEditor.Interface;

internal sealed class MapEditorFilePanelScope : EditorPanelScope;

internal sealed class MapEditorInnerScope : EditorPanelScope
{
    public override bool CanDock(EditorPanelScope? otherScope)
    {
        // Do not allow files to dock into files.
        if (otherScope is MapEditorFilePanelScope)
            return false;

        return base.CanDock(otherScope);
    }

    public override bool CanDockInto(EditorPanelScope? otherScope)
    {
        // Cannot dock into the top-level file panel.
        if (otherScope is MapEditorFilePanelScope)
            return false;

        // Cannot dock into the tab for another file.
        if (otherScope is MapEditorInnerScope otherInner && otherInner != this)
            return false;

        // Anything else goes.
        return base.CanDockInto(otherScope);
    }
}
