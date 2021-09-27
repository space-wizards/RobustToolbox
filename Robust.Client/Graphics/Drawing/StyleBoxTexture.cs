using System;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Style box based on a 9-patch texture.
    /// </summary>
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
        }

        public float ExpandMarginLeft { get; set; }

        public float ExpandMarginTop { get; set; }

        public float ExpandMarginBottom { get; set; }

        public float ExpandMarginRight { get; set; }

        public StretchMode Mode { get; set; } = StretchMode.Stretch;

        private float _patchMarginLeft;

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

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box)
        {
            if (Texture == null)
            {
                return;
            }

            box = new UIBox2(
                box.Left - ExpandMarginLeft,
                box.Top - ExpandMarginTop,
                box.Right + ExpandMarginRight,
                box.Bottom + ExpandMarginBottom);

            if (PatchMarginLeft > 0)
            {
                if (PatchMarginTop > 0)
                {
                    // Draw top left
                    var topLeftBox = new UIBox2(0, 0, PatchMarginLeft, PatchMarginTop)
                        .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(Texture, topLeftBox,
                        new UIBox2(0, 0, PatchMarginLeft, PatchMarginTop), Modulate);
                }

                {
                    // Draw left
                    var leftBox =
                        new UIBox2(0, PatchMarginTop, PatchMarginLeft, box.Height - PatchMarginBottom)
                            .Translated(box.TopLeft);
                    DrawStretchingArea(handle, leftBox,
                        new UIBox2(0, PatchMarginTop, PatchMarginLeft, Texture.Height - PatchMarginBottom));
                }

                if (PatchMarginBottom > 0)
                {
                    // Draw bottom left
                    var bottomLeftBox =
                        new UIBox2(0, box.Height - PatchMarginBottom, PatchMarginLeft, box.Height)
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
                    var topRightBox = new UIBox2(box.Width - PatchMarginRight, 0, box.Width, PatchMarginTop)
                        .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(Texture, topRightBox,
                        new UIBox2(Texture.Width - PatchMarginRight, 0, Texture.Width, PatchMarginTop), Modulate);
                }

                {
                    // Draw right
                    var rightBox =
                        new UIBox2(box.Width - PatchMarginRight, PatchMarginTop, box.Width,
                                box.Height - PatchMarginBottom)
                            .Translated(box.TopLeft);

                    DrawStretchingArea(handle, rightBox,
                        new UIBox2(Texture.Width - PatchMarginRight, PatchMarginTop,
                            Texture.Width,
                            Texture.Height - PatchMarginBottom));
                }

                if (PatchMarginBottom > 0)
                {
                    // Draw bottom right
                    var bottomRightBox =
                        new UIBox2(box.Width - PatchMarginRight, box.Height - PatchMarginBottom, box.Width, box.Height)
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
                    new UIBox2(PatchMarginLeft, 0, box.Width - PatchMarginRight, PatchMarginTop)
                        .Translated(box.TopLeft);
                DrawStretchingArea(handle, topBox,
                    new UIBox2(PatchMarginLeft, 0, Texture.Width - PatchMarginRight, PatchMarginTop));
            }

            if (PatchMarginBottom > 0)
            {
                // Draw bottom
                var bottomBox =
                    new UIBox2(PatchMarginLeft, box.Height - PatchMarginBottom, box.Width - PatchMarginRight,
                            box.Height)
                        .Translated(box.TopLeft);

                DrawStretchingArea(handle, bottomBox,
                    new UIBox2(PatchMarginLeft, Texture.Height - PatchMarginBottom,
                        Texture.Width - PatchMarginRight,
                        Texture.Height));
            }

            // Draw center
            {
                var centerBox = new UIBox2(PatchMarginLeft, PatchMarginTop, box.Width - PatchMarginRight,
                    box.Height - PatchMarginBottom).Translated(box.TopLeft);

                DrawStretchingArea(handle, centerBox, new UIBox2(PatchMarginLeft, PatchMarginTop, Texture.Width - PatchMarginRight,
                    Texture.Height - PatchMarginBottom));
            }
        }

        private void DrawStretchingArea(DrawingHandleScreen handle, UIBox2 area, UIBox2 texCoords)
        {
            if (Mode == StretchMode.Stretch)
            {
                handle.DrawTextureRectRegion(Texture!, area, texCoords, Modulate);
                return;
            }

            DebugTools.Assert(Mode == StretchMode.Tile);

            // TODO: this is an insanely expensive way to do tiling, seriously.
            // This should 100% be implemented in a shader instead.

            var texWidth = texCoords.Width;
            var texHeight = texCoords.Height;

            for (var x = area.Left; area.Right - x > 0; x += texWidth)
            {
                for (var y = area.Top; area.Bottom - y > 0; y += texHeight)
                {
                    var w = Math.Min(area.Right - x, texWidth);
                    var h = Math.Min(area.Bottom - y, texHeight);

                    handle.DrawTextureRectRegion(
                        Texture!,
                        UIBox2.FromDimensions(x, y, w, h),
                        UIBox2.FromDimensions(texCoords.Left, texCoords.Top, w, h),
                        Modulate);
                }
            }
        }

        protected override float GetDefaultContentMargin(Margin margin)
        {
            switch (margin)
            {
                case Margin.Top:
                    return PatchMarginTop;
                case Margin.Bottom:
                    return PatchMarginBottom;
                case Margin.Right:
                    return PatchMarginRight;
                case Margin.Left:
                    return PatchMarginLeft;
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
