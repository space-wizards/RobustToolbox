using System;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;
using System.Runtime.ConstrainedExecution;
using SFML.Window;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Image is the low-level class for loading and
        /// manipulating images
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class Image : ObjectBase
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the image with black color
            /// </summary>
            /// <param name="width">Image width</param>
            /// <param name="height">Image height</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Image(uint width, uint height) :
                this(width, height, Color.Black)
            {
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the image from a single color
            /// </summary>
            /// <param name="width">Image width</param>
            /// <param name="height">Image height</param>
            /// <param name="color">Color to fill the image with</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Image(uint width, uint height, Color color) :
                base(sfImage_createFromColor(width, height, color))
            {
                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("image");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the image from a file
            /// </summary>
            /// <param name="filename">Path of the image file to load</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Image(string filename) :
                base(sfImage_createFromFile(filename))
            {
                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("image", filename);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the image from a file in a stream
            /// </summary>
            /// <param name="stream">Stream containing the file contents</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Image(Stream stream) :
                base(IntPtr.Zero)
            {
                using (StreamAdaptor adaptor = new StreamAdaptor(stream))
                {
                    SetThis(sfImage_createFromStream(adaptor.InputStreamPtr));
                }

                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("image");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the image directly from an array of pixels
            /// </summary>
            /// <param name="pixels">2 dimensions array containing the pixels</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Image(Color[,] pixels) :
                base(IntPtr.Zero)
            {
                unsafe
                {
                    fixed (Color* PixelsPtr = pixels)
                    {
                        uint Width  = (uint)pixels.GetLength(0);
                        uint Height = (uint)pixels.GetLength(1);
                        SetThis(sfImage_createFromPixels(Width, Height, (byte*)PixelsPtr));
                    }
                }

                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("image");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the image directly from an array of pixels
            /// </summary>
            /// <param name="width">Image width</param>
            /// <param name="height">Image height</param>
            /// <param name="pixels">array containing the pixels</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public Image(uint width, uint height, byte[] pixels) :
                base(IntPtr.Zero)
            {
                unsafe
                {
                    fixed (byte* PixelsPtr = pixels)
                    {
                        SetThis(sfImage_createFromPixels(width, height, PixelsPtr));
                    }
                }

                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("image");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the image from another image
            /// </summary>
            /// <param name="copy">Image to copy</param>
            ////////////////////////////////////////////////////////////
            public Image(Image copy) :
                base(sfImage_copy(copy.CPointer))
            {
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Save the contents of the image to a file
            /// </summary>
            /// <param name="filename">Path of the file to save (overwritten if already exist)</param>
            /// <returns>True if saving was successful</returns>
            ////////////////////////////////////////////////////////////
            public bool SaveToFile(string filename)
            {
                return sfImage_saveToFile(CPointer, filename);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Create a transparency mask from a specified colorkey
            /// </summary>
            /// <param name="color">Color to become transparent</param>
            ////////////////////////////////////////////////////////////
            public void CreateMaskFromColor(Color color)
            {
                CreateMaskFromColor(color, 0);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Create a transparency mask from a specified colorkey
            /// </summary>
            /// <param name="color">Color to become transparent</param>
            /// <param name="alpha">Alpha value to use for transparent pixels</param>
            ////////////////////////////////////////////////////////////
            public void CreateMaskFromColor(Color color, byte alpha)
            {
                sfImage_createMaskFromColor(CPointer, color, alpha);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Copy pixels from another image onto this one.
            /// This function does a slow pixel copy and should only
            /// be used at initialization time
            /// </summary>
            /// <param name="source">Source image to copy</param>
            /// <param name="destX">X coordinate of the destination position</param>
            /// <param name="destY">Y coordinate of the destination position</param>
            ////////////////////////////////////////////////////////////
            public void Copy(Image source, uint destX, uint destY)
            {
                Copy(source, destX, destY, new IntRect(0, 0, 0, 0));
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Copy pixels from another image onto this one.
            /// This function does a slow pixel copy and should only
            /// be used at initialization time
            /// </summary>
            /// <param name="source">Source image to copy</param>
            /// <param name="destX">X coordinate of the destination position</param>
            /// <param name="destY">Y coordinate of the destination position</param>
            /// <param name="sourceRect">Sub-rectangle of the source image to copy</param>
            ////////////////////////////////////////////////////////////
            public void Copy(Image source, uint destX, uint destY, IntRect sourceRect)
            {
                Copy(source, destX, destY, sourceRect, false);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Copy pixels from another image onto this one.
            /// This function does a slow pixel copy and should only
            /// be used at initialization time
            /// </summary>
            /// <param name="source">Source image to copy</param>
            /// <param name="destX">X coordinate of the destination position</param>
            /// <param name="destY">Y coordinate of the destination position</param>
            /// <param name="sourceRect">Sub-rectangle of the source image to copy</param>
            /// <param name="applyAlpha">Should the copy take in account the source transparency?</param>
            ////////////////////////////////////////////////////////////
            public void Copy(Image source, uint destX, uint destY, IntRect sourceRect, bool applyAlpha)
            {
                sfImage_copyImage(CPointer, source.CPointer, destX, destY, sourceRect, applyAlpha);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get a pixel from the image
            /// </summary>
            /// <param name="x">X coordinate of pixel in the image</param>
            /// <param name="y">Y coordinate of pixel in the image</param>
            /// <returns>Color of pixel (x, y)</returns>
            ////////////////////////////////////////////////////////////
            public Color GetPixel(uint x, uint y)
            {
                return sfImage_getPixel(CPointer, x, y);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change the color of a pixel
            /// </summary>
            /// <param name="x">X coordinate of pixel in the image</param>
            /// <param name="y">Y coordinate of pixel in the image</param>
            /// <param name="color">New color for pixel (x, y)</param>
            ////////////////////////////////////////////////////////////
            public void SetPixel(uint x, uint y, Color color)
            {
                sfImage_setPixel(CPointer, x, y, color);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get a copy of the array of pixels (RGBA 8 bits integers components)
            /// Array size is Width x Height x 4
            /// </summary>
            /// <returns>Array of pixels</returns>
            ////////////////////////////////////////////////////////////
            public byte[] Pixels
            {
                get
                {
                    Vector2u size = Size;
                    byte[] PixelsPtr = new byte[size.X * size.Y * 4];
                    Marshal.Copy(sfImage_getPixelsPtr(CPointer), PixelsPtr, 0, PixelsPtr.Length);
                    return PixelsPtr;
                }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Size of the image, in pixels
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Vector2u Size
            {
                get {return sfImage_getSize(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Flip the image horizontally
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void FlipHorizontally()
            {
                sfImage_flipHorizontally(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Flip the image vertically
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void FlipVertically()
            {
                sfImage_flipVertically(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Provide a string describing the object
            /// </summary>
            /// <returns>String description of the object</returns>
            ////////////////////////////////////////////////////////////
            public override string ToString()
            {
                return "[Image]" +
                       " Size(" + Size + ")";
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Internal constructor
            /// </summary>
            /// <param name="cPointer">Pointer to the object in C library</param>
            ////////////////////////////////////////////////////////////
            internal Image(IntPtr cPointer) :
                base(cPointer)
            {
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                sfImage_destroy(CPointer);
            }

            #region Imports
            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfImage_createFromColor(uint Width, uint Height, Color Col);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            unsafe static extern IntPtr sfImage_createFromPixels(uint Width, uint Height, byte* Pixels);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfImage_createFromFile(string Filename);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            unsafe static extern IntPtr sfImage_createFromStream(IntPtr stream);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfImage_copy(IntPtr Image);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfImage_destroy(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfImage_saveToFile(IntPtr CPointer, string Filename);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfImage_createMaskFromColor(IntPtr CPointer, Color Col, byte Alpha);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfImage_copyImage(IntPtr CPointer, IntPtr Source, uint DestX, uint DestY, IntRect SourceRect, bool applyAlpha);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfImage_setPixel(IntPtr CPointer, uint X, uint Y, Color Col);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Color sfImage_getPixel(IntPtr CPointer, uint X, uint Y);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfImage_getPixelsPtr(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Vector2u sfImage_getSize(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfImage_flipHorizontally(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfImage_flipVertically(IntPtr CPointer);
            #endregion
        }
    }
}
