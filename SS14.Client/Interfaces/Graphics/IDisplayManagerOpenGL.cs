using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SS14.Client.Graphics;

namespace SS14.Client.Interfaces.Graphics
{
    internal interface IDisplayManagerOpenGL : IDisplayManager
    {
        void Render(FrameEventArgs args);
        void ProcessInput(FrameEventArgs frameEventArgs);

        Texture LoadTextureFromPNGStream(Stream stream, string name=null);
        Texture LoadTextureFromImage<T>(Image<T> image, string name=null) where T : struct, IPixel<T>;
        void Ready();
    }
}
