using System;
using System.Runtime.InteropServices;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Structure describing a glyph (a visual character)
        /// </summary>
        ////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential)]
        public struct Glyph
        {
            /// <summary>Offset to move horizontically to the next character</summary>
            public int Advance;

            /// <summary>Bounding rectangle of the glyph, in coordinates relative to the baseline</summary>
            public IntRect Bounds;

            /// <summary>Texture coordinates of the glyph inside the font's texture</summary>
            public FloatRect TextureRect;
        }
    }
}
