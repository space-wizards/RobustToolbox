using System;
using OpenToolkit.GraphicsLibraryFramework;
using GlfwImage = OpenToolkit.GraphicsLibraryFramework.Image;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
    internal abstract class ClydeBase : IPostInjectInit
    {
        private const string CVarVSync = "display.vsync";
        private const string CVarWindowMode = "display.windowmode";

#pragma warning disable 649
        [Dependency] protected readonly IConfigurationManager _configurationManager;
        [Dependency] protected readonly IGameControllerInternal _gameController;
#pragma warning restore 649

        protected WindowMode WindowMode { get; private set; } = WindowMode.Windowed;
        protected bool VSync { get; private set; } = true;

        public virtual void PostInject()
        {
            _configurationManager.RegisterCVar(CVarVSync, VSync, CVar.ARCHIVE, _vSyncChanged);
            _configurationManager.RegisterCVar(CVarWindowMode, (int) WindowMode, CVar.ARCHIVE, _windowModeChanged);
            _configurationManager.RegisterCVar("display.width", 1280);
            _configurationManager.RegisterCVar("display.height", 720);
            _configurationManager.RegisterCVar("display.highreslights", false, onValueChanged: HighResLightsChanged);
            _configurationManager.RegisterCVar("audio.device", "");
        }

        public abstract Vector2i ScreenSize { get; }
        public abstract void SetWindowTitle(string title);

        public abstract void CreateCursor(GlfwImage image, int x, int y);
        public abstract bool Initialize();

        protected virtual void ReloadConfig()
        {
            ReadConfig();
        }

        public abstract event Action<WindowResizedEventArgs> OnWindowResized;

        protected virtual void ReadConfig()
        {
            WindowMode = (WindowMode) _configurationManager.GetCVar<int>(CVarWindowMode);
            VSync = _configurationManager.GetCVar<bool>(CVarVSync);
        }

        private void _vSyncChanged(bool newValue)
        {
            VSync = newValue;
            VSyncChanged();
        }

        protected virtual void VSyncChanged()
        {
        }

        private void _windowModeChanged(int newValue)
        {
            WindowMode = (Graphics.WindowMode)newValue;
            WindowModeChanged();
        }

        protected virtual void WindowModeChanged()
        {
        }

        protected virtual void HighResLightsChanged(bool newValue)
        {
        }

        public abstract void CreatePngCursor(string png);
    }
}
