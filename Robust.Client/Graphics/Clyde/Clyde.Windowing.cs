using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Robust.Client.Utility.LiterallyJustMessageBox;
using FrameEventArgs = Robust.Shared.Timing.FrameEventArgs;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly List<WindowHandle> _windowHandles = new();
        private readonly List<MonitorHandle> _monitorHandles = new();

        private IWindowingImpl? _windowing;
        private Renderer _chosenRenderer;

        private Thread? _windowingThread;
        private bool _vSync;
        private WindowMode _windowMode;

        public event Action<TextEventArgs>? TextEntered;
        public event Action<MouseMoveEventArgs>? MouseMove;
        public event Action<KeyEventArgs>? KeyUp;
        public event Action<KeyEventArgs>? KeyDown;
        public event Action<MouseWheelEventArgs>? MouseWheel;
        public event Action<WindowClosedEventArgs>? CloseWindow;
        public event Action<WindowDestroyedEventArgs>? DestroyWindow;
        public event Action? OnWindowScaleChanged;
        public event Action<WindowResizedEventArgs>? OnWindowResized;
        public event Action<WindowFocusedEventArgs>? OnWindowFocused;

        // NOTE: in engine we pretend the framebuffer size is the screen size..
        // For practical reasons like UI rendering.
        public IClydeWindow MainWindow => _windowing?.MainWindow?.Handle ??
                                          throw new InvalidOperationException("Windowing is not initialized");

        public Vector2i ScreenSize => _windowing?.MainWindow?.FramebufferSize ??
                                      throw new InvalidOperationException("Windowing is not initialized");

        public bool IsFocused => _windowing?.MainWindow?.IsFocused ??
                                 throw new InvalidOperationException("Windowing is not initialized");

        public IEnumerable<IClydeWindow> AllWindows => _windowHandles;

        public Vector2 DefaultWindowScale => _windowing?.MainWindow?.WindowScale ??
                                             throw new InvalidOperationException("Windowing is not initialized");

        public Vector2 MouseScreenPosition => _windowing?.MainWindow?.LastMousePos ??
                                              throw new InvalidOperationException("Windowing is not initialized");

        public string GetKeyName(Keyboard.Key key)
        {
            DebugTools.AssertNotNull(_windowing);

            return _windowing!.KeyGetName(key);
        }

        public uint? GetX11WindowId()
        {
            return _windowing?.WindowGetX11Id(_windowing.MainWindow!) ?? null;
        }

        private bool InitWindowing()
        {
            _windowingThread = Thread.CurrentThread;

            _windowing = new GlfwWindowingImpl(this);

            return _windowing.Init();
        }

        private unsafe bool InitMainWindowAndRenderer()
        {
            DebugTools.AssertNotNull(_windowing);

            _chosenRenderer = (Renderer) _cfg.GetCVar(CVars.DisplayRenderer);

            var renderers = _chosenRenderer == Renderer.Default
                ? stackalloc Renderer[]
                {
                    Renderer.OpenGL33,
                    Renderer.OpenGL31,
                    Renderer.OpenGLES2
                }
                : stackalloc Renderer[] {_chosenRenderer};

            var succeeded = false;
            string? lastError = null;
            foreach (var renderer in renderers)
            {
                if (!_windowing!.TryInitMainWindow(renderer, out lastError))
                {
                    Logger.DebugS("clyde.win", $"{renderer} unsupported: {lastError}");
                    continue;
                }

                // We should have a main window now.
                DebugTools.AssertNotNull(_windowing.MainWindow);

                succeeded = true;
                _chosenRenderer = renderer;
                _isGLES = _chosenRenderer == Renderer.OpenGLES2;
                _isCore = _chosenRenderer == Renderer.OpenGL33;
                break;
            }

            if (!succeeded)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var msgBoxContent = "Failed to create the game window. " +
                                        "This probably means your GPU is too old to play the game. " +
                                        "Try to update your graphics drivers, " +
                                        "or enable compatibility mode in the launcher if that fails.\n" +
                                        $"The exact error is: {lastError}";

                    MessageBoxW(null,
                        msgBoxContent,
                        "Space Station 14: Failed to create window",
                        MB_OK | MB_ICONERROR);
                }

                Logger.FatalS("clyde.win",
                    "Failed to create main game window! " +
                    "This probably means your GPU is too old to run the game. " +
                    $"That or update your graphics drivers. {lastError}");

                return false;
            }

            _windowing!.GLInitMainContext(_isGLES);

            UpdateMainWindowLoadedRtSize();

            _windowing.GLMakeContextCurrent(_windowing.MainWindow!);
            InitOpenGL();
            return true;
        }

        private IEnumerable<Image<Rgba32>> LoadWindowIcons()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Does nothing on macOS so don't bother.
                yield break;
            }

            foreach (var file in _resourceCache.ContentFindFiles("/Textures/Logo/icon"))
            {
                if (file.Extension != "png")
                {
                    continue;
                }

                using var stream = _resourceCache.ContentFileRead(file);
                yield return Image.Load<Rgba32>(stream);
            }
        }

        private void ShutdownWindowing()
        {
            _windowing?.Shutdown();
        }

        public void SetWindowTitle(string title)
        {
            DebugTools.AssertNotNull(_windowing);

            _windowing!.WindowSetTitle(_windowing.MainWindow!, title);
        }

        public void SetWindowMonitor(IClydeMonitor monitor)
        {
            DebugTools.AssertNotNull(_windowing);

            var window = _windowing!.MainWindow!;

            _windowing.WindowSetMonitor(window, monitor);
        }

        public void RequestWindowAttention()
        {
            DebugTools.AssertNotNull(_windowing);

            _windowing!.WindowRequestAttention(_windowing.MainWindow!);
        }

        public async Task<IClydeWindow> CreateWindow()
        {
            DebugTools.AssertNotNull(_windowing);

            return await _windowing!.WindowCreate();
        }

        private void DoDestroyWindow(WindowReg reg)
        {
            if (reg.IsMainWindow)
                throw new InvalidOperationException("Cannot destroy main window.");

            _windowing!.WindowDestroy(reg);
        }

        public void ProcessInput(FrameEventArgs frameEventArgs)
        {
            _windowing?.ProcessEvents();
            DispatchEvents();
        }

        private void SwapMainBuffers()
        {
            _windowing?.WindowSwapBuffers(_windowing.MainWindow!);
        }

        private void VSyncChanged(bool newValue)
        {
            _vSync = newValue;
            _windowing?.UpdateVSync();
        }

        private void CreateWindowRenderTexture(WindowReg reg)
        {
            reg.RenderTexture = CreateRenderTarget(reg.FramebufferSize, new RenderTargetFormatParameters
            {
                ColorFormat = RenderTargetColorFormat.Rgba8Srgb,
                HasDepthStencil = true
            });
        }

        private void WindowModeChanged(int mode)
        {
            _windowMode = (WindowMode) mode;
            _windowing?.UpdateMainWindowMode();
        }

        Task<string> IClipboardManager.GetText()
        {
            return _windowing?.ClipboardGetText() ?? Task.FromResult("");
        }

        void IClipboardManager.SetText(string text)
        {
            _windowing?.ClipboardSetText(text);
        }

        public IEnumerable<IClydeMonitor> EnumerateMonitors()
        {
            return _monitorHandles;
        }

        public ICursor GetStandardCursor(StandardCursorShape shape)
        {
            DebugTools.AssertNotNull(_windowing);

            return _windowing!.CursorGetStandard(shape);
        }

        public ICursor CreateCursor(Image<Rgba32> image, Vector2i hotSpot)
        {
            DebugTools.AssertNotNull(_windowing);

            return _windowing!.CursorCreate(image, hotSpot);
        }

        public void SetCursor(ICursor? cursor)
        {
            DebugTools.AssertNotNull(_windowing);

            _windowing!.CursorSet(_windowing.MainWindow!, cursor);
        }


        private void SetWindowVisible(WindowReg reg, bool visible)
        {
            DebugTools.AssertNotNull(_windowing);

            _windowing!.WindowSetVisible(reg, visible);
        }

        private abstract class WindowReg
        {
            public bool IsDisposed;

            public Vector2 WindowScale;
            public Vector2 PixelRatio;
            public Vector2i FramebufferSize;
            public Vector2i WindowSize;
            public Vector2i PrevWindowSize;
            public Vector2i WindowPos;
            public Vector2i PrevWindowPos;
            public Vector2 LastMousePos;
            public bool IsFocused;
            public bool IsMinimized;
            public string Title = "";
            public bool IsVisible;
            public bool DisposeOnClose;

            public bool IsMainWindow;
            public WindowHandle Handle = default!;
            public RenderTexture? RenderTexture;
            public GLHandle QuadVao;
            public Action<WindowClosedEventArgs>? Closed;
        }

        private sealed class WindowHandle : IClydeWindow
        {
            // So funny story
            // When this class was a record, the C# compiler on .NET 5 stack overflowed
            // while compiling the Closed event.
            // VERY funny.

            private readonly Clyde _clyde;
            private readonly WindowReg _reg;

            public bool IsDisposed => _reg.IsDisposed;

            public WindowHandle(Clyde clyde, WindowReg reg)
            {
                _clyde = clyde;
                _reg = reg;
            }

            public void Dispose()
            {
                _clyde.DoDestroyWindow(_reg);
            }

            public Vector2i Size => _reg.FramebufferSize;

            public IRenderTarget RenderTarget
            {
                get
                {
                    if (_reg.IsMainWindow)
                    {
                        return _clyde._mainMainWindowRenderMainTarget;
                    }

                    return _reg.RenderTexture!;
                }
            }

            public string Title
            {
                get => _reg.Title;
                set => _clyde._windowing!.WindowSetTitle(_reg, value);
            }

            public bool IsFocused => _reg.IsFocused;
            public bool IsMinimized => _reg.IsMinimized;

            public bool IsVisible
            {
                get => _reg.IsVisible;
                set => _clyde.SetWindowVisible(_reg, value);
            }

            public bool DisposeOnClose
            {
                get => _reg.DisposeOnClose;
                set => _reg.DisposeOnClose = value;
            }

            public event Action<WindowClosedEventArgs> Closed
            {
                add => _reg.Closed += value;
                remove => _reg.Closed -= value;
            }
        }

        private sealed class MonitorHandle : IClydeMonitor
        {
            public MonitorHandle(int id, string name, Vector2i size, int refreshRate)
            {
                Id = id;
                Name = name;
                Size = size;
                RefreshRate = refreshRate;
            }

            public int Id { get; }
            public string Name { get; }
            public Vector2i Size { get; }
            public int RefreshRate { get; }
        }

        private abstract class MonitorReg
        {
            public MonitorHandle Handle = default!;
        }
    }
}
