using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface;

internal partial class UserInterfaceManager
{
    private float _tooltipTimer;
    private ICursor? _worldCursor;
    private bool _needUpdateActiveCursor;
    [ViewVariables] public Control? KeyboardFocused { get; private set; }

    [ViewVariables] public Control? CurrentlyHovered { get; private set; } = default!;

    private Control? _controlFocused;
    [ViewVariables]
    public Control? ControlFocused
    {
        get => _controlFocused;
        set
        {
            if (_controlFocused == value)
                return;
            _controlFocused?.ControlFocusExited();
            _controlFocused = value;
        }
    }

    // set to null when not counting down
    private float? _tooltipDelay;
    private Tooltip _tooltip = default!;
    private bool showingTooltip;
    private Control? _suppliedTooltip;
    private const float TooltipDelay = 1;

    private WindowRoot? _focusedRoot;

    private static (Control control, Vector2 rel)? MouseFindControlAtPos(Control control, Vector2 position)
    {
        for (var i = control.ChildCount - 1; i >= 0; i--)
        {
            var child = control.GetChild(i);
            if (!child.Visible || child.RectClipContent && !child.PixelRect.Contains((Vector2i) position))
            {
                continue;
            }

            var maybeFoundOnChild = MouseFindControlAtPos(child, position - child.PixelPosition);
            if (maybeFoundOnChild != null)
            {
                return maybeFoundOnChild;
            }
        }

        if (control.MouseFilter != Control.MouseFilterMode.Ignore && control.HasPoint(position / control.UIScale))
        {
            return (control, position);
        }

        return null;
    }

    public void KeyBindDown(BoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.CloseModals && _modalStack.Count != 0)
        {
            bool closedAny = false;
            for (var i = _modalStack.Count - 1; i >= 0; i--)
            {
                var top = _modalStack[i];

                if (top is not Popup {CloseOnEscape: false})
                {
                    RemoveModal(top);
                    closedAny = true;
                }
            }

            if (closedAny)
            {
                args.Handle();
            }
            return;
        }

        var control = ControlFocused ?? KeyboardFocused ?? MouseGetControl(args.PointerLocation);

        if (control == null)
        {
            return;
        }

        var guiArgs = new GUIBoundKeyEventArgs(args.Function, args.State, args.PointerLocation, args.CanFocus,
            args.PointerLocation.Position / control.UIScale - control.GlobalPosition,
            args.PointerLocation.Position - control.GlobalPixelPosition);

        _doGuiInput(control, guiArgs, (c, ev) => c.KeyBindDown(ev));

