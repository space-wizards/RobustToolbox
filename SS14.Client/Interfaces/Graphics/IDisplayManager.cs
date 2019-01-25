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
    }
}
