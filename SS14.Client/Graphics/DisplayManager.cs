using System;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Utility;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    public enum WindowMode
    {
        Windowed = 0,
        Fullscreen = 1,
        // Maybe add borderless? Not sure how good Godot's default fullscreen is with alt tabbing.
    }

    /// <summary>
    ///     Manages the game window, resolutions, fullscreen mode, vsync, etc...
    /// </summary>
    public class DisplayManager : IDisplayManager, IPostInjectInit
    {
        [Dependency]
        readonly IConfigurationManager configurationManager;

        public WindowMode WindowMode { get; private set; } = WindowMode.Windowed;
        public bool VSync { get; private set; } = false;

        void IPostInjectInit.PostInject()
        {
            configurationManager.RegisterCVar("display.vsync", VSync, CVar.ARCHIVE);
            configurationManager.RegisterCVar("display.windowmode", (int)WindowMode, CVar.ARCHIVE);
        }

        public void Initialize()
        {
            ReadConfig();
        }

        public void ReadConfig()
        {
            WindowMode = (WindowMode)configurationManager.GetCVar<int>("display.windowmode");
            VSync = configurationManager.GetCVar<bool>("display.vsync");

            Godot.OS.VsyncEnabled = VSync;
            Godot.OS.WindowFullscreen = WindowMode == WindowMode.Fullscreen;
        }
    }
}
