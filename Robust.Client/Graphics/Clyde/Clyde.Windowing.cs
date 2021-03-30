using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using OpenToolkit;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Robust.Client.Utility.LiterallyJustMessageBox;
using ErrorCode = OpenToolkit.GraphicsLibraryFramework.ErrorCode;
using FrameEventArgs = Robust.Shared.Timing.FrameEventArgs;
using GLFW = OpenToolkit.GraphicsLibraryFramework.GLFW;
using GLFWCallbacks = OpenToolkit.GraphicsLibraryFramework.GLFWCallbacks;
using Image = SixLabors.ImageSharp.Image;
using Vector2 = Robust.Shared.Maths.Vector2;
using GlfwImage = OpenToolkit.GraphicsLibraryFramework.Image;
using InputAction = OpenToolkit.GraphicsLibraryFramework.InputAction;
using KeyModifiers = OpenToolkit.GraphicsLibraryFramework.KeyModifiers;
using Keys = OpenToolkit.GraphicsLibraryFramework.Keys;
using Monitor = OpenToolkit.GraphicsLibraryFramework.Monitor;
using MouseButton = OpenToolkit.GraphicsLibraryFramework.MouseButton;
using OpenGlProfile = OpenToolkit.GraphicsLibraryFramework.OpenGlProfile;
using ClientApi = OpenToolkit.GraphicsLibraryFramework.ClientApi;
using ContextApi = OpenToolkit.GraphicsLibraryFramework.ContextApi;
using Window = OpenToolkit.GraphicsLibraryFramework.Window;
using WindowHintBool = OpenToolkit.GraphicsLibraryFramework.WindowHintBool;
using WindowHintInt = OpenToolkit.GraphicsLibraryFramework.WindowHintInt;
using WindowHintOpenGlProfile = OpenToolkit.GraphicsLibraryFramework.WindowHintOpenGlProfile;
using WindowHintClientApi = OpenToolkit.GraphicsLibraryFramework.WindowHintClientApi;
using WindowHintContextApi = OpenToolkit.GraphicsLibraryFramework.WindowHintContextApi;
using WindowHintString = OpenToolkit.GraphicsLibraryFramework.WindowHintString;

namespace Robust.Client.Graphics.Clyde
{
    internal unsafe partial class Clyde
    {
        // Keep delegates around to prevent GC issues.
        private GLFWCallbacks.ErrorCallback _errorCallback = default!;
        private GLFWCallbacks.MonitorCallback _monitorCallback = default!;
        private GLFWCallbacks.CharCallback _charCallback = default!;
        private GLFWCallbacks.CursorPosCallback _cursorPosCallback = default!;
        private GLFWCallbacks.KeyCallback _keyCallback = default!;
        private GLFWCallbacks.MouseButtonCallback _mouseButtonCallback = default!;
        private GLFWCallbacks.ScrollCallback _scrollCallback = default!;
        private GLFWCallbacks.WindowCloseCallback _windowCloseCallback = default!;
        private GLFWCallbacks.WindowSizeCallback _windowSizeCallback = default!;
        private GLFWCallbacks.WindowContentScaleCallback _windowContentScaleCallback = default!;
        private GLFWCallbacks.WindowIconifyCallback _windowIconifyCallback = default!;
        private GLFWCallbacks.WindowFocusCallback _windowFocusCallback = default!;

        private bool _glfwInitialized;

        private IBindingsContext _graphicsContext = default!;
        private Window* _glfwWindow;

        private Vector2i _framebufferSize;
        private bool _isFocused;
        private Vector2i _windowSize;
        private Vector2i _prevWindowSize;
        private Vector2i _prevWindowPos;
        private Vector2 _windowScale;
        private Vector2 _pixelRatio;
        private Thread? _mainThread;

        private Vector2 _lastMousePos;

        // Can't use ClydeHandle because it's 64 bit.
        private int _nextWindowId = 1;
        private readonly Dictionary<int, MonitorReg> _monitors = new();

