using System;
using Robust.Client.Graphics.Drawing;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class ContainerButton : BaseButton
    {
        public const string StylePropertyStyleBox = "stylebox";
        public const string StylePseudoClassNormal = "normal";
        public const string StylePseudoClassPressed = "pressed";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassDisabled = "disabled";

        public ContainerButton()
        {
            DrawModeChanged();
        }

        private StyleBox ActualStyleBox
        {
            get
            {
                if (TryGetStyleProperty(StylePropertyStyleBox, out StyleBox box))
                {
                    return box;
                }

                return UserInterfaceManager.ThemeDefaults.ButtonStyle;
            }
        }

        protected override void LayoutUpdateOverride()
        {
            var contentBox = ActualStyleBox.GetContentBox(PixelSizeBox);
            foreach (var child in Children)
            {
                FitChildInBox(child, contentBox);
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var style = ActualStyleBox;
            var drawBox = PixelSizeBox;
            style.Draw(handle, drawBox);
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var min = Vector2.Zero;
            foreach (var child in Children)
            {
                min = Vector2.ComponentMax(min, child.CombinedMinimumSize);
            }

            return min + ActualStyleBox.MinimumSize / UIScale;
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
