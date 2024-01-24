using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using SDL;
using static SDL.SDL;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class SdlWindowingImpl : IWindowingImpl
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly Clyde _clyde;
        private GCHandle _selfGcHandle;

        private readonly ISawmill _sawmill;
        private readonly ISawmill _sawmillSdl2;

        public SdlWindowingImpl(Clyde clyde, IDependencyCollection deps)
        {
            _clyde = clyde;
            deps.InjectDependencies(this, true);

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
            _selfGcHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            SDL_LogSetAllPriority(SDL_LogPriority.Verbose);
            SDL_LogSetOutputFunction((category, priority, description) =>
            {
                var level = priority switch
                {
                    SDL_LogPriority.Verbose => LogLevel.Verbose,
                    SDL_LogPriority.Debug => LogLevel.Debug,
                    SDL_LogPriority.Info => LogLevel.Info,
                    SDL_LogPriority.Warn => LogLevel.Warning,
                    SDL_LogPriority.Error => LogLevel.Error,
                    SDL_LogPriority.Critical => LogLevel.Fatal,
                    _ => LogLevel.Error
                };

                if (description == "That operation is not supported")
                {
                    _sawmillSdl2.Info(Environment.StackTrace);
                }

                _sawmillSdl2.Log(level, $"[{category.ToString().ToLower()}] {description}");
            });

            SDL_SetHint("SDL_WINDOWS_DPI_SCALING", true);
            SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, true);
            SDL_SetHint("SDL_HINT_IME_SUPPORT_EXTENDED_TEXT", true);
            SDL_SetHint(SDL_HINT_IME_SHOW_UI, true);

            if (SDL_Init(SDL_InitFlags.Video | SDL_InitFlags.Events) < 0)
            {
                _sawmill.Fatal("Failed to initialize SDL3: {error}", SDL_GetErrorString());
                return false;
            }

            SDL_GetVersion(out var version);
            var videoDriver = SDL_GetCurrentVideoDriverString();
            _sawmill.Debug(
                "SDL2 initialized, version: {major}.{minor}.{patch}, video driver: {videoDriver}", version.major, version.minor, version.patch, videoDriver);

            _sdlEventWakeup = SDL_RegisterEvents(1);
            // SDL_EventState(SDLUtils.SDL_EventType.SDL_SYSWMEVENT, SDL_ENABLE); // deprecated, does not exist anymore

            InitCursors();
            InitMonitors();
            ReloadKeyMap();

            SDL_AddEventWatch(EventWatch, GCHandle.ToIntPtr(_selfGcHandle));

            // SDL defaults to having text input enabled, so we have to manually turn it off in init for consistency.
            // If we don't, text input will remain enabled *until* the user first leaves a LineEdit/TextEdit.
            SDL_StopTextInput();
            return true;
        }

        public unsafe void Shutdown()
        {
            if (_selfGcHandle != default)
            {
                SDL_DelEventWatch(EventWatch, GCHandle.ToIntPtr(_selfGcHandle));
                _selfGcHandle.Free();
            }

            SDL_LogSetOutputFunction(null);

            if (SDL_WasInit(SDL_InitFlags.None) != 0)
            {
                _sawmill.Debug("Terminating SDL3");
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
                res = SDL_GL_MakeCurrent(sdlReg.Sdl2Window, (SDL_GLContext) sdlReg.GlContext);
            else
                res = SDL_GL_MakeCurrent(SDL_Window.Null, SDL_GLContext.NULL);

            if (res < 0)
                _sawmill.Error("SDL_GL_MakeCurrent failed: {error}", SDL_GetErrorString());
        }

        public void GLSwapInterval(WindowReg reg, int interval)
        {
            interval = Math.Clamp(interval, -1, 1);
            ((Sdl2WindowReg)reg).SwapInterval = interval;
            if (SDL_GL_SetSwapInterval(interval) < 0)
            {
                _sawmill.Error("SDL_GL_SetSwapInterval failed: {error}", SDL_GetErrorString());
            }
        }

        public unsafe void* GLGetProcAddress(string procName)
        {
            return SDL_GL_GetProcAddress(procName);
        }

        public string GetDescription()
        {
            SDL_GetVersion(out var version);
            _sawmill.Debug(
                "SDL2 initialized, version: {major}.{minor}.{patch}", version.major, version.minor, version.patch);

            var videoDriver = SDL_GetCurrentVideoDriverString();

            return $"SDL2 {version.major}.{version.minor}.{version.patch} ({videoDriver})";
        }
    }
}