        public event Action<TextEventArgs>? TextEntered;
        public event Action<MouseMoveEventArgs>? MouseMove;
        public event Action<KeyEventArgs>? KeyUp;
        public event Action<KeyEventArgs>? KeyDown;
        public event Action<MouseWheelEventArgs>? MouseWheel;
        public event Action<string>? CloseWindow;
        public event Action? OnWindowScaleChanged;

        // NOTE: in engine we pretend the framebuffer size is the screen size..
        // For practical reasons like UI rendering.
        public override Vector2i ScreenSize => _framebufferSize;
        public override bool IsFocused => _isFocused;
        public Vector2 DefaultWindowScale => _windowScale;
        public Vector2 MouseScreenPosition => _lastMousePos;

        public string GetKeyName(Keyboard.Key key)
        {
            var name = Keyboard.GetSpecialKeyName(key);
            if (name != null)
            {
                return Loc.GetString(name);
            }

            name = GLFW.GetKeyName(Keyboard.ConvertGlfwKeyReverse(key), 0);
            if (name != null)
            {
                return name.ToUpper();
            }

            return Loc.GetString("<unknown key>");
        }

        public string GetKeyNameScanCode(int scanCode)
        {
            return GLFW.GetKeyName(Keys.Unknown, scanCode);
        }

        public int GetKeyScanCode(Keyboard.Key key)
        {
            return GLFW.GetKeyScancode(Keyboard.ConvertGlfwKeyReverse(key));
        }

        public uint? GetX11WindowId()
        {
            try
            {
                return GLFW.GetX11Window(_glfwWindow);
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
        }

        private List<Exception>? _glfwExceptionList;
        private bool _isMinimized;

        private bool InitGlfw()
        {
            StoreCallbacks();

            GLFW.SetErrorCallback(_errorCallback);
            if (!GLFW.Init())
            {
                Logger.FatalS("clyde.win", "Failed to initialize GLFW!");
                return false;
            }

            _glfwInitialized = true;
            var version = GLFW.GetVersionString();
            Logger.DebugS("clyde.win", "GLFW initialized, version: {0}.", version);

            return true;
        }

        private bool InitWindowing()
        {
            _mainThread = Thread.CurrentThread;
            if (!InitGlfw())
            {
                return false;
            }

            InitMonitors();
            InitCursors();

            return InitWindow();
        }

        private void InitMonitors()
        {
            var monitors = GLFW.GetMonitorsRaw(out var count);

            for (var i = 0; i < count; i++)
            {
                SetupMonitor(monitors[i]);
            }
        }

        private void SetupMonitor(Monitor* monitor)
        {
            var handle = _nextWindowId++;

            DebugTools.Assert(GLFW.GetMonitorUserPointer(monitor) == null, "GLFW window already has user pointer??");

            var name = GLFW.GetMonitorName(monitor);
            var videoMode = GLFW.GetVideoMode(monitor);
            var impl = new ClydeMonitorImpl(handle, name, (videoMode->Width, videoMode->Height), videoMode->RefreshRate);

            GLFW.SetMonitorUserPointer(monitor, (void*) handle);
            _monitors[handle] = new MonitorReg
            {
                Id = handle,
                Impl = impl,
                Monitor = monitor
            };
        }

        private void DestroyMonitor(Monitor* monitor)
        {
            var ptr = GLFW.GetMonitorUserPointer(monitor);

            if (ptr == null)
            {
                var name = GLFW.GetMonitorName(monitor);
                Logger.WarningS("clyde.win", $"Monitor '{name}' had no user pointer set??");
                return;
            }

            _monitors.Remove((int) ptr);
            GLFW.SetMonitorUserPointer(monitor, null);
        }

        private bool InitWindow()
        {
            var width = ConfigurationManager.GetCVar(CVars.DisplayWidth);
            var height = ConfigurationManager.GetCVar(CVars.DisplayHeight);

            Monitor* monitor = null;

            if (WindowMode == WindowMode.Fullscreen)
            {
                monitor = GLFW.GetMonitors()[1];
                var mode = GLFW.GetVideoMode(monitor);
                width = mode->Width;
                height = mode->Height;

                GLFW.WindowHint(WindowHintInt.RefreshRate, mode->RefreshRate);
            }

#if DEBUG
            GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);
#endif
            GLFW.WindowHint(WindowHintString.X11ClassName, "SS14");
            GLFW.WindowHint(WindowHintString.X11InstanceName, "SS14");

            var renderer = (Renderer) ConfigurationManager.GetCVar<int>(CVars.DisplayRenderer);

            Span<Renderer> renderers = (renderer == Renderer.Default)
                ? stackalloc Renderer[]
                {
                    Renderer.OpenGL33,
                    Renderer.OpenGL31,
                    Renderer.OpenGLES2
                }
                : stackalloc Renderer[] {renderer};

            foreach (Renderer r in renderers)
            {
                CreateWindowForRenderer(r);

                if (_glfwWindow != null)
                {
                    renderer = r;
                    _isGLES = renderer == Renderer.OpenGLES2;
                    _isCore = renderer == Renderer.OpenGL33;
                    break;
                }

                // Window failed to init due to error.
                // Try not to treat the error code seriously.
                var code = GLFW.GetError(out string desc);
                Logger.DebugS("clyde.win", $"{r} unsupported: [${code}] ${desc}");
            }

            if (_glfwWindow == null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var code = GLFW.GetError(out string desc);

                    var errorContent = "Failed to create the game window. " +
                                       "This probably means your GPU is too old to play the game. " +
                                       "That or update your graphic drivers\n" +
                                       $"The exact error is: [{code}]\n {desc}";

                    MessageBoxW(null,
                        errorContent,
                        "Space Station 14: Failed to create window",
                        MB_OK | MB_ICONERROR);
                }

                Logger.FatalS("clyde.win",
                    "Failed to create GLFW window! " +
                    "This probably means your GPU is too old to run the game. " +
                    "That or update your graphics drivers.");
                return false;
            }

