using SixLabors.ImageSharp.PixelFormats;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;

namespace Robust.Client.Utility
{
    public static class ImageSharpExt
    {
        public static Color ConvertImgSharp(this Rgba32 color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        public static Rgba32 ConvertImgSharp(this Color color)
        {
            return new Rgba32(color.R, color.G, color.B, color.A);
        }

        /// <summary>
        ///     Blit an image into another, with the specified offset.
        /// </summary>
        /// <param name="source">The image to copy data from.</param>
        /// <param name="sourceRect">The sub section of <see cref="source"/> that will be copied.</param>
        /// <param name="destinationOffset">
        ///     The offset into <see cref="destination"/> that data will be copied into.
        /// </param>
        /// <param name="destination">The image to copy to.</param>
        /// <typeparam name="T">The type of pixel stored in the images.</typeparam>
        public static void Blit<T>(this Image<T> source, UIBox2i sourceRect,
                Image<T> destination, Vector2i destinationOffset)
            where T : struct, IPixel<T>
        {
            // TODO: Bounds checks.

            var srcSpan = source.GetPixelSpan();
            var dstSpan = destination.GetPixelSpan();

            var srcWidth = source.Width;
            var dstWidth = destination.Width;

            var (ox, oy) = destinationOffset;

            for (var x = 0; x < sourceRect.Width; x++)
            {
                for (var y = 0; y < sourceRect.Height; y++)
                {
                    var pixel = srcSpan[x + sourceRect.Left + srcWidth * (y + sourceRect.Top)];
                    dstSpan[x + ox + dstWidth * (y + oy)] = pixel;
                }
            }
        }
    }
}
