namespace Robust.Client.Editor.Interface;

public abstract class EditorPanelScope
{
    public virtual bool CanDock(EditorPanelScope? otherScope)
    {
        return true;
    }

    public virtual bool CanDockInto(EditorPanelScope? otherScope)
    {
        return true;
    }

    public static bool DockCheck(EditorPanelScope? targetScope, EditorPanelScope? panelScope)
    {
        if (targetScope != null)
        {
            if (!targetScope.CanDock(panelScope))
                return false;
        }

        if (panelScope != null)
        {
            if (!panelScope.CanDockInto(targetScope))
                return false;
        }

        return true;
    }
}
