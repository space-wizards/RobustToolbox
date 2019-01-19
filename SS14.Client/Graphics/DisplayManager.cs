using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;

namespace SS14.Client.Graphics
{
    public enum WindowMode
    {
        Windowed = 0,
        Fullscreen = 1,
        // Maybe add borderless? Not sure how good Godot's default fullscreen is with alt tabbing.
    }

    /// <summary>
    ///     Manages the game window, resolutions, fullscreen mode, VSync, etc...
    /// </summary>
    internal abstract class DisplayManager : IDisplayManager, IPostInjectInit
    {
        private const string CVarVSync = "display.vsync";
        private const string CVarWindowMode = "display.windowmode";

        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] protected readonly IGameControllerProxyInternal _gameController;

        protected WindowMode WindowMode { get; private set; } = WindowMode.Windowed;
        protected bool VSync { get; private set; }

        void IPostInjectInit.PostInject()
        {
            _configurationManager.RegisterCVar(CVarVSync, VSync, CVar.ARCHIVE);
            _configurationManager.RegisterCVar(CVarWindowMode, (int) WindowMode, CVar.ARCHIVE);
        }

        public abstract void SetWindowTitle(string title);
        public abstract void Initialize();

        public virtual void ReloadConfig()
        {
            _readConfig();
        }

        protected void _readConfig()
        {
            WindowMode = (WindowMode) _configurationManager.GetCVar<int>(CVarWindowMode);
            VSync = _configurationManager.GetCVar<bool>(CVarVSync);
        }
    }
}
