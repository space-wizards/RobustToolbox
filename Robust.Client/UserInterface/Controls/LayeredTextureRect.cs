using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Like a TextureRect... but layered
    /// </summary>
    [Virtual]
    public class LayeredTextureRect : Control
    {
        public const string StylePropertyShader = "shader";

        private bool _canShrink;
        private List<Texture> _textures = new();
        private Vector2 _textureScale = Vector2.One;

        public ShaderInstance? ShaderOverride { get; set; }

        /// <summary>
        ///     The textures to draw.
        /// </summary>
        public List<Texture> Textures
        {
            get => _textures;
            set
            {
                _textures = value;
                InvalidateMeasure();
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
        public TextureRect.StretchMode Stretch { get; set; } = TextureRect.StretchMode.Keep;

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            ShaderInstance? shader = null;

            foreach (var texture in Textures)
            {
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
                    case TextureRect.StretchMode.Scale:
                        handle.DrawTextureRect(texture,
                            UIBox2.FromDimensions(Vector2.Zero, PixelSize));
                        break;
                    case TextureRect.StretchMode.Tile:
                    // TODO: Implement Tile.
                    case TextureRect.StretchMode.Keep:
                        handle.DrawTextureRect(texture,
                            UIBox2.FromDimensions(Vector2.Zero, texture.Size * _textureScale * UIScale));
                        break;
                    case TextureRect.StretchMode.KeepCentered:
                    {
                        var position = (PixelSize - texture.Size * _textureScale * UIScale) / 2;
                        handle.DrawTextureRect(texture, UIBox2.FromDimensions(position, texture.Size * _textureScale * UIScale));
                        break;
                    }

                    case TextureRect.StretchMode.KeepAspect:
                    case TextureRect.StretchMode.KeepAspectCentered:
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
                        if (Stretch == TextureRect.StretchMode.KeepAspectCentered)
                        {
                            position = (PixelSize - size) / 2;
                        }

                        handle.DrawTextureRectRegion(texture, UIBox2.FromDimensions(position, size));
                        break;
                    }

                    case TextureRect.StretchMode.KeepAspectCovered:
                        var texSize = texture.Size * _textureScale;
                        // Calculate the scale necessary to fit width and height to control size.
                        var (scaleX, scaleY) = PixelSize / texSize;
                        // Use whichever scale is greater.
                        var scale = Math.Max(scaleX, scaleY);
                        // Offset inside the actual texture.
                        var offset = (texSize - PixelSize) / scale / 2f;
                        handle.DrawTextureRectRegion(texture, PixelSizeBox, UIBox2.FromDimensions(offset, PixelSize / scale));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (_textures.Count == 0 || CanShrink)
            {
                return Vector2.Zero;
            }

            var largestX = 0;
            var largestY = 0;

            foreach (var texture in Textures)
            {
                largestX = Math.Max(texture.Width, largestX);
                largestY = Math.Max(texture.Height, largestY);
            }

            return new Vector2(largestX, largestY) * TextureScale;
        }
    }
}
