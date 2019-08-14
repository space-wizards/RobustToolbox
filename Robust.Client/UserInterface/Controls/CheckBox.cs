using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A type of toggleable button that also has a checkbox.
    /// </summary>
    public class CheckBox : Button
    {
        public const string StylePropertyIcon = "icon";
        public const string StylePropertyHSeparation = "hseparation";

        public CheckBox()
        {
            ToggleMode = true;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            var offset = 0;
            var icon = _getIcon();
            if (icon != null)
            {
                offset += _getIcon().Width + _getHSeparation();
                var vOffset = (PixelHeight - _getIcon().Height) / 2;
                handle.DrawTextureRectRegion(icon, UIBox2.FromDimensions((0, vOffset), icon.Size * UIScale));
            }

            var box = new UIBox2(offset, 0, PixelWidth, PixelHeight);
            DrawTextInternal(handle, box);
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var minSize = _getIcon()?.Size / UIScale ?? Vector2.Zero;
            var font = ActualFont;

            if (!string.IsNullOrWhiteSpace(Text) && !ClipText)
            {
                minSize += (EnsureWidthCache() / UIScale + _getHSeparation() / UIScale, 0);
            }
            minSize = Vector2.ComponentMax(minSize, (0, font.GetHeight(UIScale) / UIScale));

            return minSize;
        }

        private Texture _getIcon()
        {
            if (TryGetStyleProperty(StylePropertyIcon, out Texture tex))
            {
                return tex;
            }

            return null;
        }

        private int _getHSeparation()
        {
            if (TryGetStyleProperty(StylePropertyHSeparation, out int hSep))
            {
                return hSep;
            }

            return 0;
        }
    }
}
