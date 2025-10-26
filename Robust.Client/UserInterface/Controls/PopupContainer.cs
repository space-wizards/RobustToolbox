using System.Numerics;
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

        /// <summary>
        ///     Alternative position to bottom-left-align the popup if <see cref="PopupOriginProperty"/>
        ///     would put it off-screen vertically.
        /// </summary>
        public static readonly AttachedProperty AltOriginUpProperty = AttachedProperty.Create("AltOriginUp",
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

        public static Vector2? GetAltOriginUp(Control control)
        {
            return control.GetValue<Vector2?>(AltOriginUpProperty);
        }

        public static void SetAltOrigin(Control control, Vector2? origin)
        {
            control.SetValue(AltOriginProperty, origin);
        }

        public static void SetAltOriginUp(Control control, Vector2? origin)
        {
            control.SetValue(AltOriginUpProperty, origin);
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
                var altPosUp = child.GetValue<Vector2?>(AltOriginUpProperty);

                var box = UIBox2.FromDimensions(offset, size);

                var isAltPos = false;
                var isAltPosUp = false;

                // Clamp the right edge.
                if (box.Right > Width)
                {
                    // Try to position at alt pos.
                    if (altPos != null && altPos.Value.X - size.X > 0)
                    {
                        // There is horizontal room at the alt pos so there we go.
                        isAltPos = true;
                        box = UIBox2.FromDimensions(new Vector2(altPos.Value.X - size.X, altPos.Value.Y), size);
                    }
                    else
                    {
                        box = box.Translated(new Vector2(-(box.Right - Width), 0));
                    }
                }

                // Clamp the bottom edge.
                if (box.Bottom > Height)
                {
                    // Try to position at alt pos.
                    if (altPosUp != null && altPosUp.Value.Y - size.Y > 0)
                    {
                        // There is vertical room at the alt pos so there we go.
                        isAltPosUp = true;
                        box = UIBox2.FromDimensions(new Vector2(altPosUp.Value.X, altPosUp.Value.Y - size.Y), size);
                    }
                    else
                    {
                        box = box.Translated(new Vector2(0, -(box.Bottom - Height)));
                    }
                }

                // Try to clamp the left edge.
                if (box.Left < 0 && !isAltPos)
                {
                    box = box.Translated(new Vector2(-offset.X, 0));
                }

                // Try to clamp the top edge.
                if (box.Top < 0 && !isAltPosUp)
                {
                    box = box.Translated(new Vector2(0, -offset.Y));
                }

                child.Arrange(box);
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