            LoadWindowIcon();

            GLFW.SetCharCallback(_glfwWindow, _charCallback);
            GLFW.SetKeyCallback(_glfwWindow, _keyCallback);
            GLFW.SetWindowCloseCallback(_glfwWindow, _windowCloseCallback);
            GLFW.SetCursorPosCallback(_glfwWindow, _cursorPosCallback);
            GLFW.SetWindowSizeCallback(_glfwWindow, _windowSizeCallback);
            GLFW.SetScrollCallback(_glfwWindow, _scrollCallback);
            GLFW.SetMouseButtonCallback(_glfwWindow, _mouseButtonCallback);
            GLFW.SetWindowContentScaleCallback(_glfwWindow, _windowContentScaleCallback);
            GLFW.SetWindowIconifyCallback(_glfwWindow, _windowIconifyCallback);
            GLFW.SetWindowFocusCallback(_glfwWindow, _windowFocusCallback);

            GLFW.MakeContextCurrent(_glfwWindow);

            VSyncChanged();

            GLFW.GetFramebufferSize(_glfwWindow, out var fbW, out var fbH);
            _framebufferSize = (fbW, fbH);
            UpdateWindowLoadedRtSize();

            GLFW.GetWindowContentScale(_glfwWindow, out var scaleX, out var scaleY);
            _windowScale = (scaleX, scaleY);

            GLFW.GetWindowSize(_glfwWindow, out var w, out var h);
            _prevWindowSize = _windowSize = (w, h);

            GLFW.GetWindowPos(_glfwWindow, out var x, out var y);
            _prevWindowPos = (x, y);

            _pixelRatio = _framebufferSize / _windowSize;

            InitGLContext();

            // Initializing OTK 3 seems to mess with the current context, so ensure it's still set.
            // This took me fucking *forever* to debug because this manifested differently on nvidia drivers vs intel mesa.
            // So I thought it was a calling convention issue with the calli OpenTK emits.
            // Because, in my tests, I had InitGLContext() AFTER the test with a delegate-based invoke of the proc.
            GLFW.MakeContextCurrent(_glfwWindow);

            InitOpenGL();

            return true;

