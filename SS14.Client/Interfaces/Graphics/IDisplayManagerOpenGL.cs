using System.IO;
using SS14.Client.Graphics;

namespace SS14.Client.Interfaces.Graphics
{
    internal interface IDisplayManagerOpenGL : IDisplayManager
    {
        void Render(FrameEventArgs args);
        void ProcessInput(FrameEventArgs frameEventArgs);

        Texture LoadTextureFromPNGStream(Stream stream);
    }
}
