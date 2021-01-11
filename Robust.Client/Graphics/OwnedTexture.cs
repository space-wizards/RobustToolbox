using System;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents a mutable texture that can be modified and deleted.
    /// </summary>
    public abstract class OwnedTexture : Texture, IDisposable
    {
        protected OwnedTexture(Vector2i size) : base(size)
        {
        }

        /// <summary>
        ///     Modifies a sub area of the texture with new data.
        /// </summary>
        /// <param name="topLeft">The top left corner of the area to modify.</param>
        /// <param name="sourceImage">The image from which to copy pixel data.</param>
        /// <param name="sourceRegion">The rectangle inside <paramref name="sourceImage"/> from which to copy.</param>
        /// <typeparam name="T">
        /// The type of pixels being used.
        /// This must match the type used when creating the texture.
        /// </typeparam>
        public abstract void SetSubImage<T>(Vector2i topLeft, Image<T> sourceImage, in UIBox2i sourceRegion)
            where T : unmanaged, IPixel<T>;

        /// <summary>
        ///     Modifies a sub area of the texture with new data.
        /// </summary>
        /// <param name="topLeft">The top left corner of the area to modify.</param>
        /// <param name="sourceImage">The image to paste onto the texture.</param>
        /// <typeparam name="T">
        /// The type of pixels being used.
        /// This must match the type used when creating the texture.
        /// </typeparam>
        public void SetSubImage<T>(Vector2i topLeft, Image<T> sourceImage)
            where T : unmanaged, IPixel<T>
        {
            SetSubImage(topLeft, sourceImage, UIBox2i.FromDimensions(0, 0, sourceImage.Width, sourceImage.Height));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~OwnedTexture()
        {
            Dispose(false);
        }
    }
}
