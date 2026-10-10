using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Shared.Utility;

public static class ImageLoader
{
    /// <summary>
    ///     Use instead of <see cref="Image.Load{TPixel}(Stream)"/>. Doesn't decode TIFF.
    /// </summary>
    public static Image<T> Load<T>(Stream stream) where T : unmanaged, IPixel<T>
    {
        return ImageOps.LoadImage<T>(stream);
    }
}
