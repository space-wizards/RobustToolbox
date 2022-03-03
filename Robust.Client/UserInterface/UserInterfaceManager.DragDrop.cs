using System.Collections.Generic;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Robust.Client.UserInterface;

public delegate DragDropDetectResult DragDropDetected(GUIBoundKeyEventArgs eventArgs);

/// <summary>
/// Represents the result of a drag-drop detection.
/// This can either cancel the op or properly start it.
/// </summary>
public struct DragDropDetectResult
{
    public static DragDropDetectResult Nothing => default;

    internal (Control source, DragDropOperation op)? Result;

    public static DragDropDetectResult Start(Control sourceControl, DragDropOperation operation)
    {
        return new() { Result = (sourceControl, operation) };
    }
}

internal sealed partial class UserInterfaceManager
{
    // The general flow of a drag-drop operation is as such:
    // A user does an input (probably UIClick) and the control says "this DETECTS a drag operation"
    //   This does not do anything on its own.
    // If the user then moves their mouse > ui.drag_threshold pixels from the place they pressed down,
    // the drag operation will properly "start".
    // Then, standard drag things happen: controls get told DragEnter, DragLeave, DragMove,
    // until the user releases the button and the control gets "dropped" on something.

    // CVar ui.drag_threshold ^ 2
    private float _dragThresholdSquared;

    private DragState _dragState;
    private ScreenCoordinates _dragStart;
    private BoundKeyFunction _dragFunction;
    private DragDropDetected? _dragDetectedHandler;
    private GUIBoundKeyEventArgs? _dragDetectStartEvent;

    private DragDropOperation? _dragOperation;

    // Controls tree "slice" for the current set of controls we're over.
    // [0] is tree leaf, [count] is UI root.
    private readonly List<Control> _currentlyDraggingOver = new();

    private void StartDragDetect(GUIBoundKeyEventArgs eventArgs)
    {
        if (eventArgs.OnDragDropDetected == null)
            return;

        if (_dragState != DragState.None)
        {
            _sawmill.Warning("Tried to start drag detection, " +
                             "but we already have another drag operation/detection in progress. Ignoring");
            return;
        }

        _dragDetectStartEvent = eventArgs;
        _dragDetectedHandler = eventArgs.OnDragDropDetected;
        // TODO: DPI.
        _dragStart = eventArgs.PointerLocation;
        _dragFunction = eventArgs.Function;
        _dragState = DragState.Detecting;
    }

    private void EndDragKeyUp(BoundKeyEventArgs eventArgs)
    {
        if (_dragFunction != eventArgs.Function)
            return;

        // End any in-progress drags or drag detections, we got that key up.
        switch (_dragState)
        {
            case DragState.Dragging:
                _sawmill.Debug("Ending drag");

                // TODO: Clean up this coordinate conversion code. This is crap.
                var uiScale = _currentlyDraggingOver[0].UIScale;
                var scaledPos = eventArgs.PointerLocation.Position / uiScale;

                var handled = false;

                var i = 0;
                for (; i < _currentlyDraggingOver.Count; i++)
                {
                    var control = _currentlyDraggingOver[i];
                    // TODO: This is O(n^2).
                    var relative = scaledPos - control.GlobalPosition;
                    var dropArgs = new DragDropEventArgs(_dragOperation!, relative);

                    control.DragDrop(dropArgs);

                    if (dropArgs.Handled)
                    {
                        handled = true;
                        break;
                    }
                }

                if (!handled)
                    _dragOperation!.Drop();

                // Go over remaining controls not dropped onto, telling them DragLeave.
                for (i++; i < _currentlyDraggingOver.Count; i++)
                {
                    var control = _currentlyDraggingOver[i];
                    control.DragLeave(new DragLeaveEventArgs(_dragOperation!));
                }

                _currentlyDraggingOver.Clear();
                _dragOperation = null;
                goto case DragState.Detecting;

            case DragState.Detecting:
                _sawmill.Debug("Ending drag detection");
                CancelDragDetect();
                break;
        }

        _dragState = DragState.None;
    }

    private void CancelDragDetect()
    {
        _dragDetectedHandler = null;
        _dragDetectStartEvent = null;
    }

    private void MouseMoveCheckDrag(MouseMoveEventArgs mouseMoveEventArgs)
    {
        switch (_dragState)
        {
            case DragState.Detecting:
            {
                // TODO: DPI.
                var diff = _dragStart.Position - mouseMoveEventArgs.Position.Position;
                var startDrag = _dragStart.Window != mouseMoveEventArgs.Position.Window
                                || diff.LengthSquared > _dragThresholdSquared;

                if (!startDrag)
                    return;

                // Start the drag, or at least try to if the control wants.
                var result = _dragDetectedHandler!(_dragDetectStartEvent!);

                if (result.Result == null)
                {
                    _sawmill.Debug("Drag cancelled");
                    CancelDragDetect();
                    return;
                }

                _sawmill.Debug("Starting drag");
                var (srcControl, op) = result.Result.Value;

                _dragOperation = op;

                var eventArgs = new DragEnterEventArgs(op);

                while (srcControl != null)
                {
                    srcControl.DragEnter(eventArgs);

                    _currentlyDraggingOver.Add(srcControl);

                    srcControl = srcControl.Parent;
                }

                _dragState = DragState.Dragging;

                // No return, fall into rest of the method which checks enter/leave
                // if the mouse already left the start control.

                goto case DragState.Dragging;
            }
            case DragState.Dragging:
            {
                // TODO: DPI?
                var curControl = MouseGetControl(mouseMoveEventArgs.Position);
                _sawmill.Debug($"A: {Control.GetDebugPath(curControl)}");
                var newOverSet = new HashSet<Control>();
                var oldOverSet = new HashSet<Control>(_currentlyDraggingOver);

                for (var newControl = curControl; newControl != null; newControl = newControl.Parent)
                {
                    newOverSet.Add(newControl);
                }

                var leaveEvent = new DragLeaveEventArgs(_dragOperation!);
                foreach (var oldOver in _currentlyDraggingOver)
                {
                    if (newOverSet.Contains(oldOver))
                        continue;

                    oldOver.DragLeave(leaveEvent);
                }

                _currentlyDraggingOver.Clear();

                var enterEvent = new DragEnterEventArgs(_dragOperation!);
                var moveEvent = new DragMoveEventArgs(_dragOperation!);

                for (var newControl = curControl; newControl != null; newControl = newControl.Parent)
                {
                    _currentlyDraggingOver.Add(newControl);

                    if (!oldOverSet.Contains(newControl))
                        newControl.DragEnter(enterEvent);

                    newControl.DragMove(moveEvent);
                }

                break;
            }
        }
    }

    private enum DragState : byte
    {
        None = 0,
        Detecting,
        Dragging
    }
}
