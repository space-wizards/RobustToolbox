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
        internal abstract Godot.StyleBox GodotStyleBox { get; }

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
        public GodotStyleBoxWrap(Godot.StyleBox godotStyleBox)
        {
            GodotStyleBox = godotStyleBox;
        }

        internal override Godot.StyleBox GodotStyleBox { get; }
    }

    /// <summary>
    ///     Style box based on a 9-patch texture.
    /// </summary>
    public class StyleBoxTexture : StyleBox
    {
        private readonly Godot.StyleBoxTexture stylebox;
        internal override Godot.StyleBox GodotStyleBox => stylebox;


        public StyleBoxTexture()
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            stylebox = new Godot.StyleBoxTexture();
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
            get => GameController.OnGodot ? stylebox.MarginLeft : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.MarginLeft = value;
                }
            }
        }

        public float MarginRight
        {
            get => GameController.OnGodot ? stylebox.MarginRight : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.MarginRight = value;
                }
            }
        }

        public float MarginTop
        {
            get => GameController.OnGodot ? stylebox.MarginTop : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.MarginTop = value;
                }
            }
        }

        public float MarginBottom
        {
            get => GameController.OnGodot ? stylebox.MarginBottom : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.MarginBottom = value;
                }
            }
        }

        public Color Modulate
        {
            get => GameController.OnGodot ? stylebox.ModulateColor.Convert() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ModulateColor = value.Convert();
                }
            }
        }

        private Texture cachedTexture;

        public Texture Texture
        {
            get
            {
                if (!GameController.OnGodot)
                {
                    return null;
                }

                return cachedTexture ?? new GodotTextureSource((Godot.Texture) stylebox.Texture);
            }
            // Woo implicit casts.
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.Texture = cachedTexture = value;
                }
            }
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
            get => GameController.OnGodot ? stylebox.BgColor.Convert() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.BgColor = value.Convert();
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

        public float MarginLeft
        {
            get => GameController.OnGodot ? stylebox.ContentMarginLeft : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ContentMarginLeft = value;
                }
            }
        }

        public float MarginRight
        {
            get => GameController.OnGodot ? stylebox.ContentMarginRight : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ContentMarginRight = value;
                }
            }
        }

        public float MarginTop
        {
            get => GameController.OnGodot ? stylebox.ContentMarginTop : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ContentMarginTop = value;
                }
            }
        }

        public float MarginBottom
        {
            get => GameController.OnGodot ? stylebox.ContentMarginBottom : default;
            set
            {
                if (GameController.OnGodot)
                {
                    stylebox.ContentMarginBottom = value;
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
    }
}