        if (guiArgs.Handled)
        {
            args.Handle();
        }
    }

    public void KeyBindUp(BoundKeyEventArgs args)
    {
        var control = ControlFocused ?? KeyboardFocused ?? MouseGetControl(args.PointerLocation);
        if (control == null)
        {
            return;
        }

        var guiArgs = new GUIBoundKeyEventArgs(args.Function, args.State, args.PointerLocation, args.CanFocus,
            args.PointerLocation.Position / control.UIScale - control.GlobalPosition,
            args.PointerLocation.Position - control.GlobalPixelPosition);

        _doGuiInput(control, guiArgs, (c, ev) => c.KeyBindUp(ev));

        // Always mark this as handled.
        // The only case it should not be is if we do not have a control to click on,
        // in which case we never reach this.
        args.Handle();
    }

    public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
    {
        _resetTooltipTimer();
        // Update which control is considered hovered.
        var newHovered = MouseGetControl(mouseMoveEventArgs.Position);
        if (newHovered != CurrentlyHovered)
        {
            _clearTooltip();
            CurrentlyHovered?.MouseExited();
            CurrentlyHovered = newHovered;
            CurrentlyHovered?.MouseEntered();
            if (CurrentlyHovered != null)
            {
                _tooltipDelay = CurrentlyHovered.TooltipDelay ?? TooltipDelay;
            }
            else
            {
                _tooltipDelay = null;
            }

            _needUpdateActiveCursor = true;
        }

        var target = ControlFocused ?? newHovered;
        if (target != null)
        {
            var pos = mouseMoveEventArgs.Position.Position;
            var guiArgs = new GUIMouseMoveEventArgs(mouseMoveEventArgs.Relative / target.UIScale,
                target,
                pos / target.UIScale, mouseMoveEventArgs.Position,
                pos / target.UIScale - target.GlobalPosition,
                pos - target.GlobalPixelPosition);

            _doMouseGuiInput(target, guiArgs, (c, ev) => c.MouseMove(ev));
        }
    }

    private void UpdateActiveCursor()
    {
        // Consider mouse input focus first so that dragging windows don't act up etc.
        var cursorTarget = ControlFocused ?? CurrentlyHovered;

        if (cursorTarget == null)
        {
            _clyde.SetCursor(_worldCursor);
            return;
        }

        if (cursorTarget.CustomCursorShape != null)
        {
            _clyde.SetCursor(cursorTarget.CustomCursorShape);
            return;
        }

        var shape = cursorTarget.DefaultCursorShape switch
        {
            Control.CursorShape.Arrow => StandardCursorShape.Arrow,
            Control.CursorShape.IBeam => StandardCursorShape.IBeam,
            Control.CursorShape.Hand => StandardCursorShape.Hand,
            Control.CursorShape.Crosshair => StandardCursorShape.Crosshair,
            Control.CursorShape.VResize => StandardCursorShape.VResize,
            Control.CursorShape.HResize => StandardCursorShape.HResize,
            _ => StandardCursorShape.Arrow
        };

        _clyde.SetCursor(_clyde.GetStandardCursor(shape));
    }

    public void MouseWheel(MouseWheelEventArgs args)
    {
        var control = MouseGetControl(args.Position);
        if (control == null)
        {
            return;
        }

        args.Handle();

        var pos = args.Position.Position;

        var guiArgs = new GUIMouseWheelEventArgs(args.Delta, control,
            pos / control.UIScale, args.Position,
            pos / control.UIScale - control.GlobalPosition, pos - control.GlobalPixelPosition);

        _doMouseGuiInput(control, guiArgs, (c, ev) => c.MouseWheel(ev), true);
    }

    public void TextEntered(TextEnteredEventArgs textEnteredEvent)
    {
        if (KeyboardFocused == null)
        {
            return;
        }

        var guiArgs = new GUITextEnteredEventArgs(KeyboardFocused, textEnteredEvent);
        KeyboardFocused.TextEntered(guiArgs);
    }

    public void TextEditing(TextEditingEventArgs textEvent)
    {
        if (KeyboardFocused == null)
        {
            return;
        }

        var guiArgs = new GUITextEditingEventArgs(KeyboardFocused, textEvent);
        KeyboardFocused.TextEditing(guiArgs);
    }

    public ScreenCoordinates MousePositionScaled => ScreenToUIPosition(_inputManager.MouseScreenPosition);

    private static void _doMouseGuiInput<T>(Control? control, T guiEvent, Action<Control, T> action,
        bool ignoreStop = false)
        where T : GUIMouseEventArgs
    {
        while (control != null)
        {
            guiEvent.SourceControl = control;
            if (control.MouseFilter != Control.MouseFilterMode.Ignore)
            {
                action(control, guiEvent);

                if (guiEvent.Handled || (!ignoreStop && control.MouseFilter == Control.MouseFilterMode.Stop))
                {
                    break;
                }
            }

            guiEvent.RelativePosition += control.Position;
            guiEvent.RelativePixelPosition += control.PixelPosition;
            control = control.Parent;
        }
    }

    private static void _doGuiInput(
        Control? control,
        GUIBoundKeyEventArgs guiEvent,
        Action<Control, GUIBoundKeyEventArgs> action,
        bool ignoreStop = false)
    {
        while (control != null)
        {
            if (control.MouseFilter != Control.MouseFilterMode.Ignore)
            {
                action(control, guiEvent);

                if (guiEvent.Handled || (!ignoreStop && control.MouseFilter == Control.MouseFilterMode.Stop))
                {
                    break;
                }
            }

            guiEvent.RelativePosition += control.Position;
            guiEvent.RelativePixelPosition += control.PixelPosition;
            control = control.Parent;
        }
    }

    private void _clearTooltip()
    {
        if (!showingTooltip) return;
        _tooltip.Visible = false;
        if (_suppliedTooltip != null)
        {
            PopupRoot.RemoveChild(_suppliedTooltip);
            _suppliedTooltip = null;
        }

        CurrentlyHovered?.PerformHideTooltip();
        _resetTooltipTimer();
        showingTooltip = false;
    }

    public void CursorChanged(Control control)
    {
        if (control == ControlFocused || control == CurrentlyHovered)
        {
            _needUpdateActiveCursor = true;
        }
    }

    public void HideTooltipFor(Control control)
    {
        if (CurrentlyHovered == control)
        {
            _clearTooltip();
        }
    }

    public bool HandleCanFocusDown(
        ScreenCoordinates pointerPosition,
        [NotNullWhen(true)] out (Control control, Vector2i rel)? hitData)
    {
        var hit = MouseGetControlAndRel(pointerPosition);
        var pos = pointerPosition.Position;

        // If we have a modal open and the mouse down was outside it, close said modal.
        for (var i = _modalStack.Count - 1; i >= 0; i--)
        {
            var top = _modalStack[i];
            var offset = pos - top.GlobalPixelPosition;
            if (!top.HasPoint(offset / top.UIScale))
            {
                if (top.MouseFilter != Control.MouseFilterMode.Stop)
                {
                    if (top is not Popup {CloseOnClick: false})
                    {
                        RemoveModal(top);
                    }
                }
                else
                {
                    ControlFocused = top;
                    hitData = null;
                    return false; // prevent anything besides the top modal control from receiving input
                }
            }
            else
            {
                break;
            }
        }


        if (hit == null)
        {
            ReleaseKeyboardFocus();
            hitData = null;
            return false;
        }

        var (control, rel) = hit.Value;

        if (control != KeyboardFocused)
        {
            ReleaseKeyboardFocus();
        }

        ControlFocused = control;

        if (ControlFocused.CanKeyboardFocus && ControlFocused.KeyboardFocusOnClick)
        {
            ControlFocused.GrabKeyboardFocus();
        }

        hitData = (control, (Vector2i) rel);
        return true;
    }

    public void HandleCanFocusUp()
    {
        ControlFocused = null;
    }

    public ScreenCoordinates ScreenToUIPosition(ScreenCoordinates coordinates)
    {
        if (!_windowsToRoot.TryGetValue(coordinates.Window, out var root))
            return default;

        return new ScreenCoordinates(coordinates.Position / root.UIScale, coordinates.Window);
    }

    public ICursor? WorldCursor
    {
        get => _worldCursor;
        set
        {
            _worldCursor = value;
            _needUpdateActiveCursor = true;
        }
    }

    private (Control control, Vector2 rel)? MouseGetControlAndRel(ScreenCoordinates coordinates)
    {
        if (!_windowsToRoot.TryGetValue(coordinates.Window, out var root))
            return null;

        return MouseFindControlAtPos(root, coordinates.Position);
    }

    public Control? MouseGetControl(ScreenCoordinates coordinates)
    {
        return MouseGetControlAndRel(coordinates)?.control;
    }

    public Control? GetSuppliedTooltipFor(Control control)
    {
        return CurrentlyHovered == control ? _suppliedTooltip : null;
    }
    /// <summary>
    ///     Converts
    /// </summary>
    /// <param name="args">Event data values for a bound key state change.</param>

    private bool OnUIKeyBindStateChanged(BoundKeyEventArgs args)
    {
        if (args.State == BoundKeyState.Down)
        {
            KeyBindDown(args);
        }
        else
        {
            KeyBindUp(args);
        }

        // If we are in a focused control or doing a CanFocus, return true
        // So that InputManager doesn't propagate events to simulation.
        if (!args.CanFocus && KeyboardFocused != null)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void GrabKeyboardFocus(Control control)
    {
        if (control == null)
        {
            throw new ArgumentNullException(nameof(control));
        }

        if (!control.CanKeyboardFocus)
        {
            throw new ArgumentException("Control cannot get keyboard focus.", nameof(control));
        }

        if (control == KeyboardFocused)
        {
            return;
        }

        ReleaseKeyboardFocus();

        KeyboardFocused = control;

        KeyboardFocused.KeyboardFocusEntered();
    }

    public void ReleaseKeyboardFocus()
    {
        var oldFocused = KeyboardFocused;
        oldFocused?.KeyboardFocusExited();
        KeyboardFocused = null;
    }

    public void ReleaseKeyboardFocus(Control ifControl)
    {
        if (ifControl == null)
        {
            throw new ArgumentNullException(nameof(ifControl));
        }

        if (ifControl == KeyboardFocused)
        {
            ReleaseKeyboardFocus();
        }
    }

    private void _resetTooltipTimer()
    {
        _tooltipTimer = 0;
    }

    private void _showTooltip()
    {
        if (showingTooltip) return;
        showingTooltip = true;
        var hovered = CurrentlyHovered;
        if (hovered == null)
        {
            return;
        }

        // show supplied tooltip if there is one
        if (hovered.TooltipSupplier != null)
        {
            _suppliedTooltip = hovered.TooltipSupplier.Invoke(hovered);
            if (_suppliedTooltip != null)
            {
                PopupRoot.AddChild(_suppliedTooltip);
                Tooltips.PositionTooltip(_suppliedTooltip);
            }
        }
        else if (!String.IsNullOrWhiteSpace(hovered.ToolTip))
        {
            // show simple tooltip if there is one
            _tooltip.Visible = true;
            _tooltip.Text = hovered.ToolTip;
            Tooltips.PositionTooltip(_tooltip);
        }

        hovered.PerformShowTooltip();
    }

    public Vector2? CalcRelativeMousePositionFor(Control control, ScreenCoordinates mousePosScaled)
    {
        var (pos, window) = mousePosScaled;
        var root = control.Root;

        if (root?.Window == null || root.Window.Id != window)
            return null;

        return pos - control.GlobalPosition;
    }
}
