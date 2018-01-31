using System;
using System.IO;
using SS14.Shared.Maths;
using SFont = SFML.Graphics.Font;

namespace SS14.Client.Graphics.Sprites
{
    public class Font : IDisposable
    {
        public readonly SFont SFMLFont;

        /// <summary>
        /// Get a glyph in the font
        /// </summary>
        /// <param name="codePoint">Unicode code point of the character to get</param>
        /// <param name="characterSize">Character size</param>
        /// <param name="bold">Retrieve the bold version or the regular one?</param>
        /// <param name="outlineThickness">Thickness of outline (when != 0 the glyph will not be filled)</param>
        /// <returns>The glyph corresponding to the character</returns>
        public Glyph GetGlyph(uint codePoint, uint characterSize, bool bold, float outlineThickness)
        {
            return new Glyph(SFMLFont.GetGlyph(codePoint, characterSize, bold, outlineThickness));
        }

        /// <summary>
        /// Get the kerning offset between two glyphs
        /// </summary>
        /// <param name="first">Unicode code point of the first character</param>
        /// <param name="second">Unicode code point of the second character</param>
        /// <param name="characterSize">Character size</param>
        /// <returns>Kerning offset, in pixels</returns>
        public float GetKerning(uint first, uint second, uint characterSize)
        {
            return SFMLFont.GetKerning(first, second, characterSize);
        }

        /// <summary>
        /// Get spacing between two consecutive lines
        /// </summary>
        /// <param name="characterSize">Character size</param>
        /// <returns>Line spacing, in pixels</returns>
        public float GetLineSpacing(uint characterSize)
        {
            return SFMLFont.GetLineSpacing(characterSize);
        }

        /// <summary>
        /// Get the position of the underline
        /// </summary>
        /// <param name="characterSize">Character size</param>
        /// <returns>Underline position, in pixels</returns>
        public float GetUnderlinePosition(uint characterSize)
        {
            return SFMLFont.GetUnderlinePosition(characterSize);
        }

        /// <summary>
        /// Get the thickness of the underline
        /// </summary>
        /// <param name="characterSize">Character size</param>
        /// <returns>Underline thickness, in pixels</returns>
        public float GetUnderlineThickness(uint characterSize)
        {
            return SFMLFont.GetUnderlineThickness(characterSize);
        }

        /// <summary>
        /// Get the texture containing the glyphs of a given size
        /// </summary>
        /// <param name="characterSize">Character size</param>
        /// <returns>Texture storing the glyphs for the given size</returns>
        public Textures.Texture GetTexture(uint characterSize)
        {
            var texture = SFMLFont.GetTexture(characterSize);
            return new Textures.Texture(texture);
        }

        internal Font(SFont font)
        {
            SFMLFont = font;
        }

        /// <summary>
        ///     Creates a font by reading from a stream.
        /// </summary>
        public Font(Stream stream)
        {
            SFMLFont = new SFont(stream);
        }

        /// <summary>
        ///     Creates a new font by copying from another font.
        /// </summary>
        /// <param name="font">The font to copy.</param>
        public Font(Font font)
        {
            SFMLFont = new SFont(font.SFMLFont);
        }

        public Font(byte[] data)
        {
            SFMLFont = new SFont(data);
        }

        public void Dispose() => SFMLFont.Dispose();

        public string Family => SFMLFont.GetInfo().Family;
    }

    /// <summary>
    /// Structure describing a glyph (a visual character)
    /// </summary>
    public struct Glyph
    {
        private SFML.Graphics.Glyph _glyph;

        /// <summary>Offset to move horizontally to the next character</summary>
        public float Advance => _glyph.Advance;

        /// <summary>Bounding rectangle of the glyph, in coordinates relative to the baseline</summary>
        public Box2 Bounds
        {
            get
            {
                var bounds = _glyph.Bounds;
                return Box2.FromDimensions(0, 0, bounds.Width, bounds.Height);
            }
        }

        /// <summary>Texture coordinates of the glyph inside the font's texture</summary>
        public Box2i TextureRect
        {
            get
            {
                var rect = _glyph.TextureRect;
                return Box2i.FromDimensions(0, 0, rect.Width, rect.Height);
            }
        }

        internal Glyph(SFML.Graphics.Glyph glyph)
        {
            _glyph = glyph;
        }
    }
}
