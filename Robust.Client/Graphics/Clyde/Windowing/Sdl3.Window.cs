using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using SDL3;
using TerraFX.Interop.Windows;
using TerraFX.Interop.Xlib;
using BOOL = TerraFX.Interop.Windows.BOOL;
using Windows = TerraFX.Interop.Windows.Windows;
using GLAttr = SDL3.SDL.SDL_GLAttr;
using X11Window = TerraFX.Interop.Xlib.Window;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl
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
            if (share is Sdl3WindowReg shareReg)
            {
                shareWindow = shareReg.Sdl3Window;
                shareContext = shareReg.GlContext;
            }

            nint ownerPtr = 0;
            if (owner is Sdl3WindowReg ownerReg)
                ownerPtr = ownerReg.Sdl3Window;

            var task = SharedWindowCreate(spec, parameters, shareWindow, shareContext, ownerPtr);

            // Block the main thread (to avoid stuff like texture uploads being problematic).
            WaitWindowCreate(task);

#pragma warning disable RA0004
            // Block above ensured task is done, this is safe.
            var result = task.Result;
#pragma warning restore RA0004
            if (result.Reg != null)
            {
                result.Reg.Owner = result.Reg.Handle;
            }

            return (result.Reg, result.Error);
        }

        private void WaitWindowCreate(Task<Sdl3WindowCreateResult> windowTask)
        {
            while (!windowTask.IsCompleted)
            {
                // Keep processing events until the window task gives either an error or success.
                WaitEvents();
                ProcessEvents(single: true);
            }
        }

        private Task<Sdl3WindowCreateResult> SharedWindowCreate(
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
            var tcs = new TaskCompletionSource<Sdl3WindowCreateResult>();
            SendCmd(new CmdWinCreate
            {
                GLSpec = glSpec,
                Parameters = parameters,
                ShareWindow = shareWindow,
                ShareContext = shareContext,
                OwnerWindow = owner,
                Tcs = tcs
            });
            return tcs.Task;
        }

        private static void FinishWindowCreate(EventWindowCreate ev)
        {
            ev.Tcs.TrySetResult(ev.Result);
        }

        private void WinThreadWinCreate(CmdWinCreate cmd)
        {
            var (window, context) = CreateSdl3WindowForRenderer(
                cmd.GLSpec,
                cmd.Parameters,
                cmd.ShareWindow,
                cmd.ShareContext,
                cmd.OwnerWindow);

            if (window == 0)
            {
                var err = SDL.SDL_GetError();

                SendEvent(new EventWindowCreate
                {
                    Result = new Sdl3WindowCreateResult { Error = err },
                    Tcs = cmd.Tcs
                });
                return;
            }

            // We can't invoke the TCS directly from the windowing thread because:
            // * it'd hit the synchronization context,
            //   which would make (blocking) main window init more annoying.
            // * it'd not be synchronized to other incoming window events correctly which might be icky.
            // So we send the TCS back to the game thread
            // which processes events in the correct order and has better control of stuff during init.
            var reg = WinThreadSetupWindow(window, context);

            SendEvent(new EventWindowCreate
            {
                Result = new Sdl3WindowCreateResult { Reg = reg },
                Tcs = cmd.Tcs
            });
        }

        private static void WinThreadWinDestroy(CmdWinDestroy cmd)
        {
            SDL.SDL_DestroyWindow(cmd.Window);
        }

        private (nint window, nint context) CreateSdl3WindowForRenderer(
            GLContextSpec? spec,
            WindowCreateParameters parameters,
            nint shareWindow,
            nint shareContext,
            nint ownerWindow)
        {
            var createProps = SDL.SDL_CreateProperties();
            SDL.SDL_SetBooleanProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_HIDDEN_BOOLEAN, true);
            SDL.SDL_SetBooleanProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_RESIZABLE_BOOLEAN, true);
            SDL.SDL_SetBooleanProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_HIGH_PIXEL_DENSITY_BOOLEAN, true);

            if (spec is { } s)
            {
                SDL.SDL_SetBooleanProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_OPENGL_BOOLEAN, true);

                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_RED_SIZE, 8);
                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_GREEN_SIZE, 8);
                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_BLUE_SIZE, 8);
                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_ALPHA_SIZE, 8);
                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_STENCIL_SIZE, 8);
                SDL.SDL_GL_SetAttribute(
                    GLAttr.SDL_GL_FRAMEBUFFER_SRGB_CAPABLE,
                    s.Profile == GLContextProfile.Es ? 0 : 1);
                int ctxFlags = 0;
