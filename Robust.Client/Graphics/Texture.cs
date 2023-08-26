using System;
using System.IO;
using JetBrains.Annotations;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Robust.Client.Graphics;

/// <summary>
///     Contains a texture used for drawing things.
/// </summary>
[PublicAPI]
public abstract class Texture : IRsiStateLike
{
    /// <summary>
    ///     The width of the texture, in pixels.
    /// </summary>
    public int Width => Size.X;

    /// <summary>
    ///     The height of the texture, in pixels.
    /// </summary>
    public int Height => Size.Y;

    /// <summary>
    ///     The size of the texture, in pixels.
    /// </summary>
    public Vector2i Size { get; /*protected set;*/ }

    public Color this[int x, int y] => this.GetPixel(x, y);

    protected Texture(Vector2i size)
    {
        Size = size;
    }

    Texture IDirectionalTextureProvider.Default => this;

    Texture IDirectionalTextureProvider.TextureFor(Direction dir)
    {
        return this;
    }

    RsiDirectionType IRsiStateLike.RsiDirections => RsiDirectionType.Dir1;
    bool IRsiStateLike.IsAnimated => false;
    int IRsiStateLike.AnimationFrameCount => 0;

    float IRsiStateLike.GetDelay(int frame)
    {
        if (frame != 0)
            throw new IndexOutOfRangeException();

        return 0;
    }

    Texture IRsiStateLike.GetFrame(RsiDirection dir, int frame)
    {
        if (frame != 0)
            throw new IndexOutOfRangeException();

        return this;
    }

    public abstract Color GetPixel(int x, int y);

    public static Texture Transparent =>
                IoCManager.Resolve<IClydeInternal>().GetStockTexture(ClydeStockTexture.Transparent);

    public static Texture White =>
        IoCManager.Resolve<IClydeInternal>().GetStockTexture(ClydeStockTexture.White);

    public static Texture Black =>
        IoCManager.Resolve<IClydeInternal>().GetStockTexture(ClydeStockTexture.Black);

    /// <summary>
    ///     Loads a new texture an existing image.
    /// </summary>
    /// <param name="image">The image to load.</param>
    /// <param name="name">The "name" of this texture. This can be referred to later to aid debugging.</param>
    /// <param name="loadParameters">
    ///     Parameters that influence the loading of textures.
    ///     Defaults to <see cref="Robust.Client.Graphics.TextureLoadParameters.Default"/> if <c>null</c>.
    /// </param>
    /// <typeparam name="T">The type of pixels of the image. At the moment, images must be <see cref="Rgba32"/>.</typeparam>
    public static Texture LoadFromImage<T>(Image<T> image, string? name = null,
        TextureLoadParameters? loadParameters = null) where T : unmanaged, IPixel<T>
    {
        var manager = IoCManager.Resolve<IClyde>();
        return manager.LoadTextureFromImage(image, name, loadParameters);
    }

    /// <summary>
    ///     Loads an image from a stream containing PNG data.
    /// </summary>
    /// <param name="stream">The stream to load the image from.</param>
    /// <param name="name">The "name" of this texture. This can be referred to later to aid debugging.</param>
    /// <param name="loadParameters">
    ///     Parameters that influence the loading of textures.
    ///     Defaults to <see cref="Robust.Client.Graphics.TextureLoadParameters.Default"/> if <c>null</c>.
    /// </param>
    public static Texture LoadFromPNGStream(Stream stream, string? name = null,
        TextureLoadParameters? loadParameters = null)
    {
        var manager = IoCManager.Resolve<IClyde>();
        return manager.LoadTextureFromPNGStream(stream, name, loadParameters);
    }
}
