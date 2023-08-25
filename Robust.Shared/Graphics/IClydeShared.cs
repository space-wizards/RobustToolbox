using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Shared.Graphics;

public interface IClydeShared
{
    Texture GetStockTexture(ClydeStockTexture stockTexture);

    /// <summary>
    ///     Loads an image from a stream containing PNG data.
    /// </summary>
    /// <param name="stream">The stream to load the image from.</param>
    /// <param name="name">The "name" of this texture. This can be referred to later to aid debugging.</param>
    /// <param name="loadParameters">
    ///     Parameters that influence the loading of textures.
    ///     Defaults to <see cref="TextureLoadParameters.Default"/> if <c>null</c>.
    /// </param>
    OwnedTexture LoadTextureFromPNGStream(Stream stream, string? name = null,
        TextureLoadParameters? loadParams = null);

    /// <summary>
    ///     Loads a new texture an existing image.
    /// </summary>
    /// <param name="image">The image to load.</param>
    /// <param name="name">The "name" of this texture. This can be referred to later to aid debugging.</param>
    /// <param name="loadParameters">
    ///     Parameters that influence the loading of textures.
    ///     Defaults to <see cref="TextureLoadParameters.Default"/> if <c>null</c>.
    /// </param>
    /// <typeparam name="T">The type of pixels of the image. At the moment, images must be <see cref="Rgba32"/>.</typeparam>
    OwnedTexture LoadTextureFromImage<T>(Image<T> image, string? name = null,
        TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>;
}
