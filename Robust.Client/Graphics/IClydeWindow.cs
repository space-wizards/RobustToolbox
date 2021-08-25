using System;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents a single operating system window.
    /// </summary>
    public interface IClydeWindow : IDisposable
    {
        bool IsDisposed { get; }
        WindowId Id { get; }
        IRenderTarget RenderTarget { get; }
        string Title { get; set; }
        Vector2i Size { get; }
        bool IsFocused { get; }
        bool IsMinimized { get; }
        bool IsVisible { get; set; }
        Vector2 ContentScale { get; }

        /// <summary>
        ///     If set to true, the user closing the window will also <see cref="IDisposable.Dispose"/> it.
        /// </summary>
        bool DisposeOnClose { get; set; }

        /// <summary>
        ///     Fired when the user tries to close the window. Note that if <see cref="DisposeOnClose"/> is not true,
        ///     this is merely a request and the user pressing the close button does nothing.
        /// </summary>
        event Action<WindowRequestClosedEventArgs> RequestClosed;

        /// <summary>
        /// Raised when the window has been definitively closed.
        /// This means the window must not be used anymore (it is disposed).
        /// </summary>
        event Action<WindowDestroyedEventArgs> Destroyed;
    }

    public interface IClydeWindowInternal : IClydeWindow
    {
        nint? WindowsHWnd { get; }
    }
}
