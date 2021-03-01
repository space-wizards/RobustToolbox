using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using OpenToolkit;
using OpenToolkit.Graphics.OpenGL4;
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
        private bool _glfwInitialized;

        // Keep delegates around to prevent GC issues.
        private GLFWCallbacks.ErrorCallback _errorCallback = default!;
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

        private readonly List<WindowReg> _windows = new();
        private readonly List<WindowHandle> _windowHandles = new();

        private Renderer _chosenRenderer;
        private IBindingsContext _graphicsContext = default!;
        private WindowReg? _mainWindow;

        private Thread? _mainThread;

        // NOTE: in engine we pretend the framebuffer size is the screen size..
        // For practical reasons like UI rendering.
        public IClydeWindow MainWindow => _mainWindow!.Handle;
        public override Vector2i ScreenSize => _mainWindow!.FramebufferSize;
        public override bool IsFocused => _mainWindow!.IsFocused;
        public IEnumerable<IClydeWindow> AllWindows => _windowHandles;
        public Vector2 DefaultWindowScale => _mainWindow!.WindowScale;
        public Vector2 MouseScreenPosition => _mainWindow!.LastMousePos;

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
                return GLFW.GetX11Window(_mainWindow!.GlfwWindow);
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
        }

        private List<Exception>? _glfwExceptionList;

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

            InitCursors();

            return InitMainWindow();
        }

        private bool InitMainWindow()
        {
            var width = _configurationManager.GetCVar(CVars.DisplayWidth);
            var height = _configurationManager.GetCVar(CVars.DisplayHeight);

            Monitor* monitor = null;

            if (WindowMode == WindowMode.Fullscreen)
            {
                monitor = GLFW.GetPrimaryMonitor();
                var mode = GLFW.GetVideoMode(monitor);
                width = mode->Width;
                height = mode->Height;
            }

#if DEBUG
            GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);
