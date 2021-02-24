using System;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class ContainerButton : BaseButton
    {
        public const string StylePropertyStyleBox = "stylebox";
        public const string StyleClassButton = "button";
        public const string StylePseudoClassNormal = "normal";
        public const string StylePseudoClassPressed = "pressed";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassDisabled = "disabled";

        public ContainerButton() : base()
        {
            DrawModeChanged();
        }

        private StyleBox ActualStyleBox
        {
            get
            {
                if (TryGetStyleProperty<StyleBox>(StylePropertyStyleBox, out var box))
                {
                    return box;
                }

                return UserInterfaceManager.ThemeDefaults.ButtonStyle;
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var boxSize = ActualStyleBox.MinimumSize / UIScale;
            var childBox = Vector2.ComponentMax(availableSize - boxSize, Vector2.Zero);
            var min = Vector2.Zero;
            foreach (var child in Children)
            {
                child.Measure(childBox);
                min = Vector2.ComponentMax(min, child.DesiredSize);
            }

            return min + boxSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var contentBox = ActualStyleBox.GetContentBox(UIBox2.FromDimensions(Vector2.Zero, finalSize * UIScale));

            foreach (var child in Children)
            {
                child.ArrangePixel((UIBox2i) contentBox);
            }

            return finalSize;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var style = ActualStyleBox;
            var drawBox = PixelSizeBox;
            style.Draw(handle, drawBox);
        }

        protected override void DrawModeChanged()
        {
            switch (DrawMode)
            {
                case DrawModeEnum.Normal:
                    SetOnlyStylePseudoClass(StylePseudoClassNormal);
                    break;
                case DrawModeEnum.Pressed:
                    SetOnlyStylePseudoClass(StylePseudoClassPressed);
                    break;
                case DrawModeEnum.Hover:
                    SetOnlyStylePseudoClass(StylePseudoClassHover);
                    break;
                case DrawModeEnum.Disabled:
                    SetOnlyStylePseudoClass(StylePseudoClassDisabled);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
