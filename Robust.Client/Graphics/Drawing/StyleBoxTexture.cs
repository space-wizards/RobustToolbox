using System;
using System.Numerics;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Style box based on a 9-patch texture. An image is
    ///     divided into up to nine regions by splitting the
    ///     image along each `PatchMargin.` The corner pieces
    ///     will be drawn once, at their original size, while
    ///     the `Mode` controls the (up to five) central pieces
    ///     which can be either stretched or tiled to fill up
    ///     the space the box is being drawn in.
    /// </summary>
    [Virtual]
    public class StyleBoxTexture : StyleBox
    {
        public StyleBoxTexture()
        {
        }

        /// <summary>
        ///     Clones a stylebox so it can be separately modified.
        /// </summary>
        public StyleBoxTexture(StyleBoxTexture copy)
            : base(copy)
        {
            PatchMarginTop = copy.PatchMarginTop;
            PatchMarginLeft = copy.PatchMarginLeft;
            PatchMarginBottom = copy.PatchMarginBottom;
            PatchMarginRight = copy.PatchMarginRight;

            ExpandMarginLeft = copy.ExpandMarginLeft;
            ExpandMarginTop = copy.ExpandMarginTop;
            ExpandMarginBottom = copy.ExpandMarginBottom;
            ExpandMarginRight = copy.ExpandMarginRight;

            Texture = copy.Texture;
            Modulate = copy.Modulate;
            TextureScale = copy.TextureScale;
        }

        /// <summary>
        /// Left expansion size, in virtual pixels.
        /// </summary>
        /// <remarks>
        /// This expands the size of the area where the patches get drawn. This will cause the drawn texture to
        /// extend beyond the box passed to the <see cref="StyleBox.Draw"/> function. This is not affected by
        /// <see cref="TextureScale"/>.
        /// </remarks>
        public float ExpandMarginLeft { get; set; }

        /// <summary>
        /// Top expansion size, in virtual pixels.
        /// </summary>
        /// <remarks>
        /// This expands the size of the area where the patches get drawn. This will cause the drawn texture to
        /// extend beyond the box passed to the <see cref="StyleBox.Draw"/> function. This is not affected by
        /// <see cref="TextureScale"/>.
        /// </remarks>
        public float ExpandMarginTop { get; set; }

        /// <summary>
        /// Bottom expansion size, in virtual pixels.
        /// </summary>
        /// <remarks>
        /// This expands the size of the area where the patches get drawn. This will cause the drawn texture to
        /// extend beyond the box passed to the <see cref="StyleBox.Draw"/> function. This is not affected by
        /// <see cref="TextureScale"/>.
        /// </remarks>
        public float ExpandMarginBottom { get; set; }

        /// <summary>
        /// Right expansion size, in virtual pixels.
        /// </summary>
        /// <remarks>
        /// This expands the size of the area where the patches get drawn. This will cause the drawn texture to
        /// extend beyond the box passed to the <see cref="StyleBox.Draw"/> function. This is not affected by
        /// <see cref="TextureScale"/>.
        /// </remarks>
        public float ExpandMarginRight { get; set; }

        public StretchMode Mode { get; set; } = StretchMode.Stretch;

        private float _patchMarginLeft;

        /// <summary>
        /// Distance of the left patch margin from the image. In texture space.
        /// The size of this patch in virtual pixels can be obtained by scaling this with <see cref="TextureScale"/>.
        /// </summary>
        public float PatchMarginLeft
        {
            get => _patchMarginLeft;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                _patchMarginLeft = value;
            }
        }

        private float _patchMarginRight;

        /// <summary>
        /// Distance of the right patch margin from the image. In texture space.
        /// The size of this patch in virtual pixels can be obtained by scaling this with <see cref="TextureScale"/>.
        /// </summary>
        public float PatchMarginRight
        {
            get => _patchMarginRight;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                _patchMarginRight = value;
            }
        }

        private float _patchMarginTop;

        /// <summary>
        /// Distance of the top patch margin from the image. In texture space.
        /// The size of this patch in virtual pixels can be obtained by scaling this with <see cref="TextureScale"/>.
        /// </summary>
        public float PatchMarginTop
        {
            get => _patchMarginTop;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                _patchMarginTop = value;
            }
        }

        private float _patchMarginBottom;

        /// <summary>
        /// Distance of the bottom patch margin from the image. In texture space.
        /// The size of this patch in virtual pixels can be obtained by scaling this with <see cref="TextureScale"/>.
        /// </summary>
        public float PatchMarginBottom
        {
            get => _patchMarginBottom;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                _patchMarginBottom = value;
            }
        }

        public Color Modulate { get; set; } = Color.White;

        public Texture? Texture { get; set; }

        /// <summary>
        /// Additional scaling to use when drawing the texture.
        /// </summary>
        public Vector2 TextureScale { get; set; } = Vector2.One;

        public void SetPatchMargin(Margin margin, float value)
        {
            if ((margin & Margin.Top) != 0)
            {
                PatchMarginTop = value;
            }

            if ((margin & Margin.Bottom) != 0)
            {
                PatchMarginBottom = value;
            }

            if ((margin & Margin.Right) != 0)
            {
                PatchMarginRight = value;
            }

            if ((margin & Margin.Left) != 0)
            {
                PatchMarginLeft = value;
            }
        }

        public void SetExpandMargin(Margin margin, float value)
        {
            if ((margin & Margin.Top) != 0)
            {
                ExpandMarginTop = value;
            }

            if ((margin & Margin.Bottom) != 0)
            {
                ExpandMarginBottom = value;
            }

            if ((margin & Margin.Right) != 0)
            {
                ExpandMarginRight = value;
            }

            if ((margin & Margin.Left) != 0)
            {
                ExpandMarginLeft = value;
            }
        }

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
        {
            if (Texture == null)
            {
                return;
            }

            box = new UIBox2(
                box.Left - ExpandMarginLeft * uiScale,
                box.Top - ExpandMarginTop * uiScale,
                box.Right + ExpandMarginRight * uiScale,
                box.Bottom + ExpandMarginBottom * uiScale);

            var scaledMargin = new UIBox2(PatchMarginLeft * TextureScale.X * uiScale, PatchMarginTop * TextureScale.Y * uiScale,
                    PatchMarginRight * TextureScale.X * uiScale, PatchMarginBottom * TextureScale.Y * uiScale);

            if (PatchMarginLeft > 0)
            {
                if (PatchMarginTop > 0)
                {
                    // Draw top left
                    var topLeftBox = new UIBox2(0, 0, scaledMargin.Left, scaledMargin.Top)
                        .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(Texture, topLeftBox,
                        new UIBox2(0, 0, PatchMarginLeft, PatchMarginTop), Modulate);
                }

                {
                    // Draw left
                    var leftBox =
                        new UIBox2(0, scaledMargin.Top, scaledMargin.Left, box.Height - scaledMargin.Bottom)
                            .Translated(box.TopLeft);
                    DrawStretchingArea(handle, leftBox,
                        new UIBox2(0, PatchMarginTop, PatchMarginLeft, Texture.Height - PatchMarginBottom), uiScale);
                }

                if (PatchMarginBottom > 0)
                {
                    // Draw bottom left
                    var bottomLeftBox =
                        new UIBox2(0, box.Height - scaledMargin.Bottom, scaledMargin.Left, box.Height)
                            .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(Texture, bottomLeftBox,
                        new UIBox2(0, Texture.Height - PatchMarginBottom, PatchMarginLeft, Texture.Height), Modulate);
                }
            }

            if (PatchMarginRight > 0)
            {
                if (PatchMarginTop > 0)
                {
                    // Draw top right
                    var topRightBox = new UIBox2(box.Width - scaledMargin.Right, 0, box.Width, scaledMargin.Top)
                        .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(Texture, topRightBox,
                        new UIBox2(Texture.Width - PatchMarginRight, 0, Texture.Width, PatchMarginTop), Modulate);
                }

                {
                    // Draw right
                    var rightBox =
                        new UIBox2(box.Width - scaledMargin.Right, scaledMargin.Top, box.Width,
                                box.Height - scaledMargin.Bottom)
                            .Translated(box.TopLeft);

                    DrawStretchingArea(handle, rightBox,
                        new UIBox2(Texture.Width - PatchMarginRight, PatchMarginTop,
                            Texture.Width,
                            Texture.Height - PatchMarginBottom), uiScale);
                }

                if (PatchMarginBottom > 0)
                {
                    // Draw bottom right
                    var bottomRightBox =
                        new UIBox2(box.Width - scaledMargin.Right, box.Height - scaledMargin.Bottom, box.Width, box.Height)
                            .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(Texture, bottomRightBox,
                        new UIBox2(Texture.Width - PatchMarginRight, Texture.Height - PatchMarginBottom, Texture.Width,
                            Texture.Height), Modulate);
                }
            }

            if (PatchMarginTop > 0)
            {
                // Draw top
                var topBox =
                    new UIBox2(scaledMargin.Left, 0, box.Width - scaledMargin.Right, scaledMargin.Top)
                        .Translated(box.TopLeft);
                DrawStretchingArea(handle, topBox,
                    new UIBox2(PatchMarginLeft, 0, Texture.Width - PatchMarginRight, PatchMarginTop), uiScale);
            }

            if (PatchMarginBottom > 0)
            {
                // Draw bottom
                var bottomBox =
                    new UIBox2(scaledMargin.Left, box.Height - scaledMargin.Bottom, box.Width - scaledMargin.Right,
                            box.Height)
                        .Translated(box.TopLeft);

                DrawStretchingArea(handle, bottomBox,
                    new UIBox2(PatchMarginLeft, Texture.Height - PatchMarginBottom,
                        Texture.Width - PatchMarginRight,
                        Texture.Height), uiScale);
            }

            // Draw center
            {
                var centerBox = new UIBox2(scaledMargin.Left, scaledMargin.Top, box.Width - scaledMargin.Right,
                    box.Height - scaledMargin.Bottom).Translated(box.TopLeft);

                DrawStretchingArea(handle, centerBox, new UIBox2(PatchMarginLeft, PatchMarginTop, Texture.Width - PatchMarginRight,
                    Texture.Height - PatchMarginBottom), uiScale);
            }
        }

        private void DrawStretchingArea(DrawingHandleScreen handle, UIBox2 area, UIBox2 texCoords, float uiScale)
        {
            if (Mode == StretchMode.Stretch)
            {
                handle.DrawTextureRectRegion(Texture!, area, texCoords, Modulate);
                return;
            }

            DebugTools.Assert(Mode == StretchMode.Tile);

            // TODO: this is an insanely expensive way to do tiling, seriously.
            // This should 100% be implemented in a shader instead.

            var sectionWidth = texCoords.Width * TextureScale.X * uiScale;
            var sectionHeight = texCoords.Height * TextureScale.Y * uiScale;
            var invScale = Vector2.One / TextureScale;

            for (var x = area.Left; area.Right - x > 0; x += sectionWidth)
            {
                for (var y = area.Top; area.Bottom - y > 0; y += sectionHeight)
                {
                    var destWidth = Math.Min(area.Right - x, sectionWidth);
                    var destHeight = Math.Min(area.Bottom - y, sectionHeight);
                    var texWidth = Math.Min((area.Right - x) * invScale.X, texCoords.Width);
                    var texHeight = Math.Min((area.Bottom - y) * invScale.Y, texCoords.Height);

                    handle.DrawTextureRectRegion(
                        Texture!,
                        UIBox2.FromDimensions(x, y, destWidth, destHeight),
                        UIBox2.FromDimensions(texCoords.Left, texCoords.Top, texWidth, texHeight),
                        Modulate);
                }
            }
        }

        protected override float GetDefaultContentMargin(Margin margin)
        {
            switch (margin)
            {
                case Margin.Top:
                    return PatchMarginTop * TextureScale.Y;
                case Margin.Bottom:
                    return PatchMarginBottom * TextureScale.Y;
                case Margin.Right:
                    return PatchMarginRight * TextureScale.X;
                case Margin.Left:
                    return PatchMarginLeft * TextureScale.X;
                default:
                    throw new ArgumentOutOfRangeException(nameof(margin), margin, null);
            }
        }

        /// <summary>
        ///     Specifies how to stretch the sides and center of the style box.
        /// </summary>
        public enum StretchMode : byte
        {
            Stretch,
            Tile,
        }
    }
}
