using System;
using JetBrains.Annotations;
using Robust.Client.Utility;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Drawing
{
    /// <summary>
    ///     Equivalent to Godot's <c>StyleBox</c> class,
    ///     this is for drawing pretty fancy boxes using minimal code.
    /// </summary>
    [PublicAPI]
    public abstract class StyleBox
    {
        public Vector2 MinimumSize =>
            new Vector2(GetContentMargin(Margin.Left) + GetContentMargin(Margin.Right),
                GetContentMargin(Margin.Top) + GetContentMargin(Margin.Bottom));

        private float? _contentMarginTop;
        private float? _contentMarginLeft;
        private float? _contentMarginBottom;
        private float? _contentMarginRight;

        public float? ContentMarginLeftOverride
        {
            get => _contentMarginLeft;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                _contentMarginLeft = value;
            }
        }

        public float? ContentMarginTopOverride
        {
            get => _contentMarginTop;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                _contentMarginTop = value;
            }
        }

        public float? ContentMarginRightOverride
        {
            get => _contentMarginRight;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                _contentMarginRight = value;
            }
        }

        public float? ContentMarginBottomOverride
        {
            get => _contentMarginBottom;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                _contentMarginBottom = value;
            }
        }

        public void Draw(DrawingHandleScreen handle, UIBox2 box)
        {
            DoDraw(handle, box);
        }

        public float GetContentMargin(Margin margin)
        {
            float? marginData;
            switch (margin)
            {
                case Margin.Top:
                    marginData = ContentMarginTopOverride;
                    break;
                case Margin.Bottom:
                    marginData = ContentMarginBottomOverride;
                    break;
                case Margin.Right:
                    marginData = ContentMarginRightOverride;
                    break;
                case Margin.Left:
                    marginData = ContentMarginLeftOverride;
                    break;
                default:
                    throw new ArgumentException("Margin must be a single margin flag.", nameof(margin));
            }

            return marginData ?? GetDefaultContentMargin(margin);
        }

        /// <summary>
        ///     Returns the offsets of the content region of this box,
        ///     if this box is drawn from the given position.
        /// </summary>
        public Vector2 GetContentOffset(Vector2 basePosition)
        {
            return basePosition + (GetContentMargin(Margin.Left), GetContentMargin(Margin.Top));
        }

        /// <summary>
        ///     Gets the box considered the "contents" of this style box, when drawn at a specific size.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     <paramref name="baseBox"/> is too small and the resultant box would have negative dimensions.
        /// </exception>
        public UIBox2 GetContentBox(UIBox2 baseBox)
        {
            var left = baseBox.Left + GetContentMargin(Margin.Left);
            var top = baseBox.Top + GetContentMargin(Margin.Top);
            var right = baseBox.Right - GetContentMargin(Margin.Right);
            var bottom = baseBox.Bottom - GetContentMargin(Margin.Bottom);

            if (left > right || top > bottom)
            {
                throw new ArgumentException("Box is too small!", nameof(baseBox));
            }

            return new UIBox2(left, top, right, bottom);
        }

        /// <summary>
        ///     Gets the draw box, positioned at <paramref name="position"/>,
        ///     that envelops a box with the given dimensions perfectly given this box's content margins.
        /// </summary>
        /// <remarks>
        ///     It's basically a reverse <see cref="GetContentBox"/>.
        /// </remarks>
        /// <param name="position">The position at which the new box should be drawn.</param>
        /// <param name="dimensions">The dimensions of the content box inside this new box.</param>
        /// <returns>
        ///     A box that, when ran through <see cref="GetContentBox"/>,
        ///     has a content box of size <paramref name="dimensions"/>
        /// </returns>
        public UIBox2 GetEnvelopBox(Vector2 position, Vector2 dimensions)
        {
            return UIBox2.FromDimensions(position, dimensions + MinimumSize);
        }

        public void SetContentMarginOverride(Margin margin, float value)
        {
            if ((margin & Margin.Left) != 0)
            {
                ContentMarginLeftOverride = value;
            }

            if ((margin & Margin.Top) != 0)
            {
                ContentMarginTopOverride = value;
            }

            if ((margin & Margin.Right) != 0)
            {
                ContentMarginRightOverride = value;
            }

            if ((margin & Margin.Bottom) != 0)
            {
                ContentMarginBottomOverride = value;
            }
        }

        protected abstract void DoDraw(DrawingHandleScreen handle, UIBox2 box);

        protected virtual float GetDefaultContentMargin(Margin margin)
        {
            return 0;
        }

        [Flags]
        public enum Margin
        {
            None = 0,
            Top = 1,
            Bottom = 2,
            Right = 4,
            Left = 8,
            All = Top | Bottom | Right | Left,
            Vertical = Top | Bottom,
            Horizontal = Left | Right,
        }
    }

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
        {
            PatchMarginTop = copy.PatchMarginTop;
            PatchMarginLeft = copy.PatchMarginLeft;
            PatchMarginBottom = copy.PatchMarginBottom;
            PatchMarginRight = copy.PatchMarginRight;
            Texture = copy.Texture;
            Modulate = copy.Modulate;
        }

        public float ExpandMarginLeft { get; set; }

        public float ExpandMarginTop { get; set; }

        public float ExpandMarginBottom { get; set; }

        public float ExpandMarginRight { get; set; }

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

        public Texture Texture { get; set; }

        [Obsolete("Use SetPatchMargin")]
        public void SetMargin(Margin margin, float value)
        {
            SetPatchMargin(margin, value);
        }

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
                    handle.DrawTextureRectRegion(Texture, leftBox,
                        new UIBox2(0, PatchMarginTop, PatchMarginLeft, Texture.Height - PatchMarginBottom), Modulate);
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
                    handle.DrawTextureRectRegion(Texture, rightBox,
                        new UIBox2(Texture.Width - PatchMarginRight, PatchMarginTop, Texture.Width,
                            Texture.Height - PatchMarginBottom), Modulate);
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
                handle.DrawTextureRectRegion(Texture, topBox,
                    new UIBox2(PatchMarginLeft, 0, Texture.Width - PatchMarginRight, PatchMarginTop), Modulate);
            }

            if (PatchMarginBottom > 0)
            {
                // Draw bottom
                var bottomBox =
                    new UIBox2(PatchMarginLeft, box.Height - PatchMarginBottom, box.Width - PatchMarginRight,
                            box.Height)
                        .Translated(box.TopLeft);
                handle.DrawTextureRectRegion(Texture, bottomBox,
                    new UIBox2(PatchMarginLeft, Texture.Height - PatchMarginBottom, Texture.Width - PatchMarginRight,
                        Texture.Height), Modulate);
            }

            // Draw center
            {
                var centerBox = new UIBox2(PatchMarginLeft, PatchMarginTop, box.Width - PatchMarginRight,
                    box.Height - PatchMarginBottom).Translated(box.TopLeft);

                handle.DrawTextureRectRegion(Texture, centerBox,
                    new UIBox2(PatchMarginLeft, PatchMarginTop, Texture.Width - PatchMarginRight,
                        Texture.Height - PatchMarginBottom), Modulate);
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
    }

    public class StyleBoxFlat : StyleBox
    {
        public Color BackgroundColor { get; set; }

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box)
        {
            handle.DrawRect(box, BackgroundColor);
        }
    }

    public class StyleBoxEmpty : StyleBox
    {
        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box)
        {
            // It's empty what more do you want?
        }
    }
}
