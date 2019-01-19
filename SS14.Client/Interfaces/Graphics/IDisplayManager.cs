namespace SS14.Client.Interfaces.Graphics
{
    /// <summary>
    ///     Manages the game window, resolutions, fullscreen mode, vsync, etc...
    /// </summary>
    public interface IDisplayManager
    {
        void SetWindowTitle(string title);
        void Initialize();
        void ReloadConfig();
    }
}
