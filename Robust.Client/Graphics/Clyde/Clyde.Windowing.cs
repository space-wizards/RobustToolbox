using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Input;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using ErrorCode = OpenToolkit.GraphicsLibraryFramework.ErrorCode;
using FrameEventArgs = Robust.Shared.Timing.FrameEventArgs;
using Image = SixLabors.ImageSharp.Image;
using Vector2 = Robust.Shared.Maths.Vector2;
using GlfwImage = OpenToolkit.GraphicsLibraryFramework.Image;
using Monitor = OpenToolkit.GraphicsLibraryFramework.Monitor;

namespace Robust.Client.Graphics.Clyde
{
    internal unsafe partial class Clyde
    {
        // Keep delegates around to prevent GC issues.
        private GLFWCallbacks.ErrorCallback _errorCallback;
        private GLFWCallbacks.CharCallback _charCallback;
        private GLFWCallbacks.CursorPosCallback _cursorPosCallback;
        private GLFWCallbacks.KeyCallback _keyCallback;
        private GLFWCallbacks.MouseButtonCallback _mouseButtonCallback;
        private GLFWCallbacks.ScrollCallback _scrollCallback;
        private GLFWCallbacks.WindowCloseCallback _windowCloseCallback;
        private GLFWCallbacks.WindowSizeCallback _windowSizeCallback;
        private GLFWCallbacks.WindowContentScaleCallback _windowContentScaleCallback;

        private bool _glfwInitialized = false;

        private GraphicsContext _graphicsContext;
        private Window* _glfwWindow;

        private Vector2i _framebufferSize;
        private Vector2i _windowSize;
        private Vector2 _windowScale;
        private Vector2 _pixelRatio;
        private Thread _mainThread;

        private Vector2 _lastMousePos;

        // NOTE: in engine we pretend the framebuffer size is the screen size..
        // For practical reasons like UI rendering.
        public override Vector2i ScreenSize => _framebufferSize;

        public Vector2 MouseScreenPosition => _lastMousePos;

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

            InitWindow();
            return true;
        }

        private void InitWindow()
        {
            GLFW.WindowHint(WindowHintBool.SrgbCapable, true);
            GLFW.WindowHint(WindowHintInt.ContextVersionMajor, MinimumOpenGLVersion.Major);
            GLFW.WindowHint(WindowHintInt.ContextVersionMinor, MinimumOpenGLVersion.Minor);
            GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
#if DEBUG
            GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);
#endif
            GLFW.WindowHint(WindowHintString.X11ClassName, "SS14");
            GLFW.WindowHint(WindowHintString.X11InstanceName, "SS14");

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

            _glfwWindow = GLFW.CreateWindow(width, height, string.Empty, monitor, null);

            LoadWindowIcon();

            GLFW.SetCharCallback(_glfwWindow, _charCallback);
            GLFW.SetKeyCallback(_glfwWindow, _keyCallback);
            GLFW.SetWindowCloseCallback(_glfwWindow, _windowCloseCallback);
            GLFW.SetCursorPosCallback(_glfwWindow, _cursorPosCallback);
            GLFW.SetWindowSizeCallback(_glfwWindow, _windowSizeCallback);
            GLFW.SetScrollCallback(_glfwWindow, _scrollCallback);
            GLFW.SetMouseButtonCallback(_glfwWindow, _mouseButtonCallback);
            GLFW.SetWindowContentScaleCallback(_glfwWindow, _windowContentScaleCallback);

            GLFW.MakeContextCurrent(_glfwWindow);

            VSyncChanged();

            GLFW.GetFramebufferSize(_glfwWindow, out var fbW, out var fbH);
            _framebufferSize = (fbW, fbH);

            GLFW.GetWindowContentScale(_glfwWindow, out var scaleX, out var scaleY);
            _windowScale = (scaleX, scaleY);

            GLFW.GetWindowSize(_glfwWindow, out var w, out var h);
            _windowSize = (w, h);

