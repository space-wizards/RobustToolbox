using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
///     Base class for display-only text controls that support selection and clipboard copy.
/// </summary>
public abstract partial class SelectableTextControl : Control
{
    /// <summary>
    ///     Style property used for selection highlight color.
    /// </summary>
    public const string StylePropertySelectionColor = TextEdit.StylePropertySelectionColor;
    public static readonly Color DefaultSelectionColor = Color.CornflowerBlue.WithAlpha(0.25f);

    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly TextSelectionHelper _selection = new();
    private readonly TextEditShared.DoubleClickState _doubleClick = new();

    /// <summary>
    ///     If true, allows selecting and copying text from this control.
    /// </summary>
    public bool Copyable
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            if (field)
            {
                CanKeyboardFocus = true;
                KeyboardFocusOnClick = true;
            }
        }
    }

    /// <summary>
    ///     True while a drag-select operation is ongoing.
    /// </summary>
    protected bool IsSelecting => _selection.IsSelecting;

    /// <summary>
    ///     Lower bound of the current selection.
    /// </summary>
    protected int SelectionLower => _selection.SelectionLower;

    /// <summary>
    ///     Upper bound of the current selection.
    /// </summary>
    protected int SelectionUpper => _selection.SelectionUpper;

    /// <summary>
    ///     Returns the plain text content used for selection and copy.
    /// </summary>
    protected abstract ReadOnlySpan<char> GetTextSpan();

    /// <summary>
    ///     Maps a position in control-relative coordinates to a UTF-16 text index.
    /// </summary>
    protected abstract int GetIndexAtPosition(Vector2 relativePosition);

    /// <summary>
    ///     Draws a selection highlight for the given range.
    /// </summary>
    protected abstract void DrawSelectionRange(DrawingHandleScreen handle, int selectionLower, int selectionUpper, Color color);

    private void SelectWord(int index)
    {
        _selection.SelectWord(GetTextSpan().ToString(), index);
    }

    /// <summary>
    ///     Clears selection state after content changes or focus loss.
    /// </summary>
    protected void ClearSelection()
    {
        _selection.ClearSelection();
    }

    /// <summary>
    ///     Draws the selection highlight when active.
    /// </summary>
    protected void DrawSelectionIfNeeded(DrawingHandleScreen handle)
    {
        if (!Copyable || !_selection.HasSelection)
            return;

        var color = StylePropertyDefault(StylePropertySelectionColor, DefaultSelectionColor);
        DrawSelectionRange(handle, _selection.SelectionLower, _selection.SelectionUpper, color);
    }

    /// <summary>
    ///     Clamps selection positions to keep drag-select stable at the control edges.
    /// </summary>
    protected virtual Vector2 ClampSelectionPosition(Vector2 relativePosition)
    {
        var pos = relativePosition;
        if (pos.Y < 0)
            pos = new Vector2(0, 0);
        else if (pos.Y > Size.Y)
            pos = new Vector2(Size.X, Size.Y);

        pos.X = MathHelper.Clamp(pos.X, 0, Size.X);
        return pos;
    }

    protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (!Copyable)
            return;

        // Copy selection to clipboard.
        if (args.Function == EngineKeyFunctions.TextCopy)
        {
            if (!HasKeyboardFocus())
                return;

            var text = GetTextSpan();
            if (text.Length == 0)
                return;

            var clipboard = IoCManager.Resolve<IClipboardManager>();
            _selection.CopySelectionOrAll(clipboard, text);
            args.Handle();
            return;
        }

        // Any non-copy input should not trap gameplay input.
        if (_selection.HasSelection)
        {
            _selection.ClearSelection();
            if (HasKeyboardFocus())
                ReleaseKeyboardFocus();
        }

        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.TextCursorSelect)
            return;

        if (!HasKeyboardFocus() && args.Function != EngineKeyFunctions.UIClick)
            return;

        var pos = ClampSelectionPosition(args.RelativePosition);
        var index = MathHelper.Clamp(GetIndexAtPosition(pos), 0, GetTextSpan().Length);
        if (args.Function == EngineKeyFunctions.UIClick && _doubleClick.Check(args.PointerLocation.Position, _timing.RealTime, _cfgManager))
        {
            SelectWord(index);
            args.Handle();
            return;
        }

        _selection.BeginSelection(index, args.Function == EngineKeyFunctions.TextCursorSelect);
        args.Handle();
    }

    protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (!Copyable)
            return;

        if (args.Function == EngineKeyFunctions.UIClick || args.Function == EngineKeyFunctions.TextCursorSelect)
        {
            _selection.EndSelection();

            // If there is no selection and we have focus, release it.
            if (!_selection.HasSelection && HasKeyboardFocus())
                ReleaseKeyboardFocus();
        }
    }

    protected internal override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!Copyable || !_selection.IsSelecting)
            return;

        var pos = ClampSelectionPosition(args.RelativePosition);
        var index = GetIndexAtPosition(pos);
        _selection.UpdateSelection(index);
    }

    protected internal override void KeyboardFocusExited()
    {
        base.KeyboardFocusExited();
        _selection.ClearSelection();
    }
}
