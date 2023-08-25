using System;
using System.IO;
using JetBrains.Annotations;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Robust.Shared.Graphics;

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
}
