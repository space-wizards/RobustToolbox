using System;
using System.IO;
using SFont = SFML.Graphics.Font;

namespace SS14.Client.Graphics.Sprites
{
    public class Font : IDisposable
    {
        public readonly SFont SFMLFont;

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
}
