using System;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class TextureButton : BaseButton
    {
        private Vector2 _scale = (1, 1);
        private Texture? _textureNormal;
        public const string StylePropertyTexture = "texture";
        public const string StylePseudoClassNormal = "normal";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassDisabled = "disabled";
        public const string StylePseudoClassPressed = "pressed";

        /// <summary>
        /// Path specified from root of resources.
        /// </summary>
        private string? _texturePath;

        /// <summary>
        /// Path specified relative to current theme.
        /// </summary>
        private string? _textureThemePath;


        public TextureButton()
        {
            DrawModeChanged();
        }

        [ViewVariables]
        public Texture? TextureNormal
        {
            get => _textureNormal;
            set
            {
                _textureNormal = value;
                InvalidateMeasure();
            }
        }

        public string TextureThemePath
        {
            set {
                _textureThemePath = value;
                _texturePath = null;
                ApplyTexture();
            }
        }


        protected override void OnThemeUpdated()
        {
            base.OnThemeUpdated();
            ApplyTexture();
        }
        public string TexturePath
        {
            set
            {
                _texturePath = value;
                _textureThemePath = null;
                ApplyTexture();
            }
        }

        /// <summary>
        /// Attempt to set TextureNormal based on _textureThemePath and _texturePath.
        /// </summary>
        private void ApplyTexture()
        {
            if (_textureThemePath != null)
                TextureNormal = Theme.ResolveTexture(_textureThemePath);
            else if (_texturePath != null)
                TextureNormal = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(_texturePath);
            else
                TextureNormal = null;
        }

        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                InvalidateMeasure();
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

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var texture = TextureNormal;

            if (texture == null)
            {
                TryGetStyleProperty(StylePropertyTexture, out texture);
            }

            return Scale * (texture?.Size ?? Vector2.Zero);
        }
    }
}
