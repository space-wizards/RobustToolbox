using System;
using JetBrains.Annotations;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Drawing
{
    /// <summary>
    ///     Equivalent to Godot's <c>StyleBox</c> class,
    ///     this is for drawing pretty fancy boxes using minimal code.
    /// </summary>
    [PublicAPI]
    public abstract class StyleBox
    {
        internal abstract Godot.StyleBox GodotStyleBox { get; }

        public Vector2 MinimumSize =>
            new Vector2(GetContentMargin(Margin.Left) + GetContentMargin(Margin.Right),
                GetContentMargin(Margin.Top) + GetContentMargin(Margin.Bottom));

        private float? _contentMarginTop;
        private float? _contentMarginLeft;
        private float? _contentMarginBottom;
        private float? _contentMarginRight;

        public float? ContentMarginLeftOverride
        {
            get
            {
                if (!GameController.OnGodot)
                {
                    return _contentMarginLeft;
                }

                var c = GodotStyleBox.ContentMarginLeft;
                if (c < 0)
                {
                    return null;
                }

                return c;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                if (GameController.OnGodot)
                {
                    GodotStyleBox.ContentMarginLeft = value ?? -1;
                }
                else
                {
                    _contentMarginLeft = value;
                }
            }
        }

        public float? ContentMarginTopOverride
        {
            get
            {
                if (!GameController.OnGodot)
                {
                    return _contentMarginTop;
                }

                var c = GodotStyleBox.ContentMarginTop;
                if (c < 0)
                {
                    return null;
                }

                return c;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                if (GameController.OnGodot)
                {
                    GodotStyleBox.ContentMarginTop = value ?? -1;
                }
                else
                {
                    _contentMarginTop = value;
                }
            }
        }

        public float? ContentMarginRightOverride
        {
            get
            {
                if (!GameController.OnGodot)
                {
                    return _contentMarginRight;
                }

                var c = GodotStyleBox.ContentMarginRight;
                if (c < 0)
                {
                    return null;
                }

                return c;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                if (GameController.OnGodot)
                {
                    GodotStyleBox.ContentMarginRight = value ?? -1;
                }
                else
                {
                    _contentMarginRight = value;
                }
            }
        }

        public float? ContentMarginBottomOverride
        {
            get
            {
                if (!GameController.OnGodot)
                {
                    return _contentMarginBottom;
                }

                var c = GodotStyleBox.ContentMarginBottom;
                if (c < 0)
                {
                    return null;
                }

                return c;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                if (GameController.OnGodot)
                {
                    GodotStyleBox.ContentMarginBottom = value ?? -1;
                }
                else
                {
                    _contentMarginBottom = value;
                }
            }
        }

        public void Draw(DrawingHandleScreen handle, UIBox2 box)
        {
            if (GameController.OnGodot)
            {
                GodotStyleBox.Draw(handle.Item, box.Convert());
            }
            else
            {
                DoDraw(handle, box);
            }
        }

        public float GetContentMargin(Margin margin)
        {
            if (GameController.OnGodot)
            {
                return GodotStyleBox.GetMargin((Godot.Margin) margin);
            }

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
            return new Vector2(GetContentMargin(Margin.Left), GetContentMargin(Margin.Top));
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
        private readonly Godot.StyleBoxTexture gdStyleBox;
        internal override Godot.StyleBox GodotStyleBox => gdStyleBox;

        public StyleBoxTexture()
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            gdStyleBox = new Godot.StyleBoxTexture();
        }

        /// <summary>
        ///     Clones a stylebox so it can be separately modified.
        /// </summary>
        public StyleBoxTexture(StyleBoxTexture copy) : this()
        {
            PatchMarginTop = copy.PatchMarginTop;
            PatchMarginLeft = copy.PatchMarginLeft;
            PatchMarginBottom = copy.PatchMarginBottom;
            PatchMarginRight = copy.PatchMarginRight;
            Texture = copy.Texture;
            Modulate = copy.Modulate;
        }

        private float _expandMarginLeft;
        private float _expandMarginRight;
        private float _expandMarginTop;
        private float _expandMarginBottom;

        public float ExpandMarginLeft
        {
            get => GameController.OnGodot ? gdStyleBox.ExpandMarginLeft : _expandMarginLeft;
            set
            {
                if (GameController.OnGodot)
                {
                    gdStyleBox.ExpandMarginLeft = value;
                }
                else
                {
                    _expandMarginLeft = value;
                }
            }
        }

        public float ExpandMarginTop
        {
            get => GameController.OnGodot ? gdStyleBox.ExpandMarginTop : _expandMarginTop;
            set
            {
                if (GameController.OnGodot)
                {
                    gdStyleBox.ExpandMarginTop = value;
                }
                else
                {
                    _expandMarginTop = value;
                }
            }
        }

        public float ExpandMarginBottom
        {
            get => GameController.OnGodot ? gdStyleBox.ExpandMarginBottom : _expandMarginBottom;
            set
            {
                if (GameController.OnGodot)
                {
                    gdStyleBox.ExpandMarginBottom = value;
                }
                else
                {
                    _expandMarginBottom = value;
                }
            }
        }

        public float ExpandMarginRight
        {
            get => GameController.OnGodot ? gdStyleBox.ExpandMarginRight : _expandMarginRight;
            set
            {
                if (GameController.OnGodot)
                {
                    gdStyleBox.ExpandMarginRight = value;
                }
                else
                {
                    _expandMarginRight = value;
                }
            }
        }

        private float _patchMarginLeft;

        public float PatchMarginLeft
        {
            get => GameController.OnGodot ? gdStyleBox.MarginLeft : _patchMarginLeft;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                if (GameController.OnGodot)
                {
                    gdStyleBox.MarginLeft = value;
                }
                else
                {
                    _patchMarginLeft = value;
                }
            }
        }

        private float _patchMarginRight;

        public float PatchMarginRight
        {
            get => GameController.OnGodot ? gdStyleBox.MarginRight : _patchMarginRight;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                if (GameController.OnGodot)
                {
                    gdStyleBox.MarginRight = value;
                }
                else
                {
                    _patchMarginRight = value;
                }
            }
        }

        private float _patchMarginTop;

        public float PatchMarginTop
        {
            get => GameController.OnGodot ? gdStyleBox.MarginTop : _patchMarginTop;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                if (GameController.OnGodot)
                {
                    gdStyleBox.MarginTop = value;
                }
                else
                {
                    _patchMarginTop = value;
                }
            }
        }

        private float _patchMarginBottom;

        public float PatchMarginBottom
        {
            get => GameController.OnGodot ? gdStyleBox.MarginBottom : _patchMarginBottom;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be less than zero.");
                }

                if (GameController.OnGodot)
                {
                    gdStyleBox.MarginBottom = value;
                }
                else
                {
                    _patchMarginBottom = value;
                }
            }
        }

        private Color _modulate = Color.White;

        public Color Modulate
        {
            get => GameController.OnGodot ? gdStyleBox.ModulateColor.Convert() : _modulate;
            set
            {
                if (GameController.OnGodot)
                {
                    gdStyleBox.ModulateColor = value.Convert();
                }
                else
                {
                    _modulate = value;
                }
            }
        }

        private Texture _texture;

        public Texture Texture
        {
            get => _texture ?? (GameController.OnGodot ? new GodotTextureSource(gdStyleBox.Texture) : null);
            // Woo implicit casts.
            set
            {
                _texture = value;
                if (GameController.OnGodot)
                {
                    gdStyleBox.Texture = value;
                }
            }
        }

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
                    handle.DrawTextureRectRegion(_texture, topLeftBox,
                        new UIBox2(0, 0, PatchMarginLeft, PatchMarginTop), Modulate);
                }

                {
                    // Draw left
                    var leftBox =
                        new UIBox2(0, PatchMarginTop, PatchMarginLeft, box.Height - PatchMarginBottom)
                            .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(_texture, leftBox,
                        new UIBox2(0, PatchMarginTop, PatchMarginLeft, Texture.Height - PatchMarginBottom), Modulate);
                }

                if (PatchMarginBottom > 0)
                {
                    // Draw bottom left
                    var bottomLeftBox =
                        new UIBox2(0, box.Height - PatchMarginBottom, PatchMarginLeft, box.Height)
                            .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(_texture, bottomLeftBox,
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
                    handle.DrawTextureRectRegion(_texture, topRightBox,
                        new UIBox2(Texture.Width - PatchMarginRight, 0, Texture.Width, PatchMarginTop), Modulate);
                }

                {
                    // Draw right
                    var rightBox =
                        new UIBox2(box.Width - PatchMarginRight, PatchMarginTop, box.Width,
                                box.Height - PatchMarginBottom)
                            .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(_texture, rightBox,
                        new UIBox2(Texture.Width - PatchMarginRight, PatchMarginTop, Texture.Width,
                            Texture.Height - PatchMarginBottom), Modulate);
                }

                if (PatchMarginBottom > 0)
                {
                    // Draw bottom right
                    var bottomRightBox =
                        new UIBox2(box.Width - PatchMarginRight, box.Height - PatchMarginBottom, box.Width, box.Height)
                            .Translated(box.TopLeft);
                    handle.DrawTextureRectRegion(_texture, bottomRightBox,
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
                handle.DrawTextureRectRegion(_texture, topBox,
                    new UIBox2(PatchMarginLeft, 0, Texture.Width - PatchMarginRight, PatchMarginTop), Modulate);
            }

            if (PatchMarginBottom > 0)
            {
                // Draw bottom
                var bottomBox =
                    new UIBox2(PatchMarginLeft, box.Height - PatchMarginBottom, box.Width - PatchMarginRight,
                            box.Height)
                        .Translated(box.TopLeft);
                handle.DrawTextureRectRegion(_texture, bottomBox,
                    new UIBox2(PatchMarginLeft, Texture.Height - PatchMarginBottom, Texture.Width - PatchMarginRight,
                        Texture.Height), Modulate);
            }

            // Draw center
            {
                var centerBox = new UIBox2(PatchMarginLeft, PatchMarginTop, box.Width - PatchMarginRight,
                    box.Height - PatchMarginBottom).Translated(box.TopLeft);

                handle.DrawTextureRectRegion(_texture, centerBox,
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
        private Color _backgroundColor;

        public Color BackgroundColor
        {
            get => GameController.OnGodot ? stylebox.BgColor.Convert() : _backgroundColor;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.BgColor = value.Convert();
                }
                else
                {
                    _backgroundColor = value;
                }
            }
        }

        private readonly Godot.StyleBoxFlat stylebox;
        internal override Godot.StyleBox GodotStyleBox => stylebox;

        public StyleBoxFlat()
        {
            if (GameController.OnGodot)
            {
                stylebox = new Godot.StyleBoxFlat();
            }
        }

        private float _marginLeft;

        public float MarginLeft
        {
            get => GameController.OnGodot ? stylebox.ContentMarginLeft : _marginLeft;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ContentMarginLeft = value;
                }
                else
                {
                    _marginLeft = value;
                }
            }
        }

        private float _marginRight;

        public float MarginRight
        {
            get => GameController.OnGodot ? stylebox.ContentMarginRight : _marginRight;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ContentMarginRight = value;
                }
                else
                {
                    _marginRight = value;
                }
            }
        }

        private float _marginTop;

        public float MarginTop
        {
            get => GameController.OnGodot ? stylebox.ContentMarginTop : _marginTop;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ContentMarginTop = value;
                }
                else
                {
                    _marginTop = value;
                }
            }
        }

        private float _marginBottom;

        public float MarginBottom
        {
            get => GameController.OnGodot ? stylebox.ContentMarginBottom : _marginBottom;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ContentMarginBottom = value;
                }
                else
                {
                    _marginBottom = value;
                }
            }
        }


        /// <summary>
        ///     Allows setting multiple margins at once.
        /// </summary>
        public void SetContentMargin(Margin margin, float value)
        {
            if ((margin & Margin.Top) != 0)
            {
                MarginTop = value;
            }

            if ((margin & Margin.Bottom) != 0)
            {
                MarginBottom = value;
            }

            if ((margin & Margin.Right) != 0)
            {
                MarginRight = value;
            }

            if ((margin & Margin.Left) != 0)
            {
                MarginLeft = value;
            }
        }

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box)
        {
            var topLeft = box.TopLeft + new Vector2(_marginLeft, _marginTop);
            var bottomRight = box.BottomRight + new Vector2(_marginRight, _marginBottom);

            handle.DrawRect(new UIBox2(topLeft, bottomRight), _backgroundColor);
        }
    }

    public class StyleBoxEmpty : StyleBox
    {
        internal override Godot.StyleBox GodotStyleBox { get; }

        public StyleBoxEmpty()
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            GodotStyleBox = new Godot.StyleBoxEmpty();
        }

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box)
        {
            // It's empty what more do you want?
        }
    }

    internal class GodotStyleBoxWrap : StyleBox
    {
        public GodotStyleBoxWrap(Godot.StyleBox godotStyleBox)
        {
            GodotStyleBox = godotStyleBox;
        }

        internal override Godot.StyleBox GodotStyleBox { get; }

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box)
        {
            throw new NotImplementedException();
        }
    }
}
