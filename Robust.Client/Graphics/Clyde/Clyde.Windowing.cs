using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using OpenToolkit;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Input;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Utility;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
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
using Window = OpenToolkit.GraphicsLibraryFramework.Window;
using WindowHintBool = OpenToolkit.GraphicsLibraryFramework.WindowHintBool;
using WindowHintInt = OpenToolkit.GraphicsLibraryFramework.WindowHintInt;
using WindowHintOpenGlProfile = OpenToolkit.GraphicsLibraryFramework.WindowHintOpenGlProfile;
using WindowHintString = OpenToolkit.GraphicsLibraryFramework.WindowHintString;

namespace Robust.Client.Graphics.Clyde
{
    internal unsafe partial class Clyde
    {
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

        private bool _glfwInitialized;

        private IBindingsContext _graphicsContext = default!;
        private Window* _glfwWindow;

        private Vector2i _framebufferSize;
        private Vector2i _windowSize;
        private Vector2i _prevWindowSize;
        private Vector2i _prevWindowPos;
        private Vector2 _windowScale;
        private Vector2 _pixelRatio;
        private Thread? _mainThread;

        private Vector2 _lastMousePos;

        // NOTE: in engine we pretend the framebuffer size is the screen size..
        // For practical reasons like UI rendering.
        public override Vector2i ScreenSize => _framebufferSize;

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

            InitCursors();

            return InitWindow();
        }

        private bool InitWindow()
        {
            var width = _configurationManager.GetCVar<int>("display.width");
            var height = _configurationManager.GetCVar<int>("display.height");

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
            GLFW.WindowHint(WindowHintBool.SrgbCapable, true);

            var renderer = (Renderer) _configurationManager.GetCVar<int>("display.renderer");

            if (renderer == Renderer.Default)
            {
                // Try OpenGL Core 3.3
                GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);
                GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);
                GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
                GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

                _glfwWindow = GLFW.CreateWindow(width, height, string.Empty, monitor, null);

                if (_glfwWindow == null)
                {
                    // Window failed to init due to error.
                    var err = GLFW.GetErrorRaw(out _);

                    if (err == ErrorCode.VersionUnavailable)
                    {
                        Logger.DebugS("clyde.win", "OpenGL Core 3.3 unsupported, trying OpenGL 3.1");

                        CreateWindowGl31();
                    }
                }
            }
            else if (renderer == Renderer.OpenGL31)
            {
                CreateWindowGl31();
            }

            // TODO: If Windows use MessageBoxW here since it's easy enough.
            // I think, at least.
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
                        "Failed to create window",
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

            void CreateWindowGl31()
            {
                GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);
                GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 1);
                GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, false);
                GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Any);

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
                var newPos = ((float) x, (float) y) * _pixelRatio;
                var delta = newPos - _lastMousePos;
                _lastMousePos = newPos;

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
                var ev = new MouseWheelEventArgs(((float) offsetX, (float) offsetY), _lastMousePos);
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
                var oldSize = _framebufferSize;
                GLFW.GetFramebufferSize(window, out var fbW, out var fbH);
                _framebufferSize = (fbW, fbH);
                _windowSize = (width, height);
                UpdateWindowLoadedRtSize();

                if (fbW == 0 || fbH == 0 || width == 0 || height == 0)
                    return;

                _pixelRatio = _framebufferSize / _windowSize;

                GL.Viewport(0, 0, fbW, fbH);
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
            _windowContentScaleCallback = OnGlfwWindownContentScale;
            _windowIconifyCallback = OnGlfwWindowIconify;
        }

        public override void SetWindowTitle(string title)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            GLFW.SetWindowTitle(_glfwWindow, title);
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
                var monitor = GLFW.GetPrimaryMonitor();
                var mode = GLFW.GetVideoMode(monitor);

                GLFW.SetWindowMonitor(_glfwWindow, GLFW.GetPrimaryMonitor(), 0, 0, mode->Width, mode->Height,
                    mode->RefreshRate);
            }
            else
            {
                GLFW.SetWindowMonitor(_glfwWindow, null, _prevWindowPos.X, _prevWindowPos.Y, _prevWindowSize.X, _prevWindowSize.Y, 0);
            }
        }

        string IClipboardManager.GetText()
        {
            return GLFW.GetClipboardString(_glfwWindow);
        }

        void IClipboardManager.SetText(string text)
        {
            GLFW.SetClipboardString(_glfwWindow, text);
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
    }
}
