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
        }
    }

    /// <summary>
    ///     Style box based on a 9-patch texture.
    /// </summary>
    public class StyleBoxTexture : StyleBox
    {
        private Godot.StyleBoxTexture stylebox;

        internal override Godot.StyleBox GodotStyleBox { get; }

        public StyleBoxTexture()
        {
            GodotStyleBox = stylebox = new Godot.StyleBoxTexture();
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
            get => stylebox.MarginLeft;
            set => stylebox.MarginLeft = value;
        }

        public float MarginRight
        {
            get => stylebox.MarginRight;
            set => stylebox.MarginRight = value;
        }

        public float MarginTop
        {
            get => stylebox.MarginTop;
            set => stylebox.MarginTop = value;
        }

        public float MarginBottom
        {
            get => stylebox.MarginBottom;
            set => stylebox.MarginBottom = value;
        }

        public Color Modulate
        {
            get => stylebox.ModulateColor.Convert();
            set => stylebox.ModulateColor = value.Convert();
        }

        private Texture cachedTexture;
        public Texture Texture
        {
            get
            {
                return cachedTexture ?? new GodotTextureSource((Godot.Texture)stylebox.Texture);
            }
            // Woo implicit casts.
            set => stylebox.Texture = cachedTexture = value;
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
}
