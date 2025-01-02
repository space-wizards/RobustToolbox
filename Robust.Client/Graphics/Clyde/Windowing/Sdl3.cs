using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using static SDL3.SDL;
using static SDL3.SDL.SDL_LogCategory;
using static SDL3.SDL.SDL_InitFlags;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl : IWindowingImpl
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly Clyde _clyde;
        private GCHandle _selfGCHandle;

        private readonly ISawmill _sawmill;
        private readonly ISawmill _sawmillSdl3;

        private SdlVideoDriver _videoDriver;

        public Sdl3WindowingImpl(Clyde clyde, IDependencyCollection deps)
        {
            _clyde = clyde;
            deps.InjectDependencies(this, true);

            _sawmill = _logManager.GetSawmill("clyde.win");
            _sawmillSdl3 = _logManager.GetSawmill("clyde.win.sdl3");
        }

        public bool Init()
        {
            InitChannels();

            if (!InitSdl3())
                return false;

            return true;
        }

        private unsafe bool InitSdl3()
        {
            _selfGCHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            SDL_SetLogPriorities(SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE);
            SDL_SetLogOutputFunction(&LogOutputFunction, (void*) GCHandle.ToIntPtr(_selfGCHandle));

            SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL_SetHint(SDL_HINT_IME_IMPLEMENTED_UI, "composition");

            // SDL3's GameInput support is currently broken and leaving it on
            // causes a "that operation is not supported" error to be logged on startup.
            // https://github.com/libsdl-org/SDL/issues/11813
            SDL_SetHint(SDL_HINT_WINDOWS_GAMEINPUT, "0");

            var res = SDL_Init(SDL_INIT_VIDEO | SDL_INIT_EVENTS);
            if (!res)
            {
                _sawmill.Fatal("Failed to initialize SDL3: {error}", SDL_GetError());
                return false;
            }

            var version = SDL_GetVersion();
            var videoDriver = SDL_GetCurrentVideoDriver();
            _sawmill.Debug(
                "SDL3 initialized, version: {major}.{minor}.{patch}, video driver: {videoDriver}",
                SDL_VERSIONNUM_MAJOR(version),
                SDL_VERSIONNUM_MINOR(version),
                SDL_VERSIONNUM_MICRO(version),
                videoDriver);

            LoadSdl3VideoDriver();

            _sdlEventWakeup = SDL_RegisterEvents(1);
            if (_sdlEventWakeup == 0)
                throw new InvalidOperationException("SDL_RegisterEvents failed");

            InitCursors();
            InitMonitors();
            ReloadKeyMap();

            SDL_AddEventWatch(&EventWatch, (void*) GCHandle.ToIntPtr(_selfGCHandle));

            return true;
        }

        private void CheckThreadApartment()
        {
            if (!OperatingSystem.IsWindows())
                return;

            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                _sawmill.Error("Thread apartment state isn't STA. This will likely break things!!!");
        }

        private void LoadSdl3VideoDriver()
        {
            _videoDriver = SDL_GetCurrentVideoDriver() switch
            {
                "windows" => SdlVideoDriver.Windows,
                "x11" => SdlVideoDriver.X11,
                _ => SdlVideoDriver.Other,
            };
        }

        public unsafe void Shutdown()
        {
            if (_selfGCHandle != default)
            {
                SDL_RemoveEventWatch(&EventWatch, (void*) GCHandle.ToIntPtr(_selfGCHandle));
                _selfGCHandle.Free();
                _selfGCHandle = default;
            }

            SDL_SetLogOutputFunction(null, null);

            if (SDL_WasInit(0) != 0)
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
            SDLBool res;
            if (reg is Sdl3WindowReg sdlReg)
                res = SDL_GL_MakeCurrent(sdlReg.Sdl3Window, sdlReg.GlContext);
            else
                res = SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero);

            if (!res)
                _sawmill.Error("SDL_GL_MakeCurrent failed: {error}", SDL_GetError());
        }

        public void GLSwapInterval(WindowReg reg, int interval)
        {
            ((Sdl3WindowReg)reg).SwapInterval = interval;
            SDL_GL_SetSwapInterval(interval);
        }

        public unsafe void* GLGetProcAddress(string procName)
        {
            return (void*) SDL_GL_GetProcAddress(procName);
        }

        public string GetDescription()
        {
            var version = SDL_GetVersion();

            var major = SDL_VERSIONNUM_MAJOR(version);
            var minor = SDL_VERSIONNUM_MINOR(version);
            var micro = SDL_VERSIONNUM_MICRO(version);

            var videoDriver = SDL_GetCurrentVideoDriver();
            var revision = SDL_GetRevision();

            return $"SDL {major}.{minor}.{micro} (rev: {revision}, video driver: {videoDriver})";
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe void LogOutputFunction(
            void* userdata,
            int category,
            SDL_LogPriority priority,
            byte* message)
        {
            var obj = (Sdl3WindowingImpl) GCHandle.FromIntPtr((IntPtr)userdata).Target!;

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

            var msg = Marshal.PtrToStringUTF8((IntPtr) message) ?? "";
            var categoryName = SdlLogCategoryName(category);
            obj._sawmillSdl3.Log(level, $"[{categoryName}] {msg}");
        }

        private static string SdlLogCategoryName(int category)
        {
            return (SDL_LogCategory) category switch {
                // @formatter:off
                SDL_LOG_CATEGORY_APPLICATION => "application",
                SDL_LOG_CATEGORY_ERROR       => "error",
                SDL_LOG_CATEGORY_ASSERT      => "assert",
                SDL_LOG_CATEGORY_SYSTEM      => "system",
                SDL_LOG_CATEGORY_AUDIO       => "audio",
                SDL_LOG_CATEGORY_VIDEO       => "video",
                SDL_LOG_CATEGORY_RENDER      => "render",
                SDL_LOG_CATEGORY_INPUT       => "input",
                SDL_LOG_CATEGORY_TEST        => "test",
                _                            => "unknown"
                // @formatter:on
            };
        }

        private enum SdlVideoDriver
        {
            // These are the ones we need to be able to check against.
            Other,
            Windows,
            X11
        }
    }
}
