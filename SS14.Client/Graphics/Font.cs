using System;
using SS14.Client.ResourceManagement;

namespace SS14.Client.Graphics
{
    /// <summary>
    ///     A generic font for rendering of text.
    ///     Does not contain properties such as size. Those are specific to children such as <see cref="VectorFont" />
    /// </summary>
    public abstract class Font
    {
#if GODOT
        internal abstract Godot.Font GodotFont { get; }

        public static implicit operator Godot.Font(Font font)
        {
            return font?.GodotFont;
        }
#endif
    }

    /// <summary>
    ///     Font type that renders vector fonts such as OTF and TTF fonts from a <see cref="FontResource"/>
    /// </summary>
    public class VectorFont : Font
    {
#if GODOT
        public int ExtraSpacingTop { get => _font.ExtraSpacingTop; set => _font.ExtraSpacingTop = value; }
        public int ExtraSpacingBottom { get => _font.ExtraSpacingBottom; set => _font.ExtraSpacingBottom = value; }
        public int ExtraSpacingChar { get => _font.ExtraSpacingChar; set => _font.ExtraSpacingChar = value; }
        public int ExtraSpacingSpace { get => _font.ExtraSpacingSpace; set => _font.ExtraSpacingSpace = value; }

        public int Size { get => _font.Size; set => _font.Size = value; }
        public bool UseFilter { get => _font.UseFilter; set => _font.UseFilter = value; }
        public bool UseMipmaps { get => _font.UseMipmaps; set => _font.UseMipmaps = value; }
#else
        public int ExtraSpacingTop
        {
            get => default;
            set {}
        }

        public int ExtraSpacingBottom
        {
            get => default;
            set {}
        }

        public int ExtraSpacingChar
        {
            get => default;
            set {}
        }

        public int ExtraSpacingSpace
        {
            get => default;
            set {}
        }

        public int Size
        {
            get => default;
            set {}
        }

        public bool UseFilter
        {
            get => default;
            set {}
        }

        public bool UseMipmaps
        {
            get => default;
            set {}
        }
#endif

#if GODOT
        internal override Godot.Font GodotFont => _font;
        private readonly Godot.DynamicFont _font;
#endif

        public VectorFont(FontResource res)
#if GODOT
            : this(res.FontData)
#endif
        {
        }

#if GODOT
        internal VectorFont(Godot.DynamicFontData data)
        {
            _font = new Godot.DynamicFont
            {
                FontData = data,
            };
        }
#endif
    }

#if GODOT
    internal class GodotWrapFont : Font
    {
        public GodotWrapFont(Godot.Font godotFont)
        {
            GodotFont = godotFont;
        }

        internal override Godot.Font GodotFont { get; }
    }
#endif
}
