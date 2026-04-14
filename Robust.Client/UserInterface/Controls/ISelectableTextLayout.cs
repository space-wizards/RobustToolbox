using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Adapter for selection and hit-testing logic for selectable text controls.
    /// </summary>
    public interface ISelectableTextLayout
    {
        /// <summary>
        ///     Returns the plain text content used for selection and copy.
        /// </summary>
        ReadOnlySpan<char> GetTextSpan();

        /// <summary>
        ///     Maps a position in control-relative coordinates to a UTF-16 text index.
        /// </summary>
        int GetIndexAtPosition(Vector2 relativePosition);

        /// <summary>
        ///     Draws a selection highlight for the given range.
        /// </summary>
        void DrawSelection(DrawingHandleScreen handle, int selectionLower, int selectionUpper, Color color);
    }
}