            void CreateWindowForRenderer(Renderer r)
            {
                if (r == Renderer.OpenGL33)
                {
                    GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);
                    GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);
                    GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
                    GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
                    GLFW.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.NativeContextApi);
                    GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
                    GLFW.WindowHint(WindowHintBool.SrgbCapable, true);
                }
                else if (r == Renderer.OpenGL31)
                {
                    GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);
                    GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 1);
                    GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, false);
                    GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
                    GLFW.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.NativeContextApi);
                    GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Any);
                    GLFW.WindowHint(WindowHintBool.SrgbCapable, true);
                }
                else if (r == Renderer.OpenGLES2)
                {
                    GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 2);
                    GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 0);
                    GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
                    GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlEsApi);
                    // GLES2 is initialized through EGL to allow ANGLE usage.
                    // (It may be an idea to make this a configuration cvar)
                    GLFW.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.EglContextApi);
                    GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Any);
                    GLFW.WindowHint(WindowHintBool.SrgbCapable, false);
                }

                _glfwWindow = GLFW.CreateWindow(width, height, string.Empty, monitor, null);
            }
        }

        private void LoadWindowIcon()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Does nothing on macOS so don't bother.
                return;
            }

            var icons = new List<Image<Rgba32>>();
            foreach (var file in _resourceCache.ContentFindFiles("/Textures/Logo/icon"))
            {
                if (file.Extension != "png")
                {
                    continue;
                }

                using (var stream = _resourceCache.ContentFileRead(file))
                {
                    var image = Image.Load<Rgba32>(stream);
                    icons.Add(image);
                }
            }

            SetWindowIcon(icons);
        }

        private void SetWindowIcon(IEnumerable<Image<Rgba32>> icons)
        {
            // Turn each image into a byte[] so we can actually pin their contents.
            // Wish I knew a clean way to do this without allocations.
            var images = icons
                .Select(i => (MemoryMarshal.Cast<Rgba32, byte>(i.GetPixelSpan()).ToArray(), i.Width, i.Height))
                .ToList();

            // ReSharper disable once SuggestVarOrType_Elsewhere
            Span<GCHandle> handles = stackalloc GCHandle[images.Count];
            Span<GlfwImage> glfwImages = new GlfwImage[images.Count];

            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];
                handles[i] = GCHandle.Alloc(image.Item1, GCHandleType.Pinned);
                var addrOfPinnedObject = (byte*) handles[i].AddrOfPinnedObject();
                glfwImages[i] = new GlfwImage(image.Width, image.Height, addrOfPinnedObject);
            }

            GLFW.SetWindowIcon(_glfwWindow, glfwImages);

            foreach (var handle in handles)
            {
                handle.Free();
            }
        }

        private class GLFWBindingsContext : IBindingsContext
        {
            public IntPtr GetProcAddress(string procName)
            {
                return GLFW.GetProcAddress(procName);
            }
        }

        private void InitGLContext()
        {
            _graphicsContext = new GLFWBindingsContext();
            GL.LoadBindings(_graphicsContext);

            if (_isGLES)
            {
                // On GLES we use some OES and KHR functions so make sure to initialize them.
                OpenToolkit.Graphics.ES20.GL.LoadBindings(_graphicsContext);
            }
        }

        private void ShutdownWindowing()
        {
            if (_glfwInitialized)
            {
                Logger.DebugS("clyde.win", "Terminating GLFW.");
                GLFW.Terminate();
            }
        }

        private static void OnGlfwError(ErrorCode code, string description)
        {
            Logger.ErrorS("clyde.win.glfw", "GLFW Error: [{0}] {1}", code, description);
        }

        private void OnGlfwMonitor(Monitor* monitor, ConnectedState state)
        {
            try
            {
                if (state == ConnectedState.Connected)
                {
                    SetupMonitor(monitor);
                }
                else
                {
                    DestroyMonitor(monitor);
                }
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwChar(Window* window, uint codepoint)
        {
            try
            {
                TextEntered?.Invoke(new TextEventArgs(codepoint));
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwCursorPos(Window* window, double x, double y)
        {
            try
            {
                var newPos = ((float) x, (float) y) * _pixelRatio;
                var delta = newPos - _lastMousePos;
                _lastMousePos = newPos;

                MouseMove?.Invoke(new MouseMoveEventArgs(delta, newPos));
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwKey(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
        {
            try
            {
                EmitKeyEvent(Keyboard.ConvertGlfwKey(key), action, mods);
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwMouseButton(Window* window, MouseButton button, InputAction action, KeyModifiers mods)
        {
            try
            {
                EmitKeyEvent(Mouse.MouseButtonToKey(Mouse.ConvertGlfwButton(button)), action, mods);
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void EmitKeyEvent(Keyboard.Key key, InputAction action, KeyModifiers mods)
        {
            var shift = (mods & KeyModifiers.Shift) != 0;
            var alt = (mods & KeyModifiers.Alt) != 0;
            var control = (mods & KeyModifiers.Control) != 0;
            var system = (mods & KeyModifiers.Super) != 0;

            var ev = new KeyEventArgs(
                key,
                action == InputAction.Repeat,
                alt, control, shift, system);

            switch (action)
            {
                case InputAction.Release:
                    KeyUp?.Invoke(ev);
                    break;
                case InputAction.Press:
                case InputAction.Repeat:
                    KeyDown?.Invoke(ev);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private void OnGlfwScroll(Window* window, double offsetX, double offsetY)
        {
            try
            {
                var ev = new MouseWheelEventArgs(((float) offsetX, (float) offsetY), _lastMousePos);
                MouseWheel?.Invoke(ev);
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwWindowClose(Window* window)
        {
            try
            {
                CloseWindow?.Invoke("Window closed");
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwWindowSize(Window* window, int width, int height)
        {
            try
            {
                var oldSize = _framebufferSize;
                GLFW.GetFramebufferSize(window, out var fbW, out var fbH);
                _framebufferSize = (fbW, fbH);
                _windowSize = (width, height);
                UpdateWindowLoadedRtSize();

                if (fbW == 0 || fbH == 0 || width == 0 || height == 0)
                    return;

                _pixelRatio = _framebufferSize / _windowSize;

                GL.Viewport(0, 0, fbW, fbH);
                CheckGlError();
                if (fbW != 0 && fbH != 0)
                {
                    _mainViewport.Dispose();
                    CreateMainViewport();
                }

                OnWindowResized?.Invoke(new WindowResizedEventArgs(oldSize, _framebufferSize));
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwWindownContentScale(Window* window, float xScale, float yScale)
        {
            try
            {
                _windowScale = (xScale, yScale);
                OnWindowScaleChanged?.Invoke();
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwWindowIconify(Window* window, bool iconified)
        {
            try
            {
                _isMinimized = iconified;
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwWindowFocus(Window* window, bool focused)
        {
            try
            {
                _isFocused = focused;
                OnWindowFocused?.Invoke(new WindowFocusedEventArgs(focused));
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void StoreCallbacks()
        {
            _errorCallback = OnGlfwError;
            _monitorCallback = OnGlfwMonitor;
            _charCallback = OnGlfwChar;
            _cursorPosCallback = OnGlfwCursorPos;
            _keyCallback = OnGlfwKey;
            _mouseButtonCallback = OnGlfwMouseButton;
            _scrollCallback = OnGlfwScroll;
            _windowCloseCallback = OnGlfwWindowClose;
            _windowSizeCallback = OnGlfwWindowSize;
            _windowContentScaleCallback = OnGlfwWindownContentScale;
            _windowIconifyCallback = OnGlfwWindowIconify;
            _windowFocusCallback = OnGlfwWindowFocus;
        }

        public override void SetWindowTitle(string title)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            GLFW.SetWindowTitle(_glfwWindow, title);
        }

        public void SetWindowMonitor(IClydeMonitor monitor)
        {
            var monitorImpl = (ClydeMonitorImpl) monitor;
            var reg = _monitors[monitorImpl.Id];

            GLFW.SetWindowMonitor(
                _glfwWindow,
                reg.Monitor,
                0, 0,
                monitorImpl.Size.X, monitorImpl.Size.Y,
                monitorImpl.RefreshRate);
        }

        public void RequestWindowAttention()
        {
            GLFW.RequestWindowAttention(_glfwWindow);
        }

        public void ProcessInput(FrameEventArgs frameEventArgs)
        {
            GLFW.PollEvents();

            if (_glfwExceptionList == null || _glfwExceptionList.Count == 0)
            {
                return;
            }

            // Exception handling.
            // See CatchCallbackException for details.

            if (_glfwExceptionList.Count == 1)
            {
                var exception = _glfwExceptionList[0];
                _glfwExceptionList = null;

                // Rethrow without losing stack trace.
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw exception; // Unreachable.
            }

            var list = _glfwExceptionList;
            _glfwExceptionList = null;
            throw new AggregateException("Exceptions have been caught inside GLFW callbacks.", list);
        }

        // Disabling inlining so that I can easily exclude it from profiles.
        // Doesn't matter anyways, it's a few extra cycles per frame.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SwapBuffers()
        {
            GLFW.SwapBuffers(_glfwWindow);
        }

        protected override void VSyncChanged()
        {
            if (_glfwWindow == null)
            {
                return;
            }

            GLFW.SwapInterval(VSync ? 1 : 0);
        }

        protected override void WindowModeChanged()
        {
            if (_glfwWindow == null)
            {
                return;
            }

            if (WindowMode == WindowMode.Fullscreen)
            {
                GLFW.GetWindowSize(_glfwWindow, out var w, out var h);
                _prevWindowSize = (w, h);

                GLFW.GetWindowPos(_glfwWindow, out var x, out var y);
                _prevWindowPos = (x, y);
                var monitor = MonitorForWindow(_glfwWindow);
                var mode = GLFW.GetVideoMode(monitor);

                GLFW.SetWindowMonitor(_glfwWindow, monitor, 0, 0, mode->Width, mode->Height,
                    mode->RefreshRate);
            }
            else
            {
                GLFW.SetWindowMonitor(_glfwWindow, null, _prevWindowPos.X, _prevWindowPos.Y, _prevWindowSize.X,
                    _prevWindowSize.Y, 0);
            }
        }

        // glfwGetWindowMonitor only works for fullscreen windows.
        // Picks the monitor with the top-left corner of the window.
        private Monitor* MonitorForWindow(Window* window)
        {
            GLFW.GetWindowPos(window, out var winPosX, out var winPosY);
            var monitors = GLFW.GetMonitorsRaw(out var count);
            for (var i = 0; i < count; i++)
            {
                var monitor = monitors[i];
                GLFW.GetMonitorPos(monitor, out var monPosX, out var monPosY);
                var videoMode = GLFW.GetVideoMode(monitor);

                var box = Box2i.FromDimensions(monPosX, monPosY, videoMode->Width, videoMode->Height);
                if (box.Contains(winPosX, winPosY))
                    return monitor;
            }

            // Fallback
            return GLFW.GetPrimaryMonitor();
        }

        string IClipboardManager.GetText()
        {
            return GLFW.GetClipboardString(_glfwWindow);
        }

        void IClipboardManager.SetText(string text)
        {
            GLFW.SetClipboardString(_glfwWindow, text);
        }

        public IEnumerable<IClydeMonitor> EnumerateMonitors()
        {
            return _monitors.Values.Select(c => c.Impl);
        }

        // We can't let exceptions unwind into GLFW, as that can cause the CLR to crash.
        // And it probably messes up GLFW too.
        // So all the callbacks are passed to this method.
        // So they can be queued for re-throw at the end of ProcessInputs().
        private void CatchCallbackException(Exception e)
        {
            if (_glfwExceptionList == null)
            {
                _glfwExceptionList = new List<Exception>();
            }

            _glfwExceptionList.Add(e);
        }

        private sealed class MonitorReg
        {
            public int Id;
            public Monitor* Monitor;
            public ClydeMonitorImpl Impl = default!;
        }

        private sealed class ClydeMonitorImpl : IClydeMonitor
        {
            public ClydeMonitorImpl(int id, string name, Vector2i size, int refreshRate)
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
    }
}
