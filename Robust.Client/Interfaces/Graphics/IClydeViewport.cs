using System;
using Robust.Client.Interfaces.Graphics.ClientEye;

namespace Robust.Client.Interfaces.Graphics
{
    /// <summary>
    ///     A viewport is responsible for rendering a section of the game map centered around an eye.
    /// </summary>
    public interface IClydeViewport : IDisposable
    {
        IRenderTarget RenderTarget { get; }
        IEye? Eye { get; set; }
    }
}
