using System;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Robust.Client.Utility
{
    public static class ImageSharpExt
    {
        public static Color ConvertImgSharp(this Rgba32 color)
        {
            return new(color.R, color.G, color.B, color.A);
        }

        public static Rgba32 ConvertImgSharp(this Color color)
        {
            return new(color.R, color.G, color.B, color.A);
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
            where T : unmanaged, IPixel<T>
        {
            // TODO: Bounds checks.

            Blit(source.GetPixelSpan(), source.Width, sourceRect, destination, destinationOffset);
        }

        public static void Blit<T>(this ReadOnlySpan<T> source, int sourceWidth, UIBox2i sourceRect,
            Image<T> destination, Vector2i destinationOffset) where T : unmanaged, IPixel<T>
        {
            var dstSpan = destination.GetPixelSpan();
            var dstWidth = destination.Width;

            var (ox, oy) = destinationOffset;

            for (var y = 0; y < sourceRect.Height; y++)
            {
                var sourceRowOffset = sourceWidth * (y + sourceRect.Top) + sourceRect.Left;
                var destRowOffset = dstWidth * (y + oy) + ox;

                for (var x = 0; x < sourceRect.Width; x++)
                {
                    var pixel = source[x + sourceRowOffset];
                    dstSpan[x + destRowOffset] = pixel;
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="T:System.Span`1" /> to the backing data if the backing group consists of a single contiguous memory buffer.
        /// </summary>
        /// <returns>The <see cref="T:System.Span`1" /> referencing the memory area.</returns>
        /// <exception cref="ArgumentException">Thrown if the image is not a single contiguous buffer.</exception>
        public static Span<T> GetPixelSpan<T>(this Image<T> image) where T : unmanaged, IPixel<T>
        {
            if (!image.TryGetSinglePixelSpan(out var span))
            {
                throw new ArgumentException("Image is not backed by a single buffer, cannot fetch span.");
            }

            return span;
        }
    }
}

