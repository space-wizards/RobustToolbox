using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Container that tries to put controls at their specified origin,
    ///     moving them around if necessary to avoid them going outside of the bounds of the container.
    /// </summary>
    public sealed class PopupContainer : Control
    {
        /// <summary>
        ///     The origin that the container tries to place the child at.
        /// </summary>
        public static readonly AttachedProperty PopupOriginProperty = AttachedProperty.Create("PopupOrigin",
            typeof(PopupContainer), typeof(Vector2), changed: PopupOriginChangedCallback);

        public PopupContainer()
        {
            RectClipContent = true;

            MouseFilter = MouseFilterMode.Ignore;
        }

        public static void SetPopupOrigin(Control control, Vector2 origin)
        {
            control.SetValue(PopupOriginProperty, origin);
        }

        private static void PopupOriginChangedCallback(Control owner, AttachedPropertyChangedEventArgs eventArgs)
        {
            if (owner.Parent is PopupContainer container)
            {
                container.UpdateLayout();
            }
        }

        protected override void LayoutUpdateOverride()
        {
            foreach (var child in Children)
            {
                var size = child.CombinedMinimumSize;
                var offset = child.GetValue<Vector2>(PopupOriginProperty);

                var (r, b) = size + offset; // bottom right corner.

                // Clamp the right edge.
                if (r > Width)
                {
                    offset -= (r - Width, 0);
                }

                // Clamp the bottom edge.
                if (b > Height)
                {
                    offset -= (0, b - Height);
                }

                // Try to clamp the left edge.
                if (offset.X < 0)
                {
                    offset -= (offset.X, 0);
                }

                // Try to clamp the top edge.
                if (offset.Y < 0)
                {
                    offset -= (0, offset.Y);
                }

                FitChildInBox(child, UIBox2.FromDimensions(offset, size));
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            // Do NOT inherit minimum size from contents!
            // Just clip 'em.
            return (0, 0);
        }

        protected override void Resized()
        {
            base.Resized();

            UpdateLayout();
        }
    }
}
