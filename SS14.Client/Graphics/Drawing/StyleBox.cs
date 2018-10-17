using System;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Drawing
{
    /// <summary>
    ///     Equivalent to Godot's <c>StyleBox</c> class,
    ///     this is for drawing pretty fancy boxes using minimal code.
    /// </summary>
    public abstract class StyleBox
    {
#if GODOT
        internal abstract Godot.StyleBox GodotStyleBox { get; }
#endif

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

    internal class GodotStyleBoxWrap : StyleBox
    {
#if GODOT
        public GodotStyleBoxWrap(Godot.StyleBox godotStyleBox)
        {
            GodotStyleBox = godotStyleBox;
        }

        internal override Godot.StyleBox GodotStyleBox { get; }
#endif
    }

    /// <summary>
    ///     Style box based on a 9-patch texture.
    /// </summary>
    public class StyleBoxTexture : StyleBox
    {
#if GODOT
        private readonly Godot.StyleBoxTexture stylebox;

        internal override Godot.StyleBox GodotStyleBox => stylebox;
#endif

        public StyleBoxTexture()
        {
#if GODOT
            stylebox = new Godot.StyleBoxTexture();
#endif
        }

        /// <summary>
        ///     Clones a stylebox so it can be separately modified.
        /// </summary>
        public StyleBoxTexture(StyleBoxTexture copy) : this()
        {
            MarginTop = copy.MarginTop;
            MarginLeft = copy.MarginLeft;
            MarginBottom = copy.MarginBottom;
            MarginRight = copy.MarginRight;
            Texture = copy.Texture;
            Modulate = copy.Modulate;
        }

        public float MarginLeft
        {
#if GODOT
            get => stylebox.MarginLeft;
            set => stylebox.MarginLeft = value;
#else
            get => default;
            set {}
#endif
        }

        public float MarginRight
        {
#if GODOT
            get => stylebox.MarginRight;
            set => stylebox.MarginRight = value;
#else
            get => default;
            set {}
#endif
        }

        public float MarginTop
        {
#if GODOT
            get => stylebox.MarginTop;
            set => stylebox.MarginTop = value;
#else
            get => default;
            set {}
#endif
        }

        public float MarginBottom
        {
#if GODOT
            get => stylebox.MarginBottom;
            set => stylebox.MarginBottom = value;
#else
            get => default;
            set {}
#endif
        }

        public Color Modulate
        {
#if GODOT
            get => stylebox.ModulateColor.Convert();
            set => stylebox.ModulateColor = value.Convert();
#else
            get => default;
            set {}
#endif
        }

        private Texture cachedTexture;

        public Texture Texture
        {
#if GODOT
            get { return cachedTexture ?? new GodotTextureSource((Godot.Texture) stylebox.Texture); }
            // Woo implicit casts.
            set => stylebox.Texture = cachedTexture = value;
#else
            get => default;
            set {}
#endif
        }

        /// <summary>
        ///     Allows setting multiple margins at once.
        /// </summary>
        public void SetMargin(Margin margin, float value)
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
    }

    public class StyleBoxFlat : StyleBox
    {
        public Color BackgroundColor
        {
#if GODOT
            get => stylebox.BgColor.Convert();
            set => stylebox.BgColor = value.Convert();
#else
            get => default;
            set {}
#endif
        }

#if GODOT
        private readonly Godot.StyleBoxFlat stylebox;

        internal override Godot.StyleBox GodotStyleBox => stylebox;
#endif

        public StyleBoxFlat()
        {
#if GODOT
            stylebox = new Godot.StyleBoxFlat();
#endif
        }

        public float MarginLeft
        {
#if GODOT
            get => stylebox.ContentMarginLeft;
            set => stylebox.ContentMarginLeft = value;
#else
            get => default;
            set {}
#endif
        }

        public float MarginRight
        {
#if GODOT
            get => stylebox.ContentMarginRight;
            set => stylebox.ContentMarginRight = value;
#else
            get => default;
            set {}
#endif
        }

        public float MarginTop
        {
#if GODOT
            get => stylebox.ContentMarginTop;
            set => stylebox.ContentMarginTop = value;
#else
            get => default;
            set {}
#endif
        }

        public float MarginBottom
        {
#if GODOT
            get => stylebox.ContentMarginBottom;
            set => stylebox.ContentMarginBottom = value;
#else
            get => default;
            set {}
#endif
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
    }
}