#endif
            GLFW.WindowHint(WindowHintString.X11ClassName, "SS14");
            GLFW.WindowHint(WindowHintString.X11InstanceName, "SS14");

            _chosenRenderer = (Renderer) _configurationManager.GetCVar(CVars.DisplayRenderer);

            var renderers = (_chosenRenderer == Renderer.Default)
                ? stackalloc Renderer[]
                {
                    Renderer.OpenGL33,
                    Renderer.OpenGL31,
                    Renderer.OpenGLES2
                }
                : stackalloc Renderer[] {_chosenRenderer};

            Window* window = null;

            foreach (var r in renderers)
            {
                window = CreateGlfwWindowForRenderer(r, width, height, ref monitor, null);

                if (window != null)
                {
                    _chosenRenderer = r;
                    _isGLES = _chosenRenderer == Renderer.OpenGLES2;
                    _isCore = _chosenRenderer == Renderer.OpenGL33;
                    break;
                }

                // Window failed to init due to error.
                // Try not to treat the error code seriously.
                var code = GLFW.GetError(out string desc);
                Logger.DebugS("clyde.win", $"{r} unsupported: [${code}] ${desc}");
            }

            if (window == null)
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

            _mainWindow = SetupWindow(window);
            _mainWindow.IsMainWindow = true;

            UpdateMainWindowLoadedRtSize();

            GLFW.MakeContextCurrent(window);
            VSyncChanged();
            InitGLContext();

            // Initializing OTK 3 seems to mess with the current context, so ensure it's still set.
            // This took me fucking *forever* to debug because this manifested differently on nvidia drivers vs intel mesa.
            // So I thought it was a calling convention issue with the calli OpenTK emits.
            // Because, in my tests, I had InitGLContext() AFTER the test with a delegate-based invoke of the proc.
            GLFW.MakeContextCurrent(window);

            InitOpenGL();

            return true;
        }

        private static Window* CreateGlfwWindowForRenderer(
            Renderer r,
            int width, int height,
            ref Monitor* monitor,
            Window* contextShare)
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

            return GLFW.CreateWindow(width, height, string.Empty, monitor, contextShare);
        }

        private WindowReg SetupWindow(Window* window)
        {
            var reg = new WindowReg
            {
                GlfwWindow = window
            };
            var handle = new WindowHandle(this, reg);
            reg.Handle = handle;

            LoadWindowIcon(window);

            GLFW.SetCharCallback(window, _charCallback);
            GLFW.SetKeyCallback(window, _keyCallback);
            GLFW.SetWindowCloseCallback(window, _windowCloseCallback);
            GLFW.SetCursorPosCallback(window, _cursorPosCallback);
            GLFW.SetWindowSizeCallback(window, _windowSizeCallback);
            GLFW.SetScrollCallback(window, _scrollCallback);
            GLFW.SetMouseButtonCallback(window, _mouseButtonCallback);
            GLFW.SetWindowContentScaleCallback(window, _windowContentScaleCallback);
            GLFW.SetWindowIconifyCallback(window, _windowIconifyCallback);
            GLFW.SetWindowFocusCallback(window, _windowFocusCallback);

            GLFW.GetFramebufferSize(window, out var fbW, out var fbH);
            reg.FramebufferSize = (fbW, fbH);

            GLFW.GetWindowContentScale(window, out var scaleX, out var scaleY);
            reg.WindowScale = (scaleX, scaleY);

            GLFW.GetWindowSize(window, out var w, out var h);
            reg.PrevWindowSize = reg.WindowSize = (w, h);

            GLFW.GetWindowPos(window, out var x, out var y);
            reg.PrevWindowPos = (x, y);

            reg.PixelRatio = reg.FramebufferSize / reg.WindowSize;

            _windows.Add(reg);
            _windowHandles.Add(handle);

            return reg;
        }

        private WindowHandle CreateWindowImpl()
        {
            DebugTools.AssertNotNull(_mainWindow);

            // GLFW.WindowHint(WindowHintBool.SrgbCapable, false);

            Monitor* monitor = null;
            var window = CreateGlfwWindowForRenderer(_chosenRenderer, 1280, 720, ref monitor, _mainWindow!.GlfwWindow);
            if (window == null)
            {
                var errCode = GLFW.GetError(out var desc);
                throw new GlfwException($"{errCode}: {desc}");
            }

            var reg = SetupWindow(window);
            CreateWindowRenderTexture(reg);

            GLFW.MakeContextCurrent(window);

            reg.QuadVao = MakeQuadVao();

            UniformConstantsUBO.Rebind();
            ProjViewUBO.Rebind();

            GLFW.MakeContextCurrent(_mainWindow.GlfwWindow);

            return reg.Handle;
        }

        private void CreateWindowRenderTexture(WindowReg reg)
        {
            reg.RenderTexture = CreateRenderTarget(reg.FramebufferSize, new RenderTargetFormatParameters
            {
                ColorFormat = RenderTargetColorFormat.Rgba8Srgb,
                HasDepthStencil = true
            });
        }

        private void LoadWindowIcon(Window* window)
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

            SetWindowIcon(window, icons);
        }

        private void SetWindowIcon(Window* window, IEnumerable<Image<Rgba32>> icons)
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

            GLFW.SetWindowIcon(window, glfwImages);

            foreach (var handle in handles)
            {
                handle.Free();
            }
        }

        private class GlfwBindingsContext : IBindingsContext
        {
            public IntPtr GetProcAddress(string procName)
            {
                return GLFW.GetProcAddress(procName);
            }
        }

        private void InitGLContext()
        {
            _graphicsContext = new GlfwBindingsContext();
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

        private WindowReg FindWindow(Window* window)
        {
            foreach (var windowReg in _windows)
            {
                if (windowReg.GlfwWindow == window)
                {
                    return windowReg;
                }
            }

            throw new KeyNotFoundException();
        }

        private static void OnGlfwError(ErrorCode code, string description)
        {
            Logger.ErrorS("clyde.win.glfw", "GLFW Error: [{0}] {1}", code, description);
        }

        private void OnGlfwChar(Window* window, uint codepoint)
        {
            try
            {
                _gameController.TextEntered(new TextEventArgs(codepoint));
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
                var windowReg = FindWindow(window);
                var newPos = ((float) x, (float) y) * windowReg.PixelRatio;
                var delta = newPos - windowReg.LastMousePos;
                windowReg.LastMousePos = newPos;

                var ev = new MouseMoveEventArgs(delta, newPos);
                _gameController.MouseMove(ev);
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
                    _gameController.KeyUp(ev);
                    break;
                case InputAction.Press:
                case InputAction.Repeat:
                    _gameController.KeyDown(ev);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private void OnGlfwScroll(Window* window, double offsetX, double offsetY)
        {
            try
            {
                var windowReg = FindWindow(window);
                var ev = new MouseWheelEventArgs(((float) offsetX, (float) offsetY), windowReg.LastMousePos);
                _gameController.MouseWheel(ev);
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
                _gameController.Shutdown("Window closed");
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
                var windowReg = FindWindow(window);
                var oldSize = windowReg.FramebufferSize;
                GLFW.GetFramebufferSize(window, out var fbW, out var fbH);
                windowReg.FramebufferSize = (fbW, fbH);
                windowReg.WindowSize = (width, height);

                if (windowReg.IsMainWindow)
                {
                    UpdateMainWindowLoadedRtSize();
                }

                if (fbW == 0 || fbH == 0 || width == 0 || height == 0)
                    return;

                windowReg.PixelRatio = windowReg.FramebufferSize / windowReg.WindowSize;

                if (windowReg.IsMainWindow)
                {
                    GL.Viewport(0, 0, fbW, fbH);
                    CheckGlError();
                    if (fbW != 0 && fbH != 0)
                    {
                        _mainViewport.Dispose();
                        CreateMainViewport();
                    }
                }
                else
                {
                    windowReg.RenderTexture!.Dispose();

                    CreateWindowRenderTexture(windowReg);
                }

                var eventArgs = new WindowResizedEventArgs(oldSize, windowReg.FramebufferSize, windowReg.Handle);
                OnWindowResized?.Invoke(eventArgs);
            }
            catch (Exception e)
            {
                CatchCallbackException(e);
            }
        }

        private void OnGlfwWindowContentScale(Window* window, float xScale, float yScale)
        {
            try
            {
                var windowReg = FindWindow(window);
                windowReg.WindowScale = (xScale, yScale);
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
                var windowReg = FindWindow(window);
                windowReg.IsMinimized = iconified;
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
                var windowReg = FindWindow(window);
                windowReg.IsFocused = focused;
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
            _charCallback = OnGlfwChar;
            _cursorPosCallback = OnGlfwCursorPos;
            _keyCallback = OnGlfwKey;
            _mouseButtonCallback = OnGlfwMouseButton;
            _scrollCallback = OnGlfwScroll;
            _windowCloseCallback = OnGlfwWindowClose;
            _windowSizeCallback = OnGlfwWindowSize;
            _windowContentScaleCallback = OnGlfwWindowContentScale;
            _windowIconifyCallback = OnGlfwWindowIconify;
            _windowFocusCallback = OnGlfwWindowFocus;
        }

        public override void SetWindowTitle(string title)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            GLFW.SetWindowTitle(_mainWindow!.GlfwWindow, title);
        }

        public void RequestWindowAttention()
        {
            GLFW.RequestWindowAttention(_mainWindow!.GlfwWindow);
        }

        public IClydeWindow CreateWindow()
        {
            return CreateWindowImpl();
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
            foreach (var window in _windows)
            {
                if (!window.IsMainWindow)
                {
                    GLFW.SwapBuffers(window.GlfwWindow);
                }
            }

            // Do main window last since it has vsync.
            GLFW.SwapBuffers(_mainWindow!.GlfwWindow);
        }

        protected override void VSyncChanged()
        {
            if (!_glfwInitialized)
            {
                return;
            }

            GLFW.SwapInterval(VSync ? 1 : 0);
        }

        protected override void WindowModeChanged()
        {
            if (_mainWindow == null)
            {
                return;
            }

            if (WindowMode == WindowMode.Fullscreen)
            {
                GLFW.GetWindowSize(_mainWindow.GlfwWindow, out var w, out var h);
                _mainWindow.PrevWindowSize = (w, h);

                GLFW.GetWindowPos(_mainWindow.GlfwWindow, out var x, out var y);
                _mainWindow.PrevWindowPos = (x, y);
                var monitor = GLFW.GetPrimaryMonitor();
                var mode = GLFW.GetVideoMode(monitor);

                GLFW.SetWindowMonitor(
                    _mainWindow.GlfwWindow,
                    GLFW.GetPrimaryMonitor(),
                    0, 0,
                    mode->Width, mode->Height,
                    mode->RefreshRate);
            }
            else
            {
                GLFW.SetWindowMonitor(
                    _mainWindow.GlfwWindow,
                    null,
                    _mainWindow.PrevWindowPos.X, _mainWindow.PrevWindowPos.Y,
                    _mainWindow.PrevWindowSize.X, _mainWindow.PrevWindowSize.Y, 0);
            }
        }

        string IClipboardManager.GetText()
        {
            return GLFW.GetClipboardString(_mainWindow!.GlfwWindow);
        }

        void IClipboardManager.SetText(string text)
        {
            GLFW.SetClipboardString(_mainWindow!.GlfwWindow, text);
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

        private sealed class WindowReg
        {
            public bool IsMainWindow;
            public Window* GlfwWindow;

            public Vector2 WindowScale;
            public Vector2 PixelRatio;
            public Vector2i FramebufferSize;
            public Vector2i WindowSize;
            public Vector2i PrevWindowSize;
            public Vector2i PrevWindowPos;
            public Vector2 LastMousePos;
            public bool IsFocused;
            public bool IsMinimized;

            public WindowHandle Handle = default!;
            public RenderTexture? RenderTexture;
            public GLHandle QuadVao;
        }

        private sealed record WindowHandle(Clyde Clyde, WindowReg Reg) : IClydeWindow
        {
            public void Dispose()
            {
            }

            public Vector2i Size => Reg.FramebufferSize;
            public IRenderTarget RenderTarget
            {
                get
                {
                    if (Reg.IsMainWindow)
                    {
                        return Clyde._mainMainWindowRenderMainTarget;
                    }

                    return Reg.RenderTexture!;
                }
            }
        }

        [Serializable]
        public class GlfwException : Exception
        {
            public GlfwException()
            {
            }

            public GlfwException(string message) : base(message)
            {
            }

            public GlfwException(string message, Exception inner) : base(message, inner)
            {
            }

            protected GlfwException(
                SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
