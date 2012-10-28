using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;
using SFML.Window;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Font is the low-level class for loading and
        /// manipulating character fonts. This class is meant to
        /// be used by String2D
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class Font : ObjectBase
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the font from a file
            /// </summary>
            /// <param name="filename">Font file to load</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Font(string filename) :
                base(sfFont_createFromFile(filename))
            {
                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("font", filename);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the font from a custom stream
            /// </summary>
            /// <param name="stream">Source stream to read from</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Font(Stream stream) :
                base(IntPtr.Zero)
            {
                myStream = new StreamAdaptor(stream);
                SetThis(sfFont_createFromStream(myStream.InputStreamPtr));

                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("font");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the font from another font
            /// </summary>
            /// <param name="copy">Font to copy</param>
            ////////////////////////////////////////////////////////////
            public Font(Font copy) :
                base(sfFont_copy(copy.CPointer))
            {
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get a glyph in the font
            /// </summary>
            /// <param name="codePoint">Unicode code point of the character to get</param>
            /// <param name="characterSize">Character size</param>
            /// <param name="bold">Retrieve the bold version or the regular one?</param>
            /// <returns>The glyph corresponding to the character</returns>
            ////////////////////////////////////////////////////////////
            public Glyph GetGlyph(uint codePoint, uint characterSize, bool bold)
            {
                return sfFont_getGlyph(CPointer, codePoint, characterSize, bold);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get the kerning offset between two glyphs
            /// </summary>
            /// <param name="first">Unicode code point of the first character</param>
            /// <param name="second">Unicode code point of the second character</param>
            /// <param name="characterSize">Character size</param>
            /// <returns>Kerning offset, in pixels</returns>
            ////////////////////////////////////////////////////////////
            public int GetKerning(uint first, uint second, uint characterSize)
            {
                return sfFont_getKerning(CPointer, first, second, characterSize);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get spacing between two consecutive lines
            /// </summary>
            /// <param name="characterSize">Character size</param>
            /// <returns>Line spacing, in pixels</returns>
            ////////////////////////////////////////////////////////////
            public int GetLineSpacing(uint characterSize)
            {
                return sfFont_getLineSpacing(CPointer, characterSize);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get the texture containing the glyphs of a given size
            /// </summary>
            /// <param name="characterSize">Character size</param>
            /// <returns>Texture storing the glyphs for the given size</returns>
            ////////////////////////////////////////////////////////////
            public Texture GetTexture(uint characterSize)
            {
                myTextures[characterSize] = new Texture(sfFont_getTexture(CPointer, characterSize));
                return myTextures[characterSize];
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Provide a string describing the object
            /// </summary>
            /// <returns>String description of the object</returns>
            ////////////////////////////////////////////////////////////
            public override string ToString()
            {
                return "[Font]";
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                if (!disposing)
                    Context.Global.SetActive(true);

                sfFont_destroy(CPointer);

                if (disposing)
                {
                    foreach (Texture texture in myTextures.Values)
                        texture.Dispose();

                    if (myStream != null)
                        myStream.Dispose();
                }

                if (!disposing)
                    Context.Global.SetActive(false);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Internal constructor
            /// </summary>
            /// <param name="cPointer">Pointer to the object in C library</param>
            ////////////////////////////////////////////////////////////
            private Font(IntPtr cPointer) :
                base(cPointer)
            {
            }

            private Dictionary<uint, Texture> myTextures = new Dictionary<uint, Texture>();
            private StreamAdaptor myStream = null;

            #region Imports
            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfFont_createFromFile(string Filename);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfFont_createFromStream(IntPtr stream);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfFont_copy(IntPtr Font);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfFont_destroy(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Glyph sfFont_getGlyph(IntPtr CPointer, uint codePoint, uint characterSize, bool bold);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern int sfFont_getKerning(IntPtr CPointer, uint first, uint second, uint characterSize);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern int sfFont_getLineSpacing(IntPtr CPointer, uint characterSize);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfFont_getTexture(IntPtr CPointer, uint characterSize);
            #endregion
        }
    }
}
