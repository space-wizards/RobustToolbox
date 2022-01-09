using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Client.Utility;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Windows;
using GlfwImage = OpenToolkit.GraphicsLibraryFramework.Image;
using Monitor = OpenToolkit.GraphicsLibraryFramework.Monitor;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed unsafe partial class GlfwWindowingImpl
        {
            private int _nextWindowId = 1;
            private static bool _eglLoaded;

            public void WindowSetTitle(WindowReg window, string title)
            {
                CheckWindowDisposed(window);

                if (title == null)
                {
                    throw new ArgumentNullException(nameof(title));
                }

                var reg = (GlfwWindowReg) window;

                SendCmd(new CmdWinSetTitle((nint) reg.GlfwWindow, title));
            }

            private void WinThreadWinSetTitle(CmdWinSetTitle cmd)
            {
                GLFW.SetWindowTitle((Window*) cmd.Window, cmd.Title);
            }

            public void WindowSetMonitor(WindowReg window, IClydeMonitor monitor)
            {
                CheckWindowDisposed(window);

                var winReg = (GlfwWindowReg) window;

                var monitorImpl = (MonitorHandle) monitor;

                SendCmd(new CmdWinSetMonitor(
                    (nint) winReg.GlfwWindow,
                    monitorImpl.Id,
                    0, 0,
                    monitorImpl.Size.X, monitorImpl.Size.Y,
                    monitorImpl.RefreshRate));
            }

            private void WinThreadWinSetMonitor(CmdWinSetMonitor cmd)
            {
                Monitor* monitorPtr;
                if (cmd.MonitorId == 0)
                {
                    monitorPtr = null;
                }
                else if (_winThreadMonitors.TryGetValue(cmd.MonitorId, out var monitorReg))
                {
                    monitorPtr = monitorReg.Ptr;
                }
                else
                {
                    return;
                }

                GLFW.SetWindowMonitor(
                    (Window*) cmd.Window,
                    monitorPtr,
                    cmd.X, cmd.Y,
                    cmd.W, cmd.H,
                    cmd.RefreshRate
                );
            }

            public void WindowSetVisible(WindowReg window, bool visible)
            {
                var reg = (GlfwWindowReg) window;
                reg.IsVisible = visible;

                SendCmd(new CmdWinSetVisible((nint) reg.GlfwWindow, visible));
            }

            private void WinThreadWinSetVisible(CmdWinSetVisible cmd)
            {
                var win = (Window*) cmd.Window;

                if (cmd.Visible)
                {
                    GLFW.ShowWindow(win);
                }
                else
                {
                    GLFW.HideWindow(win);
                }
            }

            public void WindowRequestAttention(WindowReg window)
            {
                CheckWindowDisposed(window);

                var reg = (GlfwWindowReg) window;

                SendCmd(new CmdWinRequestAttention((nint) reg.GlfwWindow));
            }

            private void WinThreadWinRequestAttention(CmdWinRequestAttention cmd)
            {
                var win = (Window*) cmd.Window;

                GLFW.RequestWindowAttention(win);
            }

            public void WindowSwapBuffers(WindowReg window)
            {
                CheckWindowDisposed(window);

                var reg = (GlfwWindowReg) window;

                GLFW.SwapBuffers(reg.GlfwWindow);
            }

            public void UpdateMainWindowMode()
            {
                if (_clyde._mainWindow == null)
                    return;

                var win = (GlfwWindowReg) _clyde._mainWindow;
                if (_clyde._windowMode == WindowMode.Fullscreen)
                {
                    win.PrevWindowSize = win.WindowSize;
                    win.PrevWindowPos = win.WindowPos;

                    SendCmd(new CmdWinSetFullscreen((nint) win.GlfwWindow));
                }
                else
                {
                    SendCmd(new CmdWinSetMonitor(
                        (nint) win.GlfwWindow,
                        0,
                        win.PrevWindowPos.X, win.PrevWindowPos.Y,
                        win.PrevWindowSize.X, win.PrevWindowSize.Y,
                        0
                    ));
                }
            }

            private void WinThreadWinSetFullscreen(CmdWinSetFullscreen cmd)
            {
                var ptr = (Window*) cmd.Window;
                //GLFW.GetWindowSize(ptr, out var w, out var h);
                //GLFW.GetWindowPos(ptr, out var x, out var y);

                var monitor = MonitorForWindow(ptr);
                var mode = GLFW.GetVideoMode(monitor);

                GLFW.SetWindowMonitor(
                    ptr,
                    monitor,
                    0, 0,
                    mode->Width, mode->Height,
                    mode->RefreshRate);
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

            public nint? WindowGetX11Display(WindowReg window)
            {
                CheckWindowDisposed(window);

                var reg = (GlfwWindowReg) window;
                try
                {
                    return GLFW.GetX11Display(reg.GlfwWindow);
                }
                catch (EntryPointNotFoundException)
                {
                    return null;
                }
            }

            public nint? WindowGetWin32Window(WindowReg window)
            {
                if (!OperatingSystem.IsWindows())
                    return null;

                var reg = (GlfwWindowReg) window;
                try
                {
                    return GLFW.GetWin32Window(reg.GlfwWindow);
                }
                catch (EntryPointNotFoundException)
                {
                    return null;
                }
            }

            public (WindowReg?, string? error) WindowCreate(
                GLContextSpec? spec,
                WindowCreateParameters parameters,
                WindowReg? share,
                WindowReg? owner)
            {
                Window* sharePtr = null;
                if (share is GlfwWindowReg glfwReg)
                    sharePtr = glfwReg.GlfwWindow;

                Window* ownerPtr = null;
                if (owner is GlfwWindowReg glfwOwnerReg)
                    ownerPtr = glfwOwnerReg.GlfwWindow;

                var task = SharedWindowCreate(
                    spec,
                    parameters,
                    sharePtr,
                    ownerPtr);

                // Block the main thread (to avoid stuff like texture uploads being problematic).
                WaitWindowCreate(task);

                var (reg, errorResult) = task.Result;

                if (reg != null)
                {
                    reg.Owner = reg.Handle;
                    return (reg, null);
                }

                var (desc, errCode) = errorResult!.Value;
                return (null, (string)$"[{errCode}]: {desc}");
            }

            public void WindowDestroy(WindowReg window)
            {
                var reg = (GlfwWindowReg) window;
                SendCmd(new CmdWinDestroy((nint) reg.GlfwWindow, window.Owner != null));
            }

            private void WaitWindowCreate(Task<GlfwWindowCreateResult> windowTask)
            {
                while (!windowTask.IsCompleted)
                {
                    // Keep processing events until the window task gives either an error or success.
                    WaitEvents();
                    ProcessEvents(single: true);
                }
            }

            private Task<GlfwWindowCreateResult> SharedWindowCreate(
                GLContextSpec? glSpec,
                WindowCreateParameters parameters,
                Window* share, Window* owner)
            {
                //
                // IF YOU'RE WONDERING WHY THIS IS TASK-BASED:
                // I originally wanted this to be async so we could avoid blocking the main thread
                // while the OS takes its stupid 100~ms just to initialize a fucking GL context.
                // This doesn't *work* because
                // we have to release the GL context while the shared context is being created.
                // (at least on WGL, I didn't test other platforms and I don't care to.)
                // Not worth it to avoid a main thread blockage by allowing Clyde to temporarily release the GL context,
                // because rendering would be locked up *anyways*.
                //
                // Basically what I'm saying is that everything about OpenGL is a fucking mistake
                // and I should get on either Veldrid or Vulkan some time.
                // Probably Veldrid tbh.
                //

                // Yes we ping-pong this TCS through the window thread and back, deal with it.
                var tcs = new TaskCompletionSource<GlfwWindowCreateResult>();
                SendCmd(new CmdWinCreate(
                    glSpec,
                    parameters,
                    (nint) share,
                    (nint) owner,
                    tcs));

                return tcs.Task;
            }

            private static void FinishWindowCreate(EventWindowCreate ev)
            {
                var (res, tcs) = ev;

                tcs.TrySetResult(res);
            }

            private void WinThreadWinCreate(CmdWinCreate cmd)
            {
                var (glSpec, parameters, share, owner, tcs) = cmd;

                var window = CreateGlfwWindowForRenderer(glSpec, parameters, (Window*) share, (Window*) owner);

                if (window == null)
                {
                    var err = GLFW.GetError(out var desc);

                    SendEvent(new EventWindowCreate(new GlfwWindowCreateResult(null, (desc, err)), tcs));
                    return;
                }

                // We can't invoke the TCS directly from the windowing thread because:
                // * it'd hit the synchronization context,
                //   which would make (blocking) main window init more annoying.
                // * it'd not be synchronized to other incoming window events correctly which might be icky.
                // So we send the TCS back to the game thread
                // which processes events in the correct order and has better control of stuff during init.
                var reg = WinThreadSetupWindow(window);

                SendEvent(new EventWindowCreate(new GlfwWindowCreateResult(reg, null), tcs));
            }

            private static void WinThreadWinDestroy(CmdWinDestroy cmd)
            {
                var window = (Window*) cmd.Window;

                if (OperatingSystem.IsWindows() && cmd.hadOwner)
                {
                    // On Windows, closing the child window causes the owner to be minimized, apparently.
                    // Clear owner on close to avoid this.

                    var hWnd = (HWND) GLFW.GetWin32Window(window);
                    DebugTools.Assert(hWnd != HWND.NULL);

                    Windows.SetWindowLongPtrW(
                        hWnd,
                        GWLP.GWLP_HWNDPARENT,
                        0);
                }

                GLFW.DestroyWindow((Window*) cmd.Window);
            }

            private Window* CreateGlfwWindowForRenderer(
                GLContextSpec? spec,
                WindowCreateParameters parameters,
                Window* contextShare,
                Window* ownerWindow)
            {
                GLFW.WindowHint(WindowHintString.X11ClassName, "RobustToolbox");
                GLFW.WindowHint(WindowHintString.X11InstanceName, "RobustToolbox");
                GLFW.WindowHint(WindowHintBool.ScaleToMonitor, true);

                if (spec == null)
                {
                    // No OpenGL context requested.
                    GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
                }
                else
                {
                    var s = spec.Value;

#if DEBUG
                    GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);
#endif

                    GLFW.WindowHint(WindowHintInt.ContextVersionMajor, s.Major);
                    GLFW.WindowHint(WindowHintInt.ContextVersionMinor, s.Minor);
                    GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, s.Profile != GLContextProfile.Compatibility);
                    GLFW.WindowHint(WindowHintBool.SrgbCapable, true);

                    switch (s.Profile)
                    {
                        case GLContextProfile.Compatibility:
                            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Any);
                            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
                            break;
                        case GLContextProfile.Core:
                            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
                            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
                            break;
                        case GLContextProfile.Es:
                            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Any);
                            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlEsApi);
                            break;
                    }

                    GLFW.WindowHint(WindowHintContextApi.ContextCreationApi,
                        s.CreationApi == GLContextCreationApi.Egl
                            ? ContextApi.EglContextApi
                            : ContextApi.NativeContextApi);

