using System;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
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

        [Dependency] protected readonly IConfigurationManager _configurationManager;
        [Dependency] protected readonly IGameControllerProxyInternal _gameController;

        protected WindowMode WindowMode { get; private set; } = WindowMode.Windowed;
        protected bool VSync { get; private set; } = true;

        public virtual void PostInject()
        {
            _configurationManager.RegisterCVar(CVarVSync, VSync, CVar.ARCHIVE);
            _configurationManager.RegisterCVar(CVarWindowMode, (int) WindowMode, CVar.ARCHIVE);
        }

        public abstract Vector2i ScreenSize { get; }
        public abstract void SetWindowTitle(string title);
        public abstract void Initialize();

        public virtual void ReloadConfig()
        {
            ReadConfig();
        }

        public abstract event Action<WindowResizedEventArgs> OnWindowResized;

        protected virtual void ReadConfig()
        {
            WindowMode = (WindowMode) _configurationManager.GetCVar<int>(CVarWindowMode);
            VSync = _configurationManager.GetCVar<bool>(CVarVSync);
        }
    }
}
