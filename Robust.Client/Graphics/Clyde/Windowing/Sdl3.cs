using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using SDL3;
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
            CheckThreadApartment();

            _selfGCHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            SDL.SDL_SetLogPriorities(SDL.SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE);
            SDL.SDL_SetLogOutputFunction(&LogOutputFunction, (void*) GCHandle.ToIntPtr(_selfGCHandle));

            SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_IME_IMPLEMENTED_UI, "composition");

            // SDL3's GameInput support is currently broken and leaving it on
            // causes a "that operation is not supported" error to be logged on startup.
            // https://github.com/libsdl-org/SDL/issues/11813
            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_GAMEINPUT, "0");

            var res = SDL.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_VIDEO | SDL.SDL_InitFlags.SDL_INIT_EVENTS);
            if (!res)
            {
                _sawmill.Fatal("Failed to initialize SDL3: {error}", SDL.SDL_GetError());
                return false;
            }

            var version = SDL.SDL_GetVersion();
            var videoDriver = SDL.SDL_GetCurrentVideoDriver();
            _sawmill.Debug(
                "SDL3 initialized, version: {major}.{minor}.{patch}, video driver: {videoDriver}",
                SDL.SDL_VERSIONNUM_MAJOR(version),
                SDL.SDL_VERSIONNUM_MINOR(version),
                SDL.SDL_VERSIONNUM_MICRO(version),
                videoDriver);

            LoadSdl3VideoDriver();

            _sdlEventWakeup = SDL.SDL_RegisterEvents(1);
            if (_sdlEventWakeup == 0)
                throw new InvalidOperationException("SDL_RegisterEvents failed");

            LoadWindowIcons();
            InitCursors();
            InitMonitors();
            ReloadKeyMap();

            SDL.SDL_AddEventWatch(&EventWatch, (void*) GCHandle.ToIntPtr(_selfGCHandle));

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
            _videoDriver = SDL.SDL_GetCurrentVideoDriver() switch
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
                SDL.SDL_RemoveEventWatch(&EventWatch, (void*) GCHandle.ToIntPtr(_selfGCHandle));
                _selfGCHandle.Free();
                _selfGCHandle = default;
            }

            SDL.SDL_SetLogOutputFunction(null, null);

            if (SDL.SDL_WasInit(0) != 0)
            {
                _sawmill.Debug("Terminating SDL3");
                SDL.SDL_Quit();
            }
        }

        public void FlushDispose()
        {
            // Not currently used
        }

        public void GLMakeContextCurrent(WindowReg? reg)
        {
            SDL.SDLBool res;
            if (reg is Sdl3WindowReg sdlReg)
                res = SDL.SDL_GL_MakeCurrent(sdlReg.Sdl3Window, sdlReg.GlContext);
            else
                res = SDL.SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero);

            if (!res)
                _sawmill.Error("SDL_GL_MakeCurrent failed: {error}", SDL.SDL_GetError());
        }

        public void GLSwapInterval(WindowReg reg, int interval)
        {
            ((Sdl3WindowReg)reg).SwapInterval = interval;
            SDL.SDL_GL_SetSwapInterval(interval);
        }

        public unsafe void* GLGetProcAddress(string procName)
        {
            return (void*) SDL.SDL_GL_GetProcAddress(procName);
        }

        public string GetDescription()
        {
            var version = SDL.SDL_GetVersion();

            var major = SDL.SDL_VERSIONNUM_MAJOR(version);
            var minor = SDL.SDL_VERSIONNUM_MINOR(version);
            var micro = SDL.SDL_VERSIONNUM_MICRO(version);

            var videoDriver = SDL.SDL_GetCurrentVideoDriver();
            var revision = SDL.SDL_GetRevision();

            return $"SDL {major}.{minor}.{micro} (rev: {revision}, video driver: {videoDriver})";
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe void LogOutputFunction(
            void* userdata,
            int category,
            SDL.SDL_LogPriority priority,
            byte* message)
        {
            var obj = (Sdl3WindowingImpl) GCHandle.FromIntPtr((IntPtr)userdata).Target!;

            var level = priority switch
            {
                SDL.SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE => LogLevel.Verbose,
                SDL.SDL_LogPriority.SDL_LOG_PRIORITY_DEBUG => LogLevel.Debug,
                SDL.SDL_LogPriority.SDL_LOG_PRIORITY_INFO => LogLevel.Info,
                SDL.SDL_LogPriority.SDL_LOG_PRIORITY_WARN => LogLevel.Warning,
                SDL.SDL_LogPriority.SDL_LOG_PRIORITY_ERROR => LogLevel.Error,
                SDL.SDL_LogPriority.SDL_LOG_PRIORITY_CRITICAL => LogLevel.Fatal,
                _ => LogLevel.Error
            };

            var msg = Marshal.PtrToStringUTF8((IntPtr) message) ?? "";
            var categoryName = SdlLogCategoryName(category);
            obj._sawmillSdl3.Log(level, $"[{categoryName}] {msg}");
        }

        private static string SdlLogCategoryName(int category)
        {
            return (SDL.SDL_LogCategory) category switch {
                // @formatter:off
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION => "application",
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ERROR       => "error",
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ASSERT      => "assert",
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_SYSTEM      => "system",
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_AUDIO       => "audio",
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_VIDEO       => "video",
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_RENDER      => "render",
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_INPUT       => "input",
                SDL.SDL_LogCategory.SDL_LOG_CATEGORY_TEST        => "test",
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
