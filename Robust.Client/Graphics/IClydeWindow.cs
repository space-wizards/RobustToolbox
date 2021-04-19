using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public interface IClydeWindow : IDisposable
    {
        bool IsDisposed { get; }
        IRenderTarget RenderTarget { get; }
        string Title { get; set; }
        Vector2i Size { get; }
        bool IsFocused { get; }
        bool IsMinimized { get; }
        bool IsVisible { get; set; }
        event Action<WindowClosedEventArgs> Closed;
    }
}
