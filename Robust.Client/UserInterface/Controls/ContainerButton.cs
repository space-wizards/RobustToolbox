using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class ContainerButton : BaseButton
    {
        public const string StylePropertyStyleBox = "stylebox";
        public const string StyleClassButton = "button";

        /// <summary>
        /// The button is toggled off.
        /// </summary>
        /// <remarks>Mutually exclusive with <see cref="StylePseudoClassPressed"/></remarks>
        public const string StylePseudoClassNormal = "normal";

        /// <summary>
        /// The button is toggled on.
        /// </summary>
        /// <remarks>Mutually exclusive with <see cref="StylePseudoClassNormal"/></remarks>
        public const string StylePseudoClassPressed = "pressed";

        /// <summary>
        /// The mouse is actively attempting to press/toggle the button.
        /// </summary>
        public const string StylePseudoClassPressing = "pressing";

        /// <summary>
        /// The mouse is hovering over the button.
        /// </summary>
        public const string StylePseudoClassHover = "hover";

        /// <summary>
        /// The button is not taking any interaction.
        /// </summary>
        public const string StylePseudoClassDisabled = "disabled";

        public StyleBox? StyleBoxOverride { get; set; }

        public ContainerButton()
        {
            DrawModeChanged();
        }

        private StyleBox ActualStyleBox
        {
            get
            {
                if (StyleBoxOverride != null)
                {
                    return StyleBoxOverride;
                }

                if (TryGetStyleProperty<StyleBox>(StylePropertyStyleBox, out var box))
                {
                    return box;
                }

                return UserInterfaceManager.ThemeDefaults.ButtonStyle;
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var boxSize = ActualStyleBox.MinimumSize;
            var childBox = Vector2.Max(availableSize - boxSize, Vector2.Zero);
            var min = Vector2.Zero;
            foreach (var child in Children)
            {
                child.Measure(childBox);
                min = Vector2.Max(min, child.DesiredSize);
            }

            return min + boxSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var box = UIBox2.FromDimensions(Vector2.Zero, finalSize);
            var contentBox = ActualStyleBox.GetContentBox(box, 1);

            foreach (var child in Children)
            {
                child.Arrange(contentBox);
            }

            return finalSize;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var style = ActualStyleBox;
            var drawBox = PixelSizeBox;
            style.Draw(handle, drawBox, UIScale);
        }

        protected override void DrawModeChanged()
        {
            SetOnlyStylePseudoClass(Pressed ? StylePseudoClassPressed : StylePseudoClassNormal);

            if (Disabled)
                AddStylePseudoClass(StylePseudoClassDisabled);

            if (IsHovered)
                AddStylePseudoClass(StylePseudoClassHover);

            if (AttemptingPress)
                AddStylePseudoClass(StylePseudoClassPressing);
        }
    }
}