#if !FULL_RELEASE
                    if (s.CreationApi == GLContextCreationApi.Egl && !_eglLoaded && OperatingSystem.IsWindows())
                    {
                        // On non-published builds (so, development), GLFW can't find libEGL.dll
                        // because it'll be in runtimes/<rid>/native/ instead of next to the actual executable.
                        // We manually preload the library here so that GLFW will find it when it does its thing.
                        NativeLibrary.TryLoad(
                            "libEGL.dll",
                            typeof(Clyde).Assembly,
                            DllImportSearchPath.SafeDirectories,
                            out _);

                        _eglLoaded = true;
                    }
#endif
                }

                Monitor* monitor = null;
                if (parameters.Monitor != null &&
                    _winThreadMonitors.TryGetValue(parameters.Monitor.Id, out var monitorReg))
                {
                    monitor = monitorReg.Ptr;
                    var mode = GLFW.GetVideoMode(monitor);
                    // Set refresh rate to monitor's so that GLFW doesn't manually select one.
                    GLFW.WindowHint(WindowHintInt.RefreshRate, mode->RefreshRate);
                }
                else
                {
                    GLFW.WindowHint(WindowHintInt.RefreshRate, -1);
                }

                GLFW.WindowHint(WindowHintBool.Visible, false);

                GLFW.WindowHint(WindowHintInt.RedBits, 8);
                GLFW.WindowHint(WindowHintInt.GreenBits, 8);
                GLFW.WindowHint(WindowHintInt.BlueBits, 8);
                GLFW.WindowHint(WindowHintInt.AlphaBits, 8);
                GLFW.WindowHint(WindowHintInt.StencilBits, 8);

                var window = GLFW.CreateWindow(
                    parameters.Width, parameters.Height,
                    parameters.Title,
                    parameters.Fullscreen ? monitor : null,
                    contextShare);

                // Check if window failed to create.
                if (window == null)
                    return null;

                if (parameters.Maximized)
                {
                    GLFW.GetMonitorPos(monitor, out var x, out var y);
                    GLFW.SetWindowPos(window, x, y);
                    GLFW.MaximizeWindow(window);
                }

                if ((parameters.Styles & OSWindowStyles.NoTitleOptions) != 0)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        var hWnd = (HWND) GLFW.GetWin32Window(window);
                        DebugTools.Assert(hWnd != HWND.NULL);

                        Windows.SetWindowLongPtrW(
                            hWnd,
                            GWL.GWL_STYLE,
                            // Cast to long here to work around a bug in rider with nint bitwise operators.
                            (nint)((long)Windows.GetWindowLongPtrW(hWnd, GWL.GWL_STYLE) & ~WS.WS_SYSMENU));
                    }
                    else
                    {
                        _sawmill.Warning("OSWindowStyles.NoTitleOptions not implemented on this platform");
                    }
                }

                if (ownerWindow != null)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        var hWnd = (HWND) GLFW.GetWin32Window(window);
                        var ownerHWnd = (HWND) GLFW.GetWin32Window(ownerWindow);
                        DebugTools.Assert(hWnd != HWND.NULL);

                        Windows.SetWindowLongPtrW(
                            hWnd,
                            GWLP.GWLP_HWNDPARENT,
                            ownerHWnd);
                    }
                    else
                    {
                        _sawmill.Warning("owner windows not implemented on this platform");
                    }


                    if (parameters.StartupLocation == WindowStartupLocation.CenterOwner)
                    {
                        // TODO: Maybe include window frames in size calculations here?
                        // Figure out frame sizes of both windows.
                        GLFW.GetWindowPos(ownerWindow, out var ownerX, out var ownerY);
                        GLFW.GetWindowSize(ownerWindow, out var ownerW, out var ownerH);

                        // Re-fetch this in case DPI scaling is changing it I guess.
                        GLFW.GetWindowSize(window, out var thisW, out var thisH);

                        GLFW.SetWindowPos(window, ownerX + (ownerW - thisW) / 2, ownerY + (ownerH - thisH) / 2);
                    }
                }

                if (parameters.Visible)
                {
                    GLFW.ShowWindow(window);
                }

                return window;
            }

            private GlfwWindowReg WinThreadSetupWindow(Window* window)
            {
                var reg = new GlfwWindowReg
                {
                    GlfwWindow = window,
                    Id = new WindowId(_nextWindowId++)
                };
                var handle = new WindowHandle(_clyde, reg);
                reg.Handle = handle;

                LoadWindowIcon(window);

                GLFW.SetCharCallback(window, _charCallback);
                GLFW.SetKeyCallback(window, _keyCallback);
                GLFW.SetWindowCloseCallback(window, _windowCloseCallback);
                GLFW.SetCursorPosCallback(window, _cursorPosCallback);
                GLFW.SetCursorEnterCallback(window, _cursorEnterCallback);
                GLFW.SetWindowSizeCallback(window, _windowSizeCallback);
                GLFW.SetWindowPosCallback(window, _windowPosCallback);
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

                return reg;
            }

            private WindowReg? FindWindow(nint window) => FindWindow((Window*) window);

            private WindowReg? FindWindow(Window* window)
            {
                foreach (var windowReg in _clyde._windows)
                {
                    var glfwReg = (GlfwWindowReg) windowReg;
                    if (glfwReg.GlfwWindow == window)
                    {
                        return windowReg;
                    }
                }

                return null;
            }

            public Task<string> ClipboardGetText(WindowReg mainWindow)
            {
                var tcs = new TaskCompletionSource<string>();
                SendCmd(new CmdGetClipboard((nint) ((GlfwWindowReg) mainWindow).GlfwWindow, tcs));
                return tcs.Task;
            }

            private static void WinThreadGetClipboard(CmdGetClipboard cmd)
            {
                var clipboard = GLFW.GetClipboardString((Window*) cmd.Window);
                // Don't have to care about synchronization I don't think so just fire this immediately.
                cmd.Tcs.TrySetResult(clipboard);
            }

            public void ClipboardSetText(WindowReg mainWindow, string text)
            {
                SendCmd(new CmdSetClipboard((nint) ((GlfwWindowReg) mainWindow).GlfwWindow, text));
            }

            private static void WinThreadSetClipboard(CmdSetClipboard cmd)
            {
                GLFW.SetClipboardString((Window*) cmd.Window, cmd.Text);
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

            public void GLMakeContextCurrent(WindowReg? window)
            {
                if (window != null)
                {
                    CheckWindowDisposed(window);

                    var reg = (GlfwWindowReg)window;

                    GLFW.MakeContextCurrent(reg.GlfwWindow);
                }
                else
                {
                    GLFW.MakeContextCurrent(null);
                }
            }

            public void GLSwapInterval(int interval)
            {
                GLFW.SwapInterval(interval);
            }

            public void* GLGetProcAddress(string procName)
            {
                return (void*) GLFW.GetProcAddress(procName);
            }

            private void CheckWindowDisposed(WindowReg reg)
            {
                if (reg.IsDisposed)
                    throw new ObjectDisposedException("Window disposed");
            }

            private sealed class GlfwWindowReg : WindowReg
            {
                public Window* GlfwWindow;

                // Kept around to avoid it being GCd.
                public CursorImpl? Cursor;
            }
        }
    }
}
