using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Shared helper for emitting selection rectangles while iterating text layout line-by-line.
    /// </summary>
    /// <remarks>
    ///     Initializes a new tracker for the given selection range.
    /// </remarks>
    internal struct TextSelectionLineTracker(int selectionLower, int selectionUpper)
    {
        private readonly int _selectionLower = selectionLower;
        private readonly int _selectionUpper = selectionUpper;

        private int _lineStartIndex = 0;
        private float _lineStartX = 0;
        private float _lineTop = 0;
        private float _lineBottom = 0;

        private float? _startX = null;
        private float? _endX = null;

        /// <summary>
        ///     Starts tracking a new line segment.
        /// </summary>
        public void BeginLine(int lineStartIndex, float lineStartX, float lineTop, float lineBottom)
        {
            _lineStartIndex = lineStartIndex;
            _lineStartX = lineStartX;
            _lineTop = lineTop;
            _lineBottom = lineBottom;

            _startX = _selectionLower < lineStartIndex && _selectionUpper > lineStartIndex ? lineStartX : null;
            _endX = null;
        }

        /// <summary>
        ///     Updates the line bounds when the cursor reaches a selection boundary.
        /// </summary>
        public void UpdateForIndex(int textIndex, float x)
        {
            if (textIndex == _selectionLower)
                _startX = x;

            if (textIndex == _selectionUpper)
                _endX = x;
        }

        /// <summary>
        ///     Emits the selection rectangle for the current line if needed.
        /// </summary>
        public void FinishLine(DrawingHandleScreen handle, Color color, int lineEndIndex, float lineEndX)
        {
            if (_selectionUpper > _lineStartIndex && _selectionLower < lineEndIndex)
            {
                _startX ??= _lineStartX;
                _endX ??= lineEndX;
            }

            if (_startX.HasValue && _endX.HasValue && _endX.Value > _startX.Value)
            {
                handle.DrawRect(new UIBox2(_startX.Value, _lineTop, _endX.Value, _lineBottom), color);
            }
        }
    }
}
