using System;
using System.Runtime.InteropServices;
using Robust.Client.Input;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using static SDL2.SDL;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl : IWindowingImpl
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        private readonly Clyde _clyde;

        private readonly ISawmill _sawmill;
        private readonly ISawmill _sawmillSdl2;

        public Sdl2WindowingImpl(Clyde clyde)
        {
            _clyde = clyde;
            IoCManager.InjectDependencies(this);

            _sawmill = _logManager.GetSawmill("clyde.win");
            _sawmillSdl2 = _logManager.GetSawmill("clyde.win.sdl2");
        }

        public bool Init()
        {
            InitChannels();

            if (!InitSdl2())
                return false;

            return true;
        }

        private bool InitSdl2()
        {
            StoreCallbacks();

            SDL_LogSetAllPriority(SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE);
            SDL_LogSetOutputFunction(_logOutputFunction!, IntPtr.Zero);

            SDL_SetHint("SDL_WINDOWS_DPI_SCALING", "1");

            var res = SDL_Init(SDL_INIT_VIDEO | SDL_INIT_EVENTS);
            if (res < 0)
            {
                _sawmill.Fatal("Failed to initialize SDL2: {error}", SDL_GetError());
                return false;
            }

            SDL_GetVersion(out var version);
            _sawmill.Debug(
                "SDL2 initialized, version: {major}.{minor}.{patch}", version.major, version.minor, version.patch);

            _sdlEventWakeup = SDL_RegisterEvents(1);

            InitCursors();
            InitMonitors();
            InitKeyMap();

            SDL_AddEventWatch(_eventWatch, IntPtr.Zero);

            return true;
        }

        public void Shutdown()
        {
            if (SDL_WasInit(0) != 0)
            {
                _sawmill.Debug("Terminating SDL2");
                SDL_Quit();
            }
        }

        public void FlushDispose()
        {
            // Not currently used
        }

        public void GLMakeContextCurrent(WindowReg? reg)
        {
            int res;
            if (reg is Sdl2WindowReg sdlReg)
                res = SDL_GL_MakeCurrent(sdlReg.Sdl2Window, sdlReg.GlContext);
            else
                res = SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero);

            if (res < 0)
                _sawmill.Error("SDL_GL_MakeCurrent failed: {error}", SDL_GetError());
        }

        public void GLSwapInterval(WindowReg reg, int interval)
        {
            ((Sdl2WindowReg)reg).SwapInterval = interval;
            SDL_GL_SetSwapInterval(interval);
        }

        public unsafe void* GLGetProcAddress(string procName)
        {
            return (void*) SDL_GL_GetProcAddress(procName);
        }

        private void LogOutputFunction(IntPtr userdata, int category, SDL_LogPriority priority, IntPtr message)
        {
            var level = priority switch
            {
                SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE => LogLevel.Verbose,
                SDL_LogPriority.SDL_LOG_PRIORITY_DEBUG => LogLevel.Debug,
                SDL_LogPriority.SDL_LOG_PRIORITY_INFO => LogLevel.Info,
                SDL_LogPriority.SDL_LOG_PRIORITY_WARN => LogLevel.Warning,
                SDL_LogPriority.SDL_LOG_PRIORITY_ERROR => LogLevel.Error,
                SDL_LogPriority.SDL_LOG_PRIORITY_CRITICAL => LogLevel.Fatal,
                _ => LogLevel.Error
            };

            var msg = Marshal.PtrToStringUTF8(message) ?? "";
            _sawmillSdl2.Log(level, msg);
        }
    }
}
