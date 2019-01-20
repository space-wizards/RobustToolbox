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
        internal abstract Godot.Font GodotFont { get; }

        public static implicit operator Godot.Font(Font font)
        {
            return font?.GodotFont;
        }
    }

    /// <summary>
    ///     Font type that renders vector fonts such as OTF and TTF fonts from a <see cref="FontResource"/>
    /// </summary>
    public class VectorFont : Font
    {
        public int ExtraSpacingTop
        {
            get => GameController.OnGodot ? _font.ExtraSpacingTop : default;
            set
            {
                if (GameController.OnGodot)
                {
                    _font.ExtraSpacingTop = value;
                }
            }
        }

        public int ExtraSpacingBottom
        {
            get => GameController.OnGodot ? _font.ExtraSpacingBottom : default;
            set
            {
                if (GameController.OnGodot)
                {
                    _font.ExtraSpacingBottom = value;
                }
            }
        }

        public int ExtraSpacingChar
        {
            get => GameController.OnGodot ? _font.ExtraSpacingChar : default;
            set
            {
                if (GameController.OnGodot)
                {
                    _font.ExtraSpacingChar = value;
                }
            }
        }

        public int ExtraSpacingSpace
        {
            get => GameController.OnGodot ? _font.ExtraSpacingSpace : default;
            set
            {
                if (GameController.OnGodot)
                {
                    _font.ExtraSpacingSpace = value;
                }
            }
        }

        public int Size
        {
            get => _font.Size;
            set
            {
                if (GameController.OnGodot)
                {
                    _font.Size = value;
                }
            }
        }

        public bool UseFilter
        {
            get => _font.UseFilter;
            set
            {
                if (GameController.OnGodot)
                {
                    _font.UseFilter = value;
                }
            }
        }

        public bool UseMipmaps
        {
            get => _font.UseMipmaps;
            set
            {
                if (GameController.OnGodot)
                {
                    _font.UseMipmaps = value;
                }
            }
        }

        internal override Godot.Font GodotFont => _font;
        private readonly Godot.DynamicFont _font;

        public VectorFont(FontResource res)
            : this(res.FontData)
        {
        }

        internal VectorFont(Godot.DynamicFontData data)
        {
            if (GameController.OnGodot)
            {
                _font = new Godot.DynamicFont
                {
                    FontData = data,
                };
            }
        }
    }

    internal class GodotWrapFont : Font
    {
        public GodotWrapFont(Godot.Font godotFont)
        {
            GodotFont = godotFont;
        }

        internal override Godot.Font GodotFont { get; }
    }
}
