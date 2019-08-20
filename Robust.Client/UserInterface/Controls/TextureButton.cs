using System;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class TextureButton : BaseButton
    {
        private Vector2 _scale = (1, 1);
        public const string StylePropertyTexture = "texture";
        public const string StylePseudoClassNormal = "normal";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassDisabled = "disabled";
        public const string StylePseudoClassPressed = "pressed";

        public TextureButton()
        {
            DrawModeChanged();
        }

        public Texture TextureNormal { get; set; }
        public Texture TextureHover { get; set; }

        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                MinimumSizeChanged();
            }
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

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            var texture = TextureNormal;

            if (IsHovered && TextureHover != null)
            {
                texture = TextureHover;
            }

            if (texture == null)
            {
                TryGetStyleProperty(StylePropertyTexture, out texture);
                if (texture == null)
                {
                    return;
                }
            }

            handle.DrawTextureRectRegion(texture, PixelSizeBox);
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return Scale * (TextureNormal?.Size ?? Vector2.Zero) / UIScale;
        }
    }
}
