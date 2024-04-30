using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Simple control that draws a single texture using a variety of possible stretching modes.
    /// </summary>
    /// <seealso cref="AnimatedTextureRect"/>
    [Virtual]
    public class TextureRect : Control
    {
        public const string StylePropertyTexture = "texture";
        public const string StylePropertyShader = "shader";
        public const string StylePropertyTextureStretch = "texture-stretch";
        public const string StylePropertyTextureScale = "texture-scale";
        public const string StylePropertyTextureSizeTarget = "texture-size-target";

        private bool _canShrink;
        private Texture? _texture;
        private Vector2 _textureScale = Vector2.One;

        public ShaderInstance? ShaderOverride { get; set; }

        /// <summary>
        ///     The texture to draw.
        /// </summary>
        public Texture? Texture
        {
            get
            {
                if (_texture is null)
                {
                    if (TryGetStyleProperty(StylePropertyTexture, out Texture? texture))
                    {
                        return texture;
                    }
                }

                return _texture;
            }
            set
            {
                var oldSize = _texture?.Size;
                _texture = value;

                if (value?.Size != oldSize)
                {
                    InvalidateMeasure();
                }
            }
        }

        private string? _texturePath;
        private StretchMode _stretch = StretchMode.Keep;

        public string TexturePath
        {
            set
            {
                Texture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(value);
                _texturePath = value;
            }

        }

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();
            InvalidateMeasure();
        }

        protected override void OnThemeUpdated()
        {
            if (_texturePath != null) Texture = Theme.ResolveTexture(_texturePath);
            base.OnThemeUpdated();
        }

        public Vector2 TextureSizeTarget
        {
            get
            {
                if (!TryGetStyleProperty(StylePropertyTextureSizeTarget, out Vector2 target))
                    target = _textureScale * Texture?.Size ?? Vector2.Zero;

                return target;
            }
        }

        /// <summary>
        ///     Scales the texture displayed.
        /// </summary>
        /// <remarks>
        ///     This does not apply to the following stretch modes: <see cref="StretchMode.Scale"/>.
        ///     This additionally does not apply if a size target is set.
        /// </remarks>
        public Vector2 TextureScale
        {
            get
            {
                if (!TryGetStyleProperty(StylePropertyTextureScale, out Vector2 scale))
                    scale = _textureScale;

                return scale;
            }
            set
            {
                _textureScale = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     If true, this control can shrink below the size of <see cref="Texture"/>.
        /// </summary>
        /// <remarks>
        ///     This does not set <see cref="Control.RectClipContent"/>.
        ///     Certain stretch modes may display outside the area of the control unless it is set.
        /// </remarks>
        public bool CanShrink
        {
            get => _canShrink;
            set
            {
                _canShrink = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Controls how the texture should be drawn if the control is larger than the size of the texture.
        /// </summary>
        public StretchMode Stretch
        {
            get
            {
                if (!TryGetStyleProperty(StylePropertyTextureStretch, out StretchMode stretch))
                    stretch = _stretch;
                return stretch;
            }
            set => _stretch = value;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var texture = Texture;

            if (texture is null)
                return;

            ShaderInstance? shader = null;

            if (ShaderOverride != null)
            {
                shader = ShaderOverride;
            }
            else if (TryGetStyleProperty(StylePropertyShader, out ShaderInstance? styleShader))
            {
                shader = styleShader;
            }

            if (shader != null)
            {
                handle.UseShader(shader);
            }

            switch (Stretch)
            {
                case StretchMode.Scale:
                case StretchMode.Tile:
                // TODO: Implement Tile.
                case StretchMode.Keep:
                case StretchMode.KeepCentered:
                case StretchMode.KeepAspect:
                case StretchMode.KeepAspectCentered:
                    handle.DrawTextureRect(texture,
                        GetDrawDimensions(texture));
                    break;
                case StretchMode.KeepAspectCovered:
                    var dimensions = GetDrawDimensions(texture);
                    var subRegion = CalcClipSubRegion(texture.Size, dimensions, PixelSizeBox);
                    handle.DrawTextureRectRegion(texture, PixelSizeBox, subRegion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static UIBox2 CalcClipSubRegion(Vector2 texSize, UIBox2 drawDimensions, UIBox2 size)
        {
            var normTL = (size.TopLeft - drawDimensions.TopLeft) / drawDimensions.Size;
            var normBR = (size.BottomRight - drawDimensions.TopLeft) / drawDimensions.Size;

            return new UIBox2(normTL * texSize, normBR * texSize);
        }

        protected UIBox2 GetDrawDimensions(Texture texture)
        {
            switch (Stretch)
            {
                case StretchMode.Scale:
                    return UIBox2.FromDimensions(Vector2.Zero, PixelSize);
                case StretchMode.Tile:
                // TODO: Implement Tile.
                case StretchMode.Keep:
                    return UIBox2.FromDimensions(Vector2.Zero, TextureSizeTarget * UIScale);
                case StretchMode.KeepCentered:
                {
                    var position = (Size - TextureSizeTarget) / 2;
                    return UIBox2.FromDimensions(position, TextureSizeTarget * UIScale);
                }

                case StretchMode.KeepAspect:
                case StretchMode.KeepAspectCentered:
                {
                    var (texWidth, texHeight) = TextureSizeTarget;
                    var width = texWidth * (PixelSize.Y / texHeight);
                    var height = (float)PixelSize.Y;
                    if (width > PixelSize.X)
                    {
                        width = PixelSize.X;
                        height = texHeight * (PixelSize.X / texWidth);
                    }

                    var size = new Vector2(width, height);
                    var position = Vector2.Zero;
                    if (Stretch == StretchMode.KeepAspectCentered)
                    {
                        position = (PixelSize - size) / 2;
                    }

                    return UIBox2.FromDimensions(position, size);
                }

                case StretchMode.KeepAspectCovered:
                    var texSize = TextureSizeTarget;
                    // Calculate the scale necessary to fit width and height to control size.
                    var (scaleX, scaleY) = PixelSize / texSize;
                    // Use whichever scale is greater.
                    var scale = Math.Max(scaleX, scaleY);
                    // Offset inside the actual texture.
                    var texDrawSize = texSize * scale;
                    var offset = (PixelSize - texDrawSize) / 2f;
                    return UIBox2.FromDimensions(offset, texDrawSize);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum StretchMode : byte
        {
            /// <summary>
            ///     The texture is stretched to fit the entire area of the control.
            /// </summary>
            Scale = 1,

            /// <summary>
            ///     The texture is tiled to fit the entire area of the control, without stretching.
            /// </summary>
            Tile = 2,

            /// <summary>
            ///     The texture is drawn in its correct size, in the top left corner of the control.
            /// </summary>
            Keep = 3,

            /// <summary>
            ///     The texture is drawn in its correct size, in the center of the control.
            /// </summary>
            KeepCentered = 4,

            /// <summary>
            ///     The texture is stretched to take as much space as possible,
            ///     while maintaining the original aspect ratio.
            ///     The texture is positioned from the top left corner of the control.
            ///     The texture remains completely visible, potentially leaving some sections of the control blank.
            /// </summary>
            KeepAspect = 5,

            /// <summary>
            ///     <see cref="KeepAspect"/>, but the texture is centered instead.
            /// </summary>
            KeepAspectCentered = 7,

            /// <summary>
            ///     <see cref="KeepAspectCentered"/>, but the texture covers the entire control,
            ///     potentially cutting out part of the texture.
            /// </summary>
            /// <example>
            ///     This effectively causes the entire control to be filled with the texture,
            ///     while preserving aspect ratio.
            /// </example>
            KeepAspectCovered = 8
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (CanShrink || Texture == null)
                return Vector2.Zero;

            return TextureSizeTarget;
        }
    }
}
