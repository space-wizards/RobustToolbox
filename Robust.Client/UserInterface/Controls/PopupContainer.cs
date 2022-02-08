using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Container that tries to put controls at their specified origin,
    ///     moving them around if necessary to avoid them going outside of the bounds of the container.
    /// </summary>
    [Virtual]
    public sealed class PopupContainer : Control
    {
        /// <summary>
        ///     The origin that the container tries to place the child at.
        /// </summary>
        public static readonly AttachedProperty PopupOriginProperty = AttachedProperty.Create("PopupOrigin",
            typeof(PopupContainer), typeof(Vector2), changed: PopupOriginChangedCallback);

        /// <summary>
        ///     Alternative position to right-align the popup if <see cref="PopupOriginProperty"/>
        ///     would put it off-screen horizontally.
        /// </summary>
        /// <remarks>
        ///     You know how right click menus with sub menus put the submenu on the left
        ///     if it's too close to the right of the screen? Yeah that.
        /// </remarks>
        public static readonly AttachedProperty AltOriginProperty = AttachedProperty.Create("AltOrigin",
            typeof(PopupContainer), typeof(Vector2?), changed: PopupOriginChangedCallback);

        public PopupContainer()
        {
            RectClipContent = true;
        }

        public static void SetPopupOrigin(Control control, Vector2 origin)
        {
            control.SetValue(PopupOriginProperty, origin);
        }

        public static Vector2 GetPopupOrigin(Control control)
        {
            return control.GetValue<Vector2>(PopupOriginProperty);
        }

        public static Vector2? GetAltOrigin(Control control)
        {
            return control.GetValue<Vector2?>(AltOriginProperty);
        }

        public static void SetAltOrigin(Control control, Vector2? origin)
        {
            control.SetValue(AltOriginProperty, origin);
        }

        private static void PopupOriginChangedCallback(Control owner, AttachedPropertyChangedEventArgs eventArgs)
        {
            if (owner.Parent is PopupContainer container)
            {
                container.InvalidateArrange();
            }
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            foreach (var child in Children)
            {
                var size = child.DesiredSize;
                var offset = child.GetValue<Vector2>(PopupOriginProperty);
                var altPos = child.GetValue<Vector2?>(AltOriginProperty);

                var (r, b) = size + offset; // bottom right corner.

                var isAltPos = false;

                // Clamp the right edge.
                if (r > Width)
                {
                    // Try to position at alt pos.
                    if (altPos != null && altPos.Value.X - size.X > 0)
                    {
                        // There is horizontal room at the alt pos so there we go.
                        isAltPos = true;
                        offset = (altPos.Value.X - size.X, altPos.Value.Y);
                        (_, b) = size + offset;
                    }
                    else
                    {
                        offset -= (r - Width, 0);
                    }
                }

                // Clamp the bottom edge.
                if (b > Height)
                {
                    offset -= (0, b - Height);
                }

                // Try to clamp the left edge.
                if (offset.X < 0 && !isAltPos)
                {
                    offset -= (offset.X, 0);
                }

                // Try to clamp the top edge.
                if (offset.Y < 0)
                {
                    offset -= (0, offset.Y);
                }

                child.Arrange(UIBox2.FromDimensions(offset, size));
            }

            return finalSize;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            // Measure to availableSize so that child controls never get too large to fit the whole screen.
            base.MeasureOverride(availableSize);

            return availableSize;
        }

        protected override void Resized()
        {
            base.Resized();

            InvalidateArrange();
        }
    }
}
