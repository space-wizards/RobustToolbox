using System;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public enum WindowMode : byte
    {
        Windowed = 0,
        Fullscreen = 1,
        // Maybe add borderless? Not sure how good Godot's default fullscreen is with alt tabbing.
    }

    /// <summary>
    ///     Manages the game window, resolutions, fullscreen mode, VSync, etc...
    /// </summary>
    internal abstract class ClydeBase
    {
        [Dependency] protected readonly IConfigurationManager _configurationManager = default!;
        [Dependency] protected readonly IGameControllerInternal _gameController = default!;

        protected WindowMode WindowMode { get; private set; } = WindowMode.Windowed;
        protected bool VSync { get; private set; } = true;

        public abstract Vector2i ScreenSize { get; }
        public abstract void SetWindowTitle(string title);

        public virtual bool Initialize()
        {
            _configurationManager.OnValueChanged(CVars.DisplayVSync, _vSyncChanged, true);
            _configurationManager.OnValueChanged(CVars.DisplayWindowMode, _windowModeChanged, true);
            _configurationManager.OnValueChanged(CVars.DisplayLightMapDivider, LightmapDividerChanged, true);
            _configurationManager.OnValueChanged(CVars.DisplayMaxLightsPerScene, MaxLightsPerSceneChanged, true);
            _configurationManager.OnValueChanged(CVars.DisplaySoftShadows, SoftShadowsChanged, true);

            return true;
        }

        protected virtual void ReloadConfig()
        {
            ReadConfig();
        }

        public abstract event Action<WindowResizedEventArgs> OnWindowResized;

        protected virtual void ReadConfig()
        {
            WindowMode = (WindowMode) _configurationManager.GetCVar(CVars.DisplayWindowMode);
            VSync = _configurationManager.GetCVar(CVars.DisplayVSync);
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
            WindowMode = (WindowMode) newValue;
            WindowModeChanged();
        }

        protected virtual void WindowModeChanged()
        {
        }

        protected virtual void LightmapDividerChanged(int newValue)
        {
        }

        protected virtual void MaxLightsPerSceneChanged(int newValue)
        {
        }

        protected virtual void SoftShadowsChanged(bool newValue)
        {
        }
    }
}
