using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Base class for display-only text controls that support selection and clipboard copy.
    /// </summary>
    [Virtual]
    public abstract class SelectableTextControl : Control
    {
        /// <summary>
        ///     Style property used for selection highlight color.
        /// </summary>
        public const string StylePropertySelectionColor = TextEdit.StylePropertySelectionColor;

        private readonly TextSelectionHelper _selection = new();
        private bool _copyable;

        /// <summary>
        ///     If true, allows selecting and copying text from this control.
        /// </summary>
        public bool Copyable
        {
            get => _copyable;
            set
            {
                if (_copyable == value)
                    return;

                _copyable = value;
                if (_copyable)
                {
                    CanKeyboardFocus = true;
                    KeyboardFocusOnClick = true;
                    DefaultCursorShape = CursorShape.IBeam;
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
        ///     Provides layout-specific selection and hit-testing for this control.
        /// </summary>
        protected abstract ISelectableTextLayout SelectionLayout { get; }

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

            var color = StylePropertyDefault(StylePropertySelectionColor, Color.CornflowerBlue.WithAlpha(0.25f));
            SelectionLayout.DrawSelection(handle, _selection.SelectionLower, _selection.SelectionUpper, color);
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

            if (args.Function == EngineKeyFunctions.TextCopy)
            {
                if (!HasKeyboardFocus())
                    return;

                var text = SelectionLayout.GetTextSpan();
                if (text.Length == 0)
                    return;

                var clipboard = IoCManager.Resolve<IClipboardManager>();
                _selection.CopySelectionOrAll(clipboard, text);
                args.Handle();
                return;
            }

            if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.TextCursorSelect)
                return;

            if (!HasKeyboardFocus() && args.Function != EngineKeyFunctions.UIClick)
                return;

            var pos = ClampSelectionPosition(args.RelativePosition);
            var index = SelectionLayout.GetIndexAtPosition(pos);
            _selection.BeginSelection(index, args.Function == EngineKeyFunctions.TextCursorSelect);
            args.Handle();
        }

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (!Copyable)
                return;

            if (args.Function == EngineKeyFunctions.UIClick || args.Function == EngineKeyFunctions.TextCursorSelect)
                _selection.EndSelection();
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (!Copyable || !_selection.IsSelecting)
                return;

            var pos = ClampSelectionPosition(args.RelativePosition);
            var index = SelectionLayout.GetIndexAtPosition(pos);
            _selection.UpdateSelection(index);
        }

        protected internal override void KeyboardFocusExited()
        {
            base.KeyboardFocusExited();
            _selection.ClearSelection();
        }
    }
}
