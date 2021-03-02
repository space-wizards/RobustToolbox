using System;

namespace Robust.Client.Graphics
{
    public interface IClydeWindow : IDisposable
    {
        bool IsDisposed { get; }
        IRenderTarget RenderTarget { get; }
        string Title { get; set; }
        bool IsFocused { get; }
        bool IsMinimized { get; }
        bool IsVisible { get; set; }
        event Action<WindowClosedEventArgs> Closed;
    }
}
