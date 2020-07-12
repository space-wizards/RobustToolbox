using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private Viewport CreateViewport()
        {
            return new Viewport();
        }

        IClydeViewport IClyde.CreateViewport()
        {
            return CreateViewport();
        }

        private sealed class Viewport : IClydeViewport
        {
            public void Dispose()
            {
            }

            public IRenderTarget RenderTarget { get; }
            public IEye Eye { get; set; }

            public void Resize(Vector2i newSize)
            {
            }
        }
    }
}
