using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenToolkit.GraphicsLibraryFramework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using GlfwImage = OpenToolkit.GraphicsLibraryFramework.Image;
using Cursor = OpenToolkit.GraphicsLibraryFramework.Cursor;

namespace Robust.Client.Interfaces.Graphics
{
    public interface IClyde
    {
        Vector2i ScreenSize { get; }
        void SetWindowTitle(string title);
        void CreateCursor(GlfwImage image, int x, int y);

        void CreatePngCursor(string png);

        void SetCursor(Cursor cursor, Window window);

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