#if DEBUG
                ctxFlags |= SDL.SDL_GL_CONTEXT_DEBUG_FLAG;
#endif
                if (s.Profile == GLContextProfile.Core)
                    ctxFlags |= SDL.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;

                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_CONTEXT_FLAGS, (int)ctxFlags);

                if (shareContext != 0)
                {
                    SDL.SDL_GL_MakeCurrent(shareWindow, shareContext);
                    SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);
                }
                else
                {
                    SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 0);
                }

                var profile = s.Profile switch
                {
                    GLContextProfile.Compatibility => SDL.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY,
                    GLContextProfile.Core => SDL.SDL_GL_CONTEXT_PROFILE_CORE,
                    GLContextProfile.Es => SDL.SDL_GL_CONTEXT_PROFILE_ES,
                    _ => SDL.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY,
                };

                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, profile);
                SDL.SDL_SetHint(SDL.SDL_HINT_OPENGL_ES_DRIVER, s.CreationApi == GLContextCreationApi.Egl ? "1" : "0");

                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, s.Major);
                SDL.SDL_GL_SetAttribute(GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, s.Minor);

                if (s.CreationApi == GLContextCreationApi.Egl)
                    WsiShared.EnsureEglAvailable();
            }

            if (parameters.Fullscreen)
                SDL.SDL_SetBooleanProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_FULLSCREEN_BOOLEAN, true);

            if ((parameters.Styles & OSWindowStyles.NoTitleBar) != 0)
                SDL.SDL_SetBooleanProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_BORDERLESS_BOOLEAN, true);

            if (ownerWindow != 0)
            {
                SDL.SDL_SetPointerProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_PARENT_POINTER, ownerWindow);

                if (parameters.StartupLocation == WindowStartupLocation.CenterOwner)
                {
                    SDL.SDL_GetWindowSize(ownerWindow, out var parentW, out var parentH);
                    SDL.SDL_GetWindowPosition(ownerWindow, out var parentX, out var parentY);

                    SDL.SDL_SetNumberProperty(
                        createProps,
                        SDL.SDL_PROP_WINDOW_CREATE_X_NUMBER,
                        parentX + (parentW - parameters.Width) / 2);
                    SDL.SDL_SetNumberProperty(
                        createProps,
                        SDL.SDL_PROP_WINDOW_CREATE_Y_NUMBER,
                        parentY + (parentH - parameters.Height) / 2);
                }
            }

            SDL.SDL_SetNumberProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, parameters.Width);
            SDL.SDL_SetNumberProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, parameters.Height);
            SDL.SDL_SetStringProperty(createProps, SDL.SDL_PROP_WINDOW_CREATE_TITLE_STRING, parameters.Title);

            // ---> CREATE <---
            var window = SDL.SDL_CreateWindowWithProperties(createProps);

            SDL.SDL_DestroyProperties(createProps);

            if (window == 0)
                return default;

            nint glContext = SDL.SDL_GL_CreateContext(window);
            if (glContext == 0)
            {
                SDL.SDL_DestroyWindow(window);
                return default;
            }

            if ((parameters.Styles & OSWindowStyles.NoTitleOptions) != 0)
            {
                var props = SDL.SDL_GetWindowProperties(window);
                switch (_videoDriver)
                {
                    case SdlVideoDriver.Windows:
                    {
                        var hWnd = SDL.SDL_GetPointerProperty(
                            props,
                            SDL.SDL_PROP_WINDOW_WIN32_HWND_POINTER,
                            0);
                        WsiShared.SetWindowStyleNoTitleOptionsWindows((HWND)hWnd);
                        break;
                    }
                    case SdlVideoDriver.X11:
                        unsafe
                        {
                            var x11Display = (Display*)SDL.SDL_GetPointerProperty(
                                props,
                                SDL.SDL_PROP_WINDOW_X11_DISPLAY_POINTER,
                                0);
                            var x11Window = (X11Window)SDL.SDL_GetNumberProperty(
                                props,
                                SDL.SDL_PROP_WINDOW_X11_WINDOW_NUMBER,
                                0);
                            WsiShared.SetWindowStyleNoTitleOptionsX11(x11Display, x11Window);
                            break;
                        }

                    default:
                        _sawmill.Warning("OSWindowStyles.NoTitleOptions not implemented on this video driver");
                        break;
                }
            }

            // TODO: Monitors, window maximize.

            // Make sure window thread doesn't keep hold of the GL context.
            SDL.SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero);

            if (parameters.Visible)
                SDL.SDL_ShowWindow(window);

            return (window, glContext);
        }

        private Sdl3WindowReg WinThreadSetupWindow(nint window, nint context)
        {
            var reg = new Sdl3WindowReg
            {
                Sdl3Window = window,
                GlContext = context,
                WindowId = SDL.SDL_GetWindowID(window),
                Id = new WindowId(_nextWindowId++)
            };
            var handle = new WindowHandle(_clyde, reg);
            reg.Handle = handle;

            var windowProps = SDL.SDL_GetWindowProperties(window);
            switch (_videoDriver)
            {
                case SdlVideoDriver.Windows:
                    reg.WindowsHwnd = SDL.SDL_GetPointerProperty(
                        windowProps,
                        SDL.SDL_PROP_WINDOW_WIN32_HWND_POINTER,
                        0);
                    break;
                case SdlVideoDriver.X11:
                    reg.X11Display = SDL.SDL_GetPointerProperty(
                        windowProps,
                        SDL.SDL_PROP_WINDOW_X11_DISPLAY_POINTER,
                        0);
                    reg.X11Id = (uint)SDL.SDL_GetNumberProperty(windowProps, SDL.SDL_PROP_WINDOW_X11_WINDOW_NUMBER, 0);
                    break;
            }

            AssignWindowIconToWindow(window);

            SDL.SDL_GetWindowSizeInPixels(window, out var fbW, out var fbH);
            reg.FramebufferSize = (fbW, fbH);

            var scale = SDL.SDL_GetWindowDisplayScale(window);
            reg.WindowScale = new Vector2(scale, scale);

            SDL.SDL_GetWindowSize(window, out var w, out var h);
            reg.PrevWindowSize = reg.WindowSize = (w, h);

            SDL.SDL_GetWindowPosition(window, out var x, out var y);
            reg.PrevWindowPos = reg.WindowPos = (x, y);

            reg.PixelRatio = reg.FramebufferSize / (Vector2)reg.WindowSize;

            return reg;
        }

        public void WindowDestroy(WindowReg window)
        {
            var reg = (Sdl3WindowReg)window;
            SendCmd(new CmdWinDestroy
            {
                Window = reg.Sdl3Window,
                HadOwner = window.Owner != null
            });
        }

        public void UpdateMainWindowMode()
        {
            if (_clyde._mainWindow == null)
                return;

            var win = (Sdl3WindowReg)_clyde._mainWindow;

            if (_clyde._windowMode == WindowMode.Fullscreen)
            {
                win.PrevWindowSize = win.WindowSize;
                win.PrevWindowPos = win.WindowPos;

                SendCmd(new CmdWinWinSetFullscreen
                {
                    Window = win.Sdl3Window,
                });
            }
            else
            {
                SendCmd(new CmdWinSetWindowed
                {
                    Window = win.Sdl3Window,
                    Width = win.PrevWindowSize.X,
                    Height = win.PrevWindowSize.Y,
                    PosX = win.PrevWindowPos.X,
                    PosY = win.PrevWindowPos.Y
                });
            }
        }

        private static void WinThreadWinSetFullscreen(CmdWinWinSetFullscreen cmd)
        {
            SDL.SDL_SetWindowFullscreen(cmd.Window, true);
        }

        private static void WinThreadWinSetWindowed(CmdWinSetWindowed cmd)
        {
            SDL.SDL_SetWindowFullscreen(cmd.Window, false);
            SDL.SDL_SetWindowSize(cmd.Window, cmd.Width, cmd.Height);
            SDL.SDL_SetWindowPosition(cmd.Window, cmd.PosX, cmd.PosY);
        }

        public void WindowSetTitle(WindowReg window, string title)
        {
            SendCmd(new CmdWinSetTitle
            {
                Window = WinPtr(window),
                Title = title,
            });
        }

        private static void WinThreadWinSetTitle(CmdWinSetTitle cmd)
        {
            SDL.SDL_SetWindowTitle(cmd.Window, cmd.Title);
        }

        public void WindowSetMonitor(WindowReg window, IClydeMonitor monitor)
        {
            // API isn't really used and kinda wack, don't feel like figuring it out for SDL3 yet.
            _sawmill.Warning("WindowSetMonitor not implemented on SDL3");
        }

        public void WindowSetSize(WindowReg window, Vector2i size)
        {
            SendCmd(new CmdWinSetSize { Window = WinPtr(window), W = size.X, H = size.Y });
        }

        public void WindowSetVisible(WindowReg window, bool visible)
        {
            window.IsVisible = visible;
            SendCmd(new CmdWinSetVisible { Window = WinPtr(window), Visible = visible });
        }

        private static void WinThreadWinSetSize(CmdWinSetSize cmd)
        {
            SDL.SDL_SetWindowSize(cmd.Window, cmd.W, cmd.H);
        }

        private static void WinThreadWinSetVisible(CmdWinSetVisible cmd)
        {
            if (cmd.Visible)
                SDL.SDL_ShowWindow(cmd.Window);
            else
                SDL.SDL_HideWindow(cmd.Window);
        }

        public void WindowRequestAttention(WindowReg window)
        {
            SendCmd(new CmdWinRequestAttention { Window = WinPtr(window) });
        }

        private void WinThreadWinRequestAttention(CmdWinRequestAttention cmd)
        {
            var res = SDL.SDL_FlashWindow(cmd.Window, SDL.SDL_FlashOperation.SDL_FLASH_UNTIL_FOCUSED);
            if (!res)
                _sawmill.Error("Failed to flash window: {error}", SDL.SDL_GetError());
        }

        public unsafe void WindowSwapBuffers(WindowReg window)
        {
            var reg = (Sdl3WindowReg)window;
            var windowPtr = WinPtr(reg);

            // On Windows, SwapBuffers does not correctly sync to the DWM compositor.
            // This means OpenGL vsync is effectively broken by default on Windows.
            // We manually sync via DwmFlush(). GLFW does this automatically, SDL3 does not.
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
                    var curCtx = SDL.SDL_GL_GetCurrentContext();
                    var curWin = SDL.SDL_GL_GetCurrentWindow();

                    if (curCtx != reg.GlContext || curWin != reg.Sdl3Window)
                        throw new InvalidOperationException("Window context must be current!");

                    SDL.SDL_GL_SetSwapInterval(0);
                    dwmFlush = true;
                    swapInterval = reg.SwapInterval;
                }
            }

            SDL.SDL_GL_SwapWindow(windowPtr);

            if (dwmFlush)
            {
                var i = swapInterval;
                while (i-- > 0)
                {
                    Windows.DwmFlush();
                }

                SDL.SDL_GL_SetSwapInterval(swapInterval);
            }
        }

        public uint? WindowGetX11Id(WindowReg window)
        {
            CheckWindowDisposed(window);

            if (_videoDriver != SdlVideoDriver.X11)
                return null;

            var reg = (Sdl3WindowReg)window;
            return reg.X11Id;
        }

        public nint? WindowGetX11Display(WindowReg window)
        {
            CheckWindowDisposed(window);

            if (_videoDriver != SdlVideoDriver.X11)
                return null;

            var reg = (Sdl3WindowReg)window;
            return reg.X11Display;
        }

        public nint? WindowGetWin32Window(WindowReg window)
        {
            CheckWindowDisposed(window);

            if (_videoDriver != SdlVideoDriver.Windows)
                return null;

            var reg = (Sdl3WindowReg)window;
            return reg.WindowsHwnd;
        }

        public void RunOnWindowThread(Action a)
        {
            SendCmd(new CmdRunAction { Action = a });
        }

        public void TextInputSetRect(WindowReg reg, UIBox2i rect, int cursor)
        {
            SendCmd(new CmdTextInputSetRect
            {
                Window = WinPtr(reg),
                Rect = new SDL.SDL_Rect
                {
                    x = rect.Left,
                    y = rect.Top,
                    w = rect.Width,
                    h = rect.Height
                },
                Cursor = cursor
            });
        }

        private static void WinThreadSetTextInputRect(CmdTextInputSetRect cmdTextInput)
        {
            var rect = cmdTextInput.Rect;
            SDL.SDL_SetTextInputArea(cmdTextInput.Window, ref rect, cmdTextInput.Cursor);
        }

        public void TextInputStart(WindowReg reg)
        {
            SendCmd(new CmdTextInputStart { Window = WinPtr(reg) });
        }

        private static void WinThreadStartTextInput(CmdTextInputStart cmd)
        {
            SDL.SDL_StartTextInput(cmd.Window);
        }

        public void TextInputStop(WindowReg reg)
        {
            SendCmd(new CmdTextInputStop { Window = WinPtr(reg) });
        }

        private static void WinThreadStopTextInput(CmdTextInputStop cmd)
        {
            SDL.SDL_StopTextInput(cmd.Window);
        }

        public void ClipboardSetText(WindowReg mainWindow, string text)
        {
            SendCmd(new CmdSetClipboard { Text = text });
        }

        private void WinThreadSetClipboard(CmdSetClipboard cmd)
        {
            var res = SDL.SDL_SetClipboardText(cmd.Text);
            if (res)
                _sawmill.Error("Failed to set clipboard text: {error}", SDL.SDL_GetError());
        }

        public Task<string> ClipboardGetText(WindowReg mainWindow)
        {
            var tcs = new TaskCompletionSource<string>();
            SendCmd(new CmdGetClipboard { Tcs = tcs });
            return tcs.Task;
        }

        private static void WinThreadGetClipboard(CmdGetClipboard cmd)
        {
            cmd.Tcs.TrySetResult(SDL.SDL_GetClipboardText());
        }

        private static void CheckWindowDisposed(WindowReg reg)
        {
            if (reg.IsDisposed)
                throw new ObjectDisposedException("Window disposed");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint WinPtr(WindowReg reg) => ((Sdl3WindowReg)reg).Sdl3Window;

        private WindowReg? FindWindow(uint windowId)
        {
            foreach (var windowReg in _clyde._windows)
            {
                var glfwReg = (Sdl3WindowReg)windowReg;
                if (glfwReg.WindowId == windowId)
                    return windowReg;
            }

            return null;
        }


        private sealed class Sdl3WindowReg : WindowReg
        {
            public nint Sdl3Window;
            public uint WindowId;
            public nint GlContext;
#pragma warning disable CS0649
            public bool Fullscreen;
#pragma warning restore CS0649
            public int SwapInterval;

            // Kept around to avoid it being GCd.
            public CursorImpl? Cursor;

            public nint WindowsHwnd;
            public nint X11Display;
            public uint X11Id;
        }
    }
}
