using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

// TODO: Events need more useful fields.

public abstract class BaseDragEventArgs
{
    public DragDropOperation Operation { get; internal set; }

    public BaseDragEventArgs(DragDropOperation operation)
    {
        Operation = operation;
    }
}

public sealed class DragEnterEventArgs : BaseDragEventArgs
{
    public DragEnterEventArgs(DragDropOperation operation) : base(operation)
    {
    }
}

public sealed class DragLeaveEventArgs : BaseDragEventArgs
{
    public DragLeaveEventArgs(DragDropOperation operation) : base(operation)
    {
    }
}

public sealed class DragDropEventArgs : BaseDragEventArgs
{
    public Vector2 RelativePosition { get; }

    public DragDropEventArgs(DragDropOperation operation, Vector2 relativePosition) : base(operation)
    {
        RelativePosition = relativePosition;
    }

    public bool Handled { get; private set; }

    public void Handle()
    {
        Handled = true;
    }
}

public sealed class DragMoveEventArgs : BaseDragEventArgs
{
    public DragMoveEventArgs(DragDropOperation operation) : base(operation)
    {
    }
}

public partial class Control
{
    public event Action<DragEnterEventArgs>? OnDragEnter;
    public event Action<DragLeaveEventArgs>? OnDragLeave;
    public event Action<DragDropEventArgs>? OnDragDrop;
    public event Action<DragMoveEventArgs>? OnDragMove;

    public virtual void DragEnter(DragEnterEventArgs eventArgs)
    {
        OnDragEnter?.Invoke(eventArgs);
    }

    public virtual void DragLeave(DragLeaveEventArgs eventArgs)
    {
        OnDragLeave?.Invoke(eventArgs);
    }

    public virtual void DragDrop(DragDropEventArgs eventArgs)
    {
        OnDragDrop?.Invoke(eventArgs);
    }

    public virtual void DragMove(DragMoveEventArgs eventArgs)
    {
        OnDragMove?.Invoke(eventArgs);
    }
}
