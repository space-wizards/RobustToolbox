using System;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Shared helper for non-editable text selection state and clipboard copy logic.
    /// </summary>
    internal sealed class TextSelectionHelper
    {
        private int _selectionAnchor;
        private int _selectionCursor;

        /// <summary>
        ///     True while a drag-select operation is ongoing.
        /// </summary>
        public bool IsSelecting { get; private set; }

        /// <summary>
        ///     Fixed end of the selection range.
        /// </summary>
        public int SelectionAnchor
        {
            get => _selectionAnchor;
            set => _selectionAnchor = Math.Max(0, value);
        }

        /// <summary>
        ///     Moving end of the selection range.
        /// </summary>
        public int SelectionCursor
        {
            get => _selectionCursor;
            set => _selectionCursor = Math.Max(0, value);
        }

        /// <summary>
        ///     True if the selection range is non-empty.
        /// </summary>
        public bool HasSelection => _selectionAnchor != _selectionCursor;

        /// <summary>
        ///     Lower bound of the selection range.
        /// </summary>
        public int SelectionLower => Math.Min(_selectionAnchor, _selectionCursor);

        /// <summary>
        ///     Upper bound of the selection range.
        /// </summary>
        public int SelectionUpper => Math.Max(_selectionAnchor, _selectionCursor);

        /// <summary>
        ///     Starts a selection drag, optionally preserving the anchor for shift-select style behavior.
        /// </summary>
        public void BeginSelection(int index, bool preserveAnchor)
        {
            if (!preserveAnchor)
                _selectionAnchor = index;

            _selectionCursor = index;
            IsSelecting = true;
        }

        /// <summary>
        ///     Updates the moving end of the selection range.
        /// </summary>
        public void UpdateSelection(int index)
        {
            _selectionCursor = index;
        }

        /// <summary>
        ///     Ends a selection drag without clearing the current range.
        /// </summary>
        public void EndSelection()
        {
            IsSelecting = false;
        }

        /// <summary>
        ///     Clears the current selection range and ends any active drag.
        /// </summary>
        public void ClearSelection()
        {
            _selectionAnchor = 0;
            _selectionCursor = 0;
            IsSelecting = false;
        }

        /// <summary>
        ///     Returns the selected text, or null if the selection is empty.
        /// </summary>
        public string? GetSelectedText(ReadOnlySpan<char> text)
        {
            if (!HasSelection)
                return null;

            var lower = MathHelper.Clamp(SelectionLower, 0, text.Length);
            var upper = MathHelper.Clamp(SelectionUpper, 0, text.Length);
            if (upper <= lower)
                return null;

            return new string(text[lower..upper]);
        }

        /// <summary>
        ///     Copies the selection when present, otherwise copies the full text.
        /// </summary>
        public void CopySelectionOrAll(IClipboardManager clipboard, ReadOnlySpan<char> text)
        {
            if (text.Length == 0)
                return;

            var selected = GetSelectedText(text);
            if (selected != null)
            {
                clipboard.SetText(selected);
                return;
            }

            clipboard.SetText(new string(text));
        }
    }
}
