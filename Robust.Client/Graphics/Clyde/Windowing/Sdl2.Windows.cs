using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static SDL2.SDL;
using static SDL2.SDL.SDL_bool;
using static SDL2.SDL.SDL_FlashOperation;
using static SDL2.SDL.SDL_GLattr;
using static SDL2.SDL.SDL_GLcontext;
using static SDL2.SDL.SDL_GLprofile;
using static SDL2.SDL.SDL_SYSWM_TYPE;
using static SDL2.SDL.SDL_WindowFlags;
using BOOL = TerraFX.Interop.Windows.BOOL;
using HWND = TerraFX.Interop.Windows.HWND;
using GWLP = TerraFX.Interop.Windows.GWLP;
using Windows = TerraFX.Interop.Windows.Windows;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl2WindowingImpl
    {
        private int _nextWindowId = 1;

        public (WindowReg?, string? error) WindowCreate(
            GLContextSpec? spec,
            WindowCreateParameters parameters,
            WindowReg? share,
            WindowReg? owner)
        {
            nint shareWindow = 0;
            nint shareContext = 0;
            if (share is Sdl2WindowReg shareReg)
            {
                shareWindow = shareReg.Sdl2Window;
                shareContext = shareReg.GlContext;
            }

            nint ownerPtr = 0;
            if (owner is Sdl2WindowReg ownerReg)
                ownerPtr = ownerReg.Sdl2Window;

            var task = SharedWindowCreate(spec, parameters, shareWindow, shareContext, ownerPtr);

            // Block the main thread (to avoid stuff like texture uploads being problematic).
            WaitWindowCreate(task);

#pragma warning disable RA0004
            // Block above ensured task is done, this is safe.
            var (reg, error) = task.Result;
#pragma warning restore RA0004
            if (reg != null)
            {
                reg.Owner = reg.Handle;
            }

            return (reg, error);
        }

        private void WaitWindowCreate(Task<Sdl2WindowCreateResult> windowTask)
        {
            while (!windowTask.IsCompleted)
            {
                // Keep processing events until the window task gives either an error or success.
                WaitEvents();
                ProcessEvents(single: true);
            }
        }

        private Task<Sdl2WindowCreateResult> SharedWindowCreate(
            GLContextSpec? glSpec,
            WindowCreateParameters parameters,
            nint shareWindow,
            nint shareContext,
            nint owner)
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
            var tcs = new TaskCompletionSource<Sdl2WindowCreateResult>();
            SendCmd(new CmdWinCreate(glSpec, parameters, shareWindow, shareContext, owner, tcs));
            return tcs.Task;
        }

        private static void FinishWindowCreate(EventWindowCreate ev)
        {
            var (res, tcs) = ev;

            tcs.TrySetResult(res);
        }

        private void WinThreadWinCreate(CmdWinCreate cmd)
        {
            var (glSpec, parameters, shareWindow, shareContext, owner, tcs) = cmd;

            var (window, context) = CreateSdl2WindowForRenderer(glSpec, parameters, shareWindow, shareContext, owner);

            if (window == 0)
            {
                var err = SDL_GetError();

                SendEvent(new EventWindowCreate(new Sdl2WindowCreateResult(null, err), tcs));
                return;
            }

            // We can't invoke the TCS directly from the windowing thread because:
            // * it'd hit the synchronization context,
            //   which would make (blocking) main window init more annoying.
            // * it'd not be synchronized to other incoming window events correctly which might be icky.
            // So we send the TCS back to the game thread
            // which processes events in the correct order and has better control of stuff during init.
            var reg = WinThreadSetupWindow(window, context);

            SendEvent(new EventWindowCreate(new Sdl2WindowCreateResult(reg, null), tcs));
        }

        private static void WinThreadWinDestroy(CmdWinDestroy cmd)
        {
            if (OperatingSystem.IsWindows() && cmd.HadOwner)
            {
                // On Windows, closing the child window causes the owner to be minimized, apparently.
                // Clear owner on close to avoid this.

                SDL_SysWMinfo wmInfo = default;
                if (SDL_GetWindowWMInfo(cmd.Window, ref wmInfo) == SDL_TRUE && wmInfo.subsystem == SDL_SYSWM_WINDOWS)
                {
                    var hWnd = (HWND)wmInfo.info.win.window;
                    DebugTools.Assert(hWnd != HWND.NULL);

                    Windows.SetWindowLongPtrW(
                        hWnd,
                        GWLP.GWLP_HWNDPARENT,
                        0);
                }
            }

            SDL_DestroyWindow(cmd.Window);
        }

        private (nint window, nint context) CreateSdl2WindowForRenderer(
            GLContextSpec? spec,
            WindowCreateParameters parameters,
            nint shareWindow,
            nint shareContext,
            nint ownerWindow)
        {
            var windowFlags = SDL_WINDOW_HIDDEN | SDL_WINDOW_RESIZABLE;

            if (spec is { } s)
            {
                windowFlags |= SDL_WINDOW_OPENGL;

                SDL_GL_SetAttribute(SDL_GL_RED_SIZE, 8);
                SDL_GL_SetAttribute(SDL_GL_GREEN_SIZE, 8);
                SDL_GL_SetAttribute(SDL_GL_BLUE_SIZE, 8);
                SDL_GL_SetAttribute(SDL_GL_ALPHA_SIZE, 8);
                SDL_GL_SetAttribute(SDL_GL_STENCIL_SIZE, 8);
                SDL_GL_SetAttribute(SDL_GL_FRAMEBUFFER_SRGB_CAPABLE, s.Profile == GLContextProfile.Es ? 0 : 1);
                SDL_GLcontext ctxFlags = 0;
#if DEBUG
                ctxFlags |= SDL_GL_CONTEXT_DEBUG_FLAG;
#endif
                if (s.Profile == GLContextProfile.Core)
                    ctxFlags |= SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;

                SDL_GL_SetAttribute(SDL_GL_CONTEXT_FLAGS, (int)ctxFlags);

                if (shareContext != 0)
                {
                    SDL_GL_MakeCurrent(shareWindow, shareContext);
                    SDL_GL_SetAttribute(SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);
                }
                else
                {
                    SDL_GL_SetAttribute(SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 0);
                }

                var profile = s.Profile switch
                {
                    GLContextProfile.Compatibility => SDL_GL_CONTEXT_PROFILE_COMPATIBILITY,
                    GLContextProfile.Core => SDL_GL_CONTEXT_PROFILE_CORE,
                    GLContextProfile.Es => SDL_GL_CONTEXT_PROFILE_ES,
                    _ => SDL_GL_CONTEXT_PROFILE_COMPATIBILITY,
                };

                SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, profile);
                SDL_SetHint("SDL_OPENGL_ES_DRIVER", s.CreationApi == GLContextCreationApi.Egl ? "1" : "0");

                SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, s.Major);
                SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, s.Minor);

                if (s.CreationApi == GLContextCreationApi.Egl)
                    WsiShared.EnsureEglAvailable();
            }

            nint window = SDL_CreateWindow(
                "",
                SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED,
                parameters.Width, parameters.Height,
                windowFlags);

            if (window == 0)
                return default;

            nint glContext = SDL_GL_CreateContext(window);
            if (glContext == 0)
            {
                SDL_DestroyWindow(window);
                return default;
            }

            // TODO: Monitors, window maximize, fullscreen.
            // TODO: a bunch of win32 calls for funny window properties I still haven't ported to other platforms.

            // Make sure window thread doesn't keep hold of the GL context.
            SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero);


            if (OperatingSystem.IsWindows())
            {
                SDL_SysWMinfo info = default;
                if (SDL_GetWindowWMInfo(window, ref info) == SDL_TRUE && info.subsystem == SDL_SYSWM_WINDOWS)
                    WsiShared.WindowsSharedWindowCreate((HWND) info.info.win.window, _cfg);
            }

            if (parameters.Visible)
                SDL_ShowWindow(window);

            return (window, glContext);
        }

        private unsafe Sdl2WindowReg WinThreadSetupWindow(nint window, nint context)
        {
            var reg = new Sdl2WindowReg
            {
                Sdl2Window = window,
                GlContext = context,
                WindowId = SDL_GetWindowID(window),
                Id = new WindowId(_nextWindowId++)
            };
            var handle = new WindowHandle(_clyde, reg);
            reg.Handle = handle;

            var res = SDL_GetWindowWMInfo(window, ref reg.SysWMinfo);
            if (res == SDL_FALSE)
                _sawmill.Error("Failed to get window WM info: {error}", SDL_GetError());

            // LoadWindowIcon(window);

            SDL_GL_GetDrawableSize(window, out var fbW, out var fbH);
            reg.FramebufferSize = (fbW, fbH);

            reg.WindowScale = GetWindowScale(window);

            SDL_GetWindowSize(window, out var w, out var h);
            reg.PrevWindowSize = reg.WindowSize = (w, h);

            SDL_GetWindowPosition(window, out var x, out var y);
            reg.PrevWindowPos = (x, y);

            reg.PixelRatio = reg.FramebufferSize / (Vector2) reg.WindowSize;

            return reg;
        }

        public void WindowDestroy(WindowReg window)
        {
            var reg = (Sdl2WindowReg) window;
            SendCmd(new CmdWinDestroy(reg.Sdl2Window, window.Owner != null));
        }

        public void UpdateMainWindowMode()
        {
            if (_clyde._mainWindow == null)
                return;

            var win = (Sdl2WindowReg) _clyde._mainWindow;

            SendCmd(new CmdWinWinSetMode(win.Sdl2Window, _clyde._windowMode));
        }

        private static void WinThreadWinSetMode(CmdWinWinSetMode cmd)
        {
            var flags = cmd.Mode switch
            {
                WindowMode.Fullscreen => (uint) SDL_WINDOW_FULLSCREEN_DESKTOP,
                _ => 0u
            };

            SDL_SetWindowFullscreen(cmd.Window, flags);
        }

        public void WindowSetTitle(WindowReg window, string title)
        {
            SendCmd(new CmdWinSetTitle(WinPtr(window), title));
        }

        private static void WinThreadWinSetTitle(CmdWinSetTitle cmd)
        {
            SDL_SetWindowTitle(cmd.Window, cmd.Title);
        }

        public void WindowSetMonitor(WindowReg window, IClydeMonitor monitor)
        {
            // API isn't really used and kinda wack, don't feel like figuring it out for SDL2 yet.
            _sawmill.Warning("WindowSetMonitor not implemented on SDL2");
        }

        public void WindowSetVisible(WindowReg window, bool visible)
        {
            SendCmd(new CmdWinSetVisible(WinPtr(window), visible));
        }

        private static void WinThreadWinSetVisible(CmdWinSetVisible cmd)
        {
            if (cmd.Visible)
                SDL_ShowWindow(cmd.Window);
            else
                SDL_HideWindow(cmd.Window);
        }

        public void WindowRequestAttention(WindowReg window)
        {
            SendCmd(new CmdWinRequestAttention(WinPtr(window)));
        }

        private void WinThreadWinRequestAttention(CmdWinRequestAttention cmd)
        {
            var res = SDL_FlashWindow(cmd.Window, SDL_FLASH_UNTIL_FOCUSED);
            if (res < 0)
                _sawmill.Error("Failed to flash window: {error}", SDL_GetError());
        }

        public unsafe void WindowSwapBuffers(WindowReg window)
        {
            var reg = (Sdl2WindowReg)window;
            var windowPtr = WinPtr(reg);

            // On Windows, SwapBuffers does not correctly sync to the DWM compositor.
            // This means OpenGL vsync is effectively broken by default on Windows.
            // We manually sync via DwmFlush(). GLFW does this automatically, SDL2 does not.
            //
            // Windows DwmFlush logic partly taken from:
            // https://github.com/love2d/love/blob/5175b0d1b599ea4c7b929f6b4282dd379fa116b8/src/modules/window/sdl/Window.cpp#L1018
            // https://github.com/glfw/glfw/blob/d3ede7b6847b66cf30b067214b2b4b126d4c729b/src/wgl_context.c#L321-L340
            // See also: https://github.com/libsdl-org/SDL/issues/5797

            var dwmFlush = false;
            var swapInterval = 0;

            if (OperatingSystem.IsWindows() && !reg.Fullscreen && reg.SwapInterval > 0)
            {
                BOOL compositing;
                // 6.2 is Windows 8
                // https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw
                if (OperatingSystem.IsWindowsVersionAtLeast(6, 2)
                    || Windows.SUCCEEDED(Windows.DwmIsCompositionEnabled(&compositing)) && compositing)
                {
                    var curCtx = SDL_GL_GetCurrentContext();
                    var curWin = SDL_GL_GetCurrentWindow();

                    if (curCtx != reg.GlContext || curWin != reg.Sdl2Window)
                        throw new InvalidOperationException("Window context must be current!");

                    SDL_GL_SetSwapInterval(0);
                    dwmFlush = true;
                    swapInterval = reg.SwapInterval;
                }
            }

            SDL_GL_SwapWindow(windowPtr);

            if (dwmFlush)
            {
                var i = swapInterval;
                while (i-- > 0)
                {
                    Windows.DwmFlush();
                }

                SDL_GL_SetSwapInterval(swapInterval);
            }
        }

        public uint? WindowGetX11Id(WindowReg window)
        {
            CheckWindowDisposed(window);

            var reg = (Sdl2WindowReg) window;

            if (reg.SysWMinfo.subsystem != SDL_SYSWM_X11)
                return null;

            return (uint?) reg.SysWMinfo.info.x11.window;
        }

        public nint? WindowGetX11Display(WindowReg window)
        {
            CheckWindowDisposed(window);

            var reg = (Sdl2WindowReg) window;

            if (reg.SysWMinfo.subsystem != SDL_SYSWM_X11)
                return null;

            return reg.SysWMinfo.info.x11.display;
        }

        public nint? WindowGetWin32Window(WindowReg window)
        {
            CheckWindowDisposed(window);

            var reg = (Sdl2WindowReg) window;

            if (reg.SysWMinfo.subsystem != SDL_SYSWM_WINDOWS)
                return null;

            return reg.SysWMinfo.info.win.window;
        }

        public void RunOnWindowThread(Action a)
        {
            SendCmd(new CmdRunAction(a));
        }

        public void TextInputSetRect(UIBox2i rect)
        {
            SendCmd(new CmdTextInputSetRect(new SDL_Rect
            {
                x = rect.Left,
                y = rect.Top,
                w = rect.Width,
                h = rect.Height
            }));
        }

        private static void WinThreadSetTextInputRect(CmdTextInputSetRect cmdTextInput)
        {
            var rect = cmdTextInput.Rect;
            SDL_SetTextInputRect(ref rect);
        }

        public void TextInputStart()
        {
            SendCmd(CmdTextInputStart.Instance);
        }

        private static void WinThreadStartTextInput()
        {
            SDL_StartTextInput();
        }

        public void TextInputStop()
        {
            SendCmd(CmdTextInputStop.Instance);
        }

        private static void WinThreadStopTextInput()
        {
            SDL_StopTextInput();
        }

        public void ClipboardSetText(WindowReg mainWindow, string text)
        {
            SendCmd(new CmdSetClipboard(text));
        }

        private void WinThreadSetClipboard(CmdSetClipboard cmd)
        {
            var res = SDL_SetClipboardText(cmd.Text);
            if (res < 0)
                _sawmill.Error("Failed to set clipboard text: {error}", SDL_GetError());
        }

        public Task<string> ClipboardGetText(WindowReg mainWindow)
        {
            var tcs = new TaskCompletionSource<string>();
            SendCmd(new CmdGetClipboard(tcs));
            return tcs.Task;
        }

        private static void WinThreadGetClipboard(CmdGetClipboard cmd)
        {
            cmd.Tcs.TrySetResult(SDL_GetClipboardText());
        }

        private static (float h, float v) GetWindowScale(nint window)
        {
            var display = SDL_GetWindowDisplayIndex(window);
            SDL_GetDisplayDPI(display, out _, out var hDpi, out var vDpi);
            return (hDpi / 96f, vDpi / 96f);
        }

        private static void CheckWindowDisposed(WindowReg reg)
        {
            if (reg.IsDisposed)
                throw new ObjectDisposedException("Window disposed");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint WinPtr(WindowReg reg) => ((Sdl2WindowReg)reg).Sdl2Window;

        private WindowReg? FindWindow(uint windowId)
        {
            foreach (var windowReg in _clyde._windows)
            {
                var glfwReg = (Sdl2WindowReg) windowReg;
                if (glfwReg.WindowId == windowId)
                    return windowReg;
            }

            return null;
        }


        private sealed class Sdl2WindowReg : WindowReg
        {
            public nint Sdl2Window;
            public uint WindowId;
            public nint GlContext;
            public SDL_SysWMinfo SysWMinfo;
#pragma warning disable CS0649
            public bool Fullscreen;
#pragma warning restore CS0649
            public int SwapInterval;

            // Kept around to avoid it being GCd.
            public CursorImpl? Cursor;
        }

    }
}
