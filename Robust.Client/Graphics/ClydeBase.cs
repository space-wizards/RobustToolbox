using System;
using Robust.Shared;
using Robust.Shared.Configuration;
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
        [Dependency] protected readonly IConfigurationManager ConfigurationManager = default!;

        protected WindowMode WindowMode { get; private set; } = WindowMode.Windowed;
        protected bool VSync { get; private set; } = true;

        public abstract Vector2i ScreenSize { get; }
        public abstract bool IsFocused { get; }

        public abstract void SetWindowTitle(string title);

        public virtual bool Initialize()
        {
            ConfigurationManager.OnValueChanged(CVars.DisplayVSync, _vSyncChanged);
            ConfigurationManager.OnValueChanged(CVars.DisplayWindowMode, _windowModeChanged);
            ConfigurationManager.OnValueChanged(CVars.DisplayLightMapDivider, LightmapDividerChanged);
            ConfigurationManager.OnValueChanged(CVars.DisplayMaxLightsPerScene, MaxLightsPerSceneChanged);
            ConfigurationManager.OnValueChanged(CVars.DisplaySoftShadows, SoftShadowsChanged);

            return true;
        }

        protected virtual void ReloadConfig()
        {
            ReadConfig();
        }

        public abstract event Action<WindowResizedEventArgs> OnWindowResized;

        public abstract event Action<WindowFocusedEventArgs> OnWindowFocused;

        protected virtual void ReadConfig()
        {
            WindowMode = (WindowMode) ConfigurationManager.GetCVar(CVars.DisplayWindowMode);
            VSync = ConfigurationManager.GetCVar(CVars.DisplayVSync);
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

        protected static Vector2i ClampSubRegion(Vector2i size, UIBox2i? subRegionSpecified)
        {
            return subRegionSpecified == null
                ? size
                : UIBox2i.FromDimensions(Vector2i.Zero, size).Intersection(subRegionSpecified.Value)?.Size ?? default;
        }
    }
}
