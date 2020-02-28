using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics
{
    public interface IClyde
    {
        Vector2i ScreenSize { get; }
        void SetWindowTitle(string title);
        event Action<WindowResizedEventArgs> OnWindowResized;

        Texture LoadTextureFromPNGStream(Stream stream, string name = null,
            TextureLoadParameters? loadParams = null);

        Texture LoadTextureFromImage<T>(Image<T> image, string name = null,
            TextureLoadParameters? loadParams = null) where T : unmanaged, IPixel<T>;

        IRenderTarget CreateRenderTarget(Vector2i size, RenderTargetFormatParameters format,
            TextureSampleParameters? sampleParameters = null, string name = null);
    }

    // TODO: Maybe implement IDisposable for render targets. I got lazy and didn't.
}
