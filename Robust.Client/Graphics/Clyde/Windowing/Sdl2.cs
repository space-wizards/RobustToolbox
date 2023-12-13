using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using static SDL2.SDL;
using static SDL2.SDL.SDL_LogCategory;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl : IWindowingImpl
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly Clyde _clyde;
        private GCHandle _selfGCHandle;

        private readonly ISawmill _sawmill;
        private readonly ISawmill _sawmillSdl2;

        public Sdl2WindowingImpl(Clyde clyde, IDependencyCollection deps)
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

        private unsafe bool InitSdl2()
        {
            _selfGCHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            SDL_LogSetAllPriority(SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE);
            SDL_LogSetOutputFunction(&LogOutputFunction, (void*) GCHandle.ToIntPtr(_selfGCHandle));

            SDL_SetHint("SDL_WINDOWS_DPI_SCALING", "1");
            SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL_SetHint(SDL_HINT_IME_SUPPORT_EXTENDED_TEXT, "1");
            SDL_SetHint(SDL_HINT_IME_SHOW_UI, "1");

            var res = SDL_Init(SDL_INIT_VIDEO | SDL_INIT_EVENTS);
            if (res < 0)
            {
                _sawmill.Fatal("Failed to initialize SDL2: {error}", SDL_GetError());
                return false;
            }

            SDL_GetVersion(out var version);
            var videoDriver = SDL_GetCurrentVideoDriver();
            _sawmill.Debug(
                "SDL2 initialized, version: {major}.{minor}.{patch}, video driver: {videoDriver}", version.major, version.minor, version.patch, videoDriver);

            _sdlEventWakeup = SDL_RegisterEvents(1);

            SDL_EventState(SDL_EventType.SDL_SYSWMEVENT, SDL_ENABLE);

            InitCursors();
            InitMonitors();
            ReloadKeyMap();

            SDL_AddEventWatch(&EventWatch, (void*) GCHandle.ToIntPtr(_selfGCHandle));

            // SDL defaults to having text input enabled, so we have to manually turn it off in init for consistency.
            // If we don't, text input will remain enabled *until* the user first leaves a LineEdit/TextEdit.
            SDL_StopTextInput();

            return true;
        }

        public unsafe void Shutdown()
        {
            if (_selfGCHandle != default)
            {
                SDL_DelEventWatch(&EventWatch, (void*) GCHandle.ToIntPtr(_selfGCHandle));
                _selfGCHandle.Free();
            }

            SDL_LogSetOutputFunction(null, null);

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

        public string GetDescription()
        {
            SDL_GetVersion(out var version);
            _sawmill.Debug(
                "SDL2 initialized, version: {major}.{minor}.{patch}", version.major, version.minor, version.patch);

            var videoDriver = SDL_GetCurrentVideoDriver();

            return $"SDL2 {version.major}.{version.minor}.{version.patch} ({videoDriver})";
        }

        [UnmanagedCallersOnly(CallConvs = new []{typeof(CallConvCdecl)})]
        private static unsafe void LogOutputFunction(
            void* userdata,
            int category,
            SDL_LogPriority priority,
            byte* message)
        {
            var obj = (Sdl2WindowingImpl) GCHandle.FromIntPtr((IntPtr)userdata).Target!;

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
            if (msg == "That operation is not supported")
            {
                obj._sawmillSdl2.Info(Environment.StackTrace);
            }
            
            var categoryName = SdlLogCategoryName(category);
            obj._sawmillSdl2.Log(level, $"[{categoryName}] {msg}");
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
    }
}
