using System;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Simple control that draws a single texture using a variety of possible stretching modes.
    /// </summary>
    /// <seealso cref="AnimatedTextureRect"/>
    public class TextureRect : Control
    {
        public const string StylePropertyTexture = "texture";
        public const string StylePropertyShader = "shader";

        private bool _canShrink;
        private Texture? _texture;
        private Vector2 _textureScale = Vector2.One;

        public ShaderInstance? ShaderOverride { get; set; }

        /// <summary>
        ///     The texture to draw.
        /// </summary>
        public Texture? Texture
        {
            get => _texture;
            set
            {
                var oldSize = _texture?.Size;
                _texture = value;

                if (value?.Size != oldSize)
                {
                    MinimumSizeChanged();
                }
            }
        }

        /// <summary>
        ///     Scales the texture displayed.
        /// </summary>
        /// <remarks>
        ///     This does not apply to the following stretch modes: <see cref="StretchMode.Scale"/>.
        /// </remarks>
        public Vector2 TextureScale
        {
            get => _textureScale;
            set
            {
                _textureScale = value;
                MinimumSizeChanged();
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
                MinimumSizeChanged();
            }
        }

        /// <summary>
        ///     Controls how the texture should be drawn if the control is larger than the size of the texture.
        /// </summary>
        public StretchMode Stretch { get; set; } = StretchMode.Keep;

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var texture = _texture;
            ShaderInstance? shader = null;

            if (texture == null)
            {
                TryGetStyleProperty(StylePropertyTexture, out texture);
                if (texture == null)
                {
                    return;
                }
            }

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
                    handle.DrawTextureRectRegion(texture, PixelSizeBox, GetDrawDimensions(texture));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                    return UIBox2.FromDimensions(Vector2.Zero, texture.Size * _textureScale * UIScale);
                case StretchMode.KeepCentered:
                {
                    var position = (PixelSize - texture.Size * _textureScale * UIScale) / 2;
                    return UIBox2.FromDimensions(position, texture.Size * _textureScale * UIScale);
                }

                case StretchMode.KeepAspect:
                case StretchMode.KeepAspectCentered:
                {
                    var (texWidth, texHeight) = texture.Size * _textureScale;
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
                    var texSize = texture.Size * _textureScale;
                    // Calculate the scale necessary to fit width and height to control size.
                    var (scaleX, scaleY) = PixelSize / texSize;
                    // Use whichever scale is greater.
                    var scale = Math.Max(scaleX, scaleY);
                    // Offset inside the actual texture.
                    var offset = (texSize - PixelSize) / scale / 2f;
                    return UIBox2.FromDimensions(offset, PixelSize / scale);
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
            var texture = _texture;

            if (texture == null)
            {
                TryGetStyleProperty(StylePropertyTexture, out texture);
            }

            if (texture == null || CanShrink)
            {
                return Vector2.Zero;
            }

            return texture.Size * TextureScale;
        }
    }
}
