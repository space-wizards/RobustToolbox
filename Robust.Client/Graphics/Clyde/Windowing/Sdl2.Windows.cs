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
            nint sharePtr = 0;
            if (share is Sdl2WindowReg shareReg)
                sharePtr = shareReg.Sdl2Window;

            nint ownerPtr = 0;
            if (owner is Sdl2WindowReg ownerReg)
                ownerPtr = ownerReg.Sdl2Window;

            var task = SharedWindowCreate(spec, parameters, sharePtr, ownerPtr);

            // Block the main thread (to avoid stuff like texture uploads being problematic).
            WaitWindowCreate(task);

            var (reg, error) = task.Result;
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
            nint share, nint owner)
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
            SendCmd(new CmdWinCreate(glSpec, parameters, share, owner, tcs));
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

            var (window, context) = CreateSdl2WindowForRenderer(glSpec, parameters, share, owner);

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
            nint contextShare,
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
                SDL_GL_SetAttribute(SDL_GL_FRAMEBUFFER_SRGB_CAPABLE, 1);
                SDL_GLcontext ctxFlags = 0;
#if DEBUG
                ctxFlags |= SDL_GL_CONTEXT_DEBUG_FLAG;
#endif
                if (s.Profile != GLContextProfile.Compatibility)
                    ctxFlags |= SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;

                SDL_GL_SetAttribute(SDL_GL_CONTEXT_RELEASE_BEHAVIOR, (int)ctxFlags);

                var profile = s.Profile switch
                {
                    GLContextProfile.Compatibility => SDL_GL_CONTEXT_PROFILE_COMPATIBILITY,
                    GLContextProfile.Core => SDL_GL_CONTEXT_PROFILE_CORE,
                    GLContextProfile.Es => SDL_GL_CONTEXT_PROFILE_ES,
                    _ => SDL_GL_CONTEXT_PROFILE_COMPATIBILITY,
                };

                SDL_SetHint("SDL_OPENGL_ES_DRIVER", s.CreationApi == GLContextCreationApi.Egl ? "1" : "0");

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

            if (parameters.Visible)
                SDL_ShowWindow(window);

            return (window, glContext);
        }

        private Sdl2WindowReg WinThreadSetupWindow(nint window, nint context)
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

        public void WindowDestroy(WindowReg reg)
        {
            throw new NotImplementedException();
        }

        public void WindowSetTitle(WindowReg window, string title)
        {
            SendCmd(new CmdWinSetTitle(WinPtr(window), title));
        }

        public void WindowSetMonitor(WindowReg window, IClydeMonitor monitor)
        {
            throw new NotImplementedException();
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

        public void WindowSwapBuffers(WindowReg window)
        {
            var windowPtr = WinPtr(window);
            SDL_GL_SwapWindow(windowPtr);
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

        private static void WinThreadWinSetTitle(CmdWinSetTitle cmd)
        {
            SDL_SetWindowTitle(cmd.Window, cmd.Title);
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

            // Kept around to avoid it being GCd.
            public CursorImpl? Cursor;
        }

    }
}
