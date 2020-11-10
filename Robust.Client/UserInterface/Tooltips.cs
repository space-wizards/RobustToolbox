using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface
{
    /// <summary>
    /// Utilities for working with tooltips.
    /// </summary>
    public static class Tooltips
    {

        /// <summary>
        /// Positions the provided control as a tooltip within the bounds of its parent UserInterfaceManager screen
        /// under the current mouse position. Sizing Based on its current combined minimum size.
        /// Defaults to the top left corner
        /// of the control being placed at the mouse position but
        /// adjusting to a different corner if the control would go beyond the edge of the bounds.
        /// </summary>
        /// <param name="tooltip">control to position (current size will be used to determine bounds)</param>
        public static void PositionTooltip(Control tooltip)
        {
            PositionTooltip(tooltip.UserInterfaceManager.RootControl.Size,
                tooltip.UserInterfaceManager.MousePositionScaled,
                tooltip);
        }

        /// <summary>
        /// Positions the provided control as a tooltip within the provided screenBounds based on its current
        /// combined minimum size.
        /// Defaults to the top left corner
        /// of the control being placed at the indicated position but
        /// adjusting to a different corner if the control would go beyond the edge of the bounds.
        /// </summary>
        /// <param name="screenBounds">max x and y screen coordinates for the tooltip to occupy, tooltip
        /// will be shifted to avoid exceeding these bounds.</param>
        /// <param name="screenPosition">position to place the tooltip at, in screen coordinates</param>
        /// <param name="tooltip">control to position (current size will be used to determine bounds)</param>
        public static void PositionTooltip(Vector2 screenBounds, Vector2 screenPosition, Control tooltip)
        {
            LayoutContainer.SetPosition(tooltip, screenPosition);

            var combinedMinSize = tooltip.CombinedMinimumSize;
            var (right, bottom) = tooltip.Position + combinedMinSize;

            if (right > screenBounds.X)
            {
                LayoutContainer.SetPosition(tooltip, (screenPosition.X - combinedMinSize.X, tooltip.Position.Y));
            }

            if (bottom > screenBounds.Y)
            {
                LayoutContainer.SetPosition(tooltip, (tooltip.Position.X, screenPosition.Y - combinedMinSize.Y));
            }
        }
    }
}
