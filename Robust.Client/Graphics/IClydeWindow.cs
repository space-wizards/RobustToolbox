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

        /// <summary>
        ///     If set to true, the user closing the window will also <see cref="IDisposable.Dispose"/> it.
        /// </summary>
        bool DisposeOnClose { get; set; }

        /// <summary>
        ///     Fired when the user tries to close the window. Note that if <see cref="DisposeOnClose"/> is not true,
        ///     this is merely a request and the user pressing the close button does nothing.
        /// </summary>
        event Action<WindowClosedEventArgs> Closed;
    }
}
