namespace Robust.Client.UserInterface;

public abstract class DragDropOperation
{
    /// <summary>
    /// Called if no control accepted this drag drop operation. Useful as a fallback path.
    /// </summary>
    public virtual void Drop()
    {
    }

    public virtual void AfterDrop()
    {

    }
}
