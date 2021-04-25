using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using OpenToolkit;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Input;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;
using GlfwImage = OpenToolkit.GraphicsLibraryFramework.Image;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed unsafe partial class GlfwWindowingImpl
        {
            private readonly List<GlfwWindowReg> _windows = new();

            public IReadOnlyList<WindowReg> AllWindows => _windows;
            public IBindingsContext GraphicsBindingContext => _mainGraphicsContext;

            public WindowReg? MainWindow => _mainWindow;
            private GlfwWindowReg? _mainWindow;
            private GlfwBindingsContext _mainGraphicsContext = default!;

            public bool TryInitMainWindow(Renderer renderer, [NotNullWhen(false)] out string? error)
            {
                var width = _cfg.GetCVar(CVars.DisplayWidth);
                var height = _cfg.GetCVar(CVars.DisplayHeight);

                Monitor* monitor = null;

                if (_clyde.WindowMode == WindowMode.Fullscreen)
                {
                    monitor = GLFW.GetPrimaryMonitor();
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

                var window = CreateGlfwWindowForRenderer(renderer, width, height, ref monitor, null);

                if (window == null)
                {
                    var code = GLFW.GetError(out var desc);
                    error = $"[{code}] {desc}";

                    return false;
                }

                _mainWindow = SetupWindow(window);
                _mainWindow.IsMainWindow = true;

                GLFW.MakeContextCurrent(window);
                UpdateVSync();

                error = null;
                return true;
            }

            public void WindowSetTitle(WindowReg window, string title)
            {
                CheckWindowDisposed(window);

                if (title == null)
                {
                    throw new ArgumentNullException(nameof(title));
                }

                var reg = (GlfwWindowReg) window;

                GLFW.SetWindowTitle(reg.GlfwWindow, title);
                reg.Title = title;
            }

            public void WindowSetMonitor(WindowReg window, IClydeMonitor monitor)
            {
                CheckWindowDisposed(window);

                var monitorImpl = (MonitorHandle) monitor;
                var reg = _monitors[monitorImpl.Id];

                GLFW.SetWindowMonitor(
                    _mainWindow!.GlfwWindow,
                    reg.Monitor,
                    0, 0, monitorImpl.Size.X, monitorImpl.Size.Y,
                    monitorImpl.RefreshRate);
            }

            public void WindowSetVisible(WindowReg window, bool visible)
            {
                var reg = (GlfwWindowReg) window;
                reg.IsVisible = visible;

                if (visible)
                {
                    GLFW.ShowWindow(reg.GlfwWindow);
                }
                else
                {
                    GLFW.HideWindow(reg.GlfwWindow);
                }
            }

            public void WindowRequestAttention(WindowReg window)
            {
                CheckWindowDisposed(window);

                var reg = (GlfwWindowReg) window;

                GLFW.RequestWindowAttention(reg.GlfwWindow);
            }

            public void WindowSwapBuffers(WindowReg window)
            {
                CheckWindowDisposed(window);

                var reg = (GlfwWindowReg) window;

                GLFW.SwapBuffers(reg.GlfwWindow);
            }

            public void UpdateVSync()
            {
                if (_mainWindow == null)
                    return;

                GLFW.MakeContextCurrent(_mainWindow!.GlfwWindow);
                GLFW.SwapInterval(_clyde.VSync ? 1 : 0);
            }

            public void UpdateMainWindowMode()
            {
                if (_mainWindow == null)
                {
                    return;
                }

                if (_clyde.WindowMode == WindowMode.Fullscreen)
                {
                    GLFW.GetWindowSize(_mainWindow.GlfwWindow, out var w, out var h);
                    _mainWindow.PrevWindowSize = (w, h);

                    GLFW.GetWindowPos(_mainWindow.GlfwWindow, out var x, out var y);
                    _mainWindow.PrevWindowPos = (x, y);
                    var monitor = MonitorForWindow(_mainWindow.GlfwWindow);
                    var mode = GLFW.GetVideoMode(monitor);

                    GLFW.SetWindowMonitor(
                        _mainWindow.GlfwWindow,
                        monitor,
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

            public uint? WindowGetX11Id(WindowReg window)
            {
                CheckWindowDisposed(window);

                var reg = (GlfwWindowReg) window;
                try
                {
                    return GLFW.GetX11Window(reg.GlfwWindow);
                }
                catch (EntryPointNotFoundException)
                {
                    return null;
                }
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

            private GlfwWindowReg SetupWindow(Window* window)
            {
                var reg = new GlfwWindowReg
                {
                    GlfwWindow = window
                };
                var handle = new WindowHandle(_clyde, reg);
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
                _clyde._windowHandles.Add(handle);

                return reg;
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

            public WindowHandle WindowCreate()
            {
                DebugTools.AssertNotNull(_mainWindow);

                // GLFW.WindowHint(WindowHintBool.SrgbCapable, false);

                Monitor* monitor = null;
                var window = CreateGlfwWindowForRenderer(
                    _clyde._chosenRenderer,
                    1280, 720,
                    ref monitor,
                    _mainWindow!.GlfwWindow);

                if (window == null)
                {
                    var errCode = GLFW.GetError(out var desc);
                    throw new GlfwException($"{errCode}: {desc}");
                }

                var reg = SetupWindow(window);
                CreateWindowRenderTexture(reg);

                GLFW.MakeContextCurrent(window);

                // VSync always off for non-primary windows.
                GLFW.SwapInterval(0);

                reg.QuadVao = _clyde.MakeQuadVao();

                _clyde.UniformConstantsUBO.Rebind();
                _clyde.ProjViewUBO.Rebind();

                GLFW.MakeContextCurrent(_mainWindow.GlfwWindow);

                return reg.Handle;
            }

            public string KeyGetName(Keyboard.Key key)
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

            public int KeyGetScanCode(Keyboard.Key key)
            {
                return GLFW.GetKeyScancode(Keyboard.ConvertGlfwKeyReverse(key));
            }

            public string KeyGetNameScanCode(int scanCode)
            {
                return GLFW.GetKeyName(Keys.Unknown, scanCode);
            }

            public string ClipboardGetText()
            {
                return GLFW.GetClipboardString(_mainWindow!.GlfwWindow);
            }

            public void ClipboardSetText(string text)
            {
                GLFW.SetClipboardString(_mainWindow!.GlfwWindow, text);
            }

            private void CreateWindowRenderTexture(WindowReg reg)
            {
                reg.RenderTexture = _clyde.CreateRenderTarget(reg.FramebufferSize, new RenderTargetFormatParameters
                {
                    ColorFormat = RenderTargetColorFormat.Rgba8Srgb,
                    HasDepthStencil = true
                });
            }

            public void LoadWindowIcon(Window* window)
            {
                var icons = _clyde.LoadWindowIcons().ToArray();

                // Turn each image into a byte[] so we can actually pin their contents.
                // Wish I knew a clean way to do this without allocations.
                var images = icons
                    .Select(i => (MemoryMarshal.Cast<Rgba32, byte>(i.GetPixelSpan()).ToArray(), i.Width, i.Height))
                    .ToList();

                // ReSharper disable once SuggestVarOrType_Elsewhere
                Span<GCHandle> handles = stackalloc GCHandle[images.Count];
                Span<GlfwImage> glfwImages = stackalloc GlfwImage[images.Count];

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

            public void GLInitMainContext(bool gles)
            {
                _mainGraphicsContext = new GlfwBindingsContext();
                GL.LoadBindings(_mainGraphicsContext);

                if (gles)
                {
                    // On GLES we use some OES and KHR functions so make sure to initialize them.
                    OpenToolkit.Graphics.ES20.GL.LoadBindings(_mainGraphicsContext);
                }
            }

            public void GLMakeContextCurrent(WindowReg window)
            {
                CheckWindowDisposed(window);

                var reg = (GlfwWindowReg) window;

                GLFW.MakeContextCurrent(reg.GlfwWindow);
            }

            private void CheckWindowDisposed(WindowReg reg)
            {
                if (reg.Disposed)
                    throw new ObjectDisposedException("Window disposed");
            }

            private sealed class GlfwWindowReg : WindowReg
            {
                public Window* GlfwWindow;
            }

            private class GlfwBindingsContext : IBindingsContext
            {
                public IntPtr GetProcAddress(string procName)
                {
                    return GLFW.GetProcAddress(procName);
                }
            }
        }
    }
}