            _pixelRatio = _framebufferSize / _windowSize;

            InitGLContext();

            // Initializing OTK 3 seems to mess with the current context, so ensure it's still set.
            // This took me fucking *forever* to debug because this manifested differently on nvidia drivers vs intel mesa.
            // So I thought it was a calling convention issue with the calli OpenTK emits.
            // Because, in my tests, I had InitGLContext() AFTER the test with a delegate-based invoke of the proc.
            GLFW.MakeContextCurrent(_glfwWindow);

            InitOpenGL();
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
                    var image = Image.Load(stream);
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

        private void InitGLContext()
        {
            // Initialize the OpenTK 3 GL context with GLFW.
            _graphicsContext = new GraphicsContext(new ContextHandle((IntPtr) _glfwWindow), GLFW.GetProcAddress,
                () => new ContextHandle((IntPtr) GLFW.GetCurrentContext()));
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
            _gameController.TextEntered(new TextEventArgs(codepoint));
        }

        private void OnGlfwCursorPos(Window* window, double x, double y)
        {
            var newPos = ((float) x, (float) y) * _pixelRatio;
            var delta = newPos - _lastMousePos;
            _lastMousePos = newPos;

            var ev = new MouseMoveEventArgs(delta, newPos);
            _gameController.MouseMove(ev);
        }

        private void OnGlfwKey(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
        {
            EmitKeyEvent(Keyboard.ConvertGlfwKey(key), action, mods);
        }

        private void OnGlfwMouseButton(Window* window, MouseButton button, InputAction action, KeyModifiers mods)
        {
            EmitKeyEvent(Mouse.MouseButtonToKey(Mouse.ConvertGlfwButton(button)), action, mods);
        }

        private void EmitKeyEvent(Keyboard.Key key, InputAction action, KeyModifiers mods)
        {
            var shift = mods.HasFlag(KeyModifiers.Shift);
            var alt = mods.HasFlag(KeyModifiers.Alt);
            var control = mods.HasFlag(KeyModifiers.Control);
            var system = mods.HasFlag(KeyModifiers.Super);

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
            var ev = new MouseWheelEventArgs(((float) offsetX, (float) offsetY), _lastMousePos);
            _gameController.MouseWheel(ev);
        }

        private void OnGlfwWindowClose(Window* window)
        {
            _gameController.Shutdown("Window closed");
        }

        private void OnGlfwWindowSize(Window* window, int width, int height)
        {
            var oldSize = _framebufferSize;
            GLFW.GetFramebufferSize(window, out var fbW, out var fbH);
            _framebufferSize = (fbW, fbH);
            _windowSize = (width, height);

            if(fbW == 0 || fbH == 0 || width == 0 || height == 0)
                return;

            _pixelRatio = _framebufferSize / _windowSize;

            GL.Viewport(0, 0, fbW, fbH);
            if (fbW != 0 && fbH != 0)
            {
                _regenerateLightRenderTarget();
            }

            OnWindowResized?.Invoke(new WindowResizedEventArgs(oldSize, _framebufferSize));
        }

        private void OnGlfwWindownContentScale(Window *window, float xScale, float yScale)
        {
            _windowScale = (xScale, yScale);
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
        }

        public override void SetWindowTitle(string title)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            GLFW.SetWindowTitle(_glfwWindow, title);
        }

        public void ProcessInput(FrameEventArgs frameEventArgs)
        {
            GLFW.PollEvents();
        }

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
                var monitor = GLFW.GetPrimaryMonitor();
                var mode = GLFW.GetVideoMode(monitor);
                GLFW.SetWindowMonitor(_glfwWindow, GLFW.GetPrimaryMonitor(), 0, 0, mode->Width, mode->Height,
                    mode->RefreshRate);
            }
            else
            {
                GLFW.SetWindowMonitor(_glfwWindow, null, 0, 0, 1280, 720, 0);
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
    }
}
