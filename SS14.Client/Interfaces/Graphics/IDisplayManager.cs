using System;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Graphics
{
    /// <summary>
    ///     Manages the game window, resolutions, fullscreen mode, vsync, etc...
    /// </summary>
    public interface IDisplayManager
    {
        Vector2i ScreenSize { get; }
        void SetWindowTitle(string title);
        void Initialize();
        void ReloadConfig();
        event Action<WindowResizedEventArgs> OnWindowResized;
    }

    public class WindowResizedEventArgs : EventArgs
    {
        public WindowResizedEventArgs(Vector2i oldSize, Vector2i newSize)
        {
            OldSize = oldSize;
            NewSize = newSize;
        }

        public Vector2i OldSize { get; }
        public Vector2i NewSize { get; }
    }
}
