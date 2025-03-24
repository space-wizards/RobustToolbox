﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using TerraFX.Interop.Windows;
using static Robust.Client.Graphics.Clyde.Egl;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        /// <summary>
        /// Context manager that uses EGL directly so that we get better control over multi-window management.
        /// </summary>
        private sealed unsafe class GLContextEgl : GLContextBase
        {
            // TODO: Currently this class uses ANGLE and Windows-specific initialization code.
            // It could be made more general purpose later if anybody ever gets adventurous with like, Wayland.

            private readonly Dictionary<WindowId, WindowData> _windowData = new();

            private readonly ISawmill _sawmill;

            private void* _eglDisplay;
            private void* _eglContext;
            private void* _eglConfig;

            public override bool HasBrokenWindowSrgb => Clyde._isGLES && OperatingSystem.IsWindows();

            public GLContextEgl(Clyde clyde) : base(clyde)
            {
                _sawmill = clyde._logManager.GetSawmill("clyde.ogl.egl");
            }

            public override GLContextSpec? SpecWithOpenGLVersion(RendererOpenGLVersion version)
            {
                return null;
            }

            public override void UpdateVSync()
            {
                var interval = Clyde._vSync ? 1 : 0;

                eglSwapInterval(_eglDisplay, interval);
            }

            public void InitializePublic()
            {
                var extensions = Marshal.PtrToStringUTF8((nint) eglQueryString(null, EGL_EXTENSIONS));
                _sawmill.Debug($"EGL client extensions: {extensions}!");
            }

            public override void WindowCreated(GLContextSpec? spec, WindowReg reg)
            {
                var data = new WindowData
                {
                    Reg = reg
                };
                _windowData[reg.Id] = data;

                if (reg.IsMainWindow)
                    Initialize(data);

                var attribs = stackalloc int[]
                {
                    EGL_GL_COLORSPACE, EGL_GL_COLORSPACE_SRGB,
                    EGL_NONE
                };

                if (OperatingSystem.IsWindows())
                {
                    // Set up window surface.
                    var hWNd = Clyde._windowing!.WindowGetWin32Window(reg)!.Value;
                    data.EglSurface = eglCreateWindowSurface(_eglDisplay, _eglConfig, (void*) hWNd, attribs);
                    if (data.EglSurface == (void*) EGL_NO_SURFACE)
                        throw new Exception("eglCreateWindowSurface failed.");
                }
                else if (OperatingSystem.IsLinux())
                {
                    var window = Clyde._windowing!.WindowGetX11Id(reg)!.Value;
                    data.EglSurface = eglCreateWindowSurface(_eglDisplay, _eglConfig, (void*) window, attribs);
                    if (data.EglSurface == (void*) EGL_NO_SURFACE)
                        throw new Exception("eglCreateWindowSurface failed.");
                }
                else
                {
                    throw new NotSupportedException("EGL is not currently supported outside Windows ANGLE or X11 Linux");
                }

                if (reg.IsMainWindow)
                {
                    var result = eglMakeCurrent(_eglDisplay, data.EglSurface, data.EglSurface, _eglContext);
                    if (result == EGL_FALSE)
                        throw new Exception("eglMakeCurrent failed.");
                }
            }

            public override void WindowDestroyed(WindowReg reg)
            {
                var data = _windowData[reg.Id];
                eglDestroySurface(_eglDisplay, data.EglSurface);
            }

            private void Initialize(WindowData mainWindow)
            {
                if (OperatingSystem.IsWindows())
                {
                    // Setting up ANGLE without manually selecting a D3D11 device requires a windows DC.
                    mainWindow.DC = Windows.GetDC((HWND)Clyde._windowing!.WindowGetWin32Window(mainWindow.Reg)!.Value);

                    _eglDisplay = eglGetPlatformDisplayEXT(EGL_PLATFORM_ANGLE_ANGLE, (void*) mainWindow.DC, null);
                    if (_eglDisplay == null)
                        throw new Exception("eglGetPlatformDisplayEXT failed.");
                }
                else if (OperatingSystem.IsLinux())
                {
                    var xDisplay = Clyde._windowing!.WindowGetX11Display(mainWindow.Reg)!.Value;
                    _eglDisplay = eglGetDisplay((void*) xDisplay);
                    if (mainWindow.EglSurface == (void*) EGL_NO_SURFACE)
                        throw new Exception("eglCreateWindowSurface failed.");
                }
                else
                {
                    throw new NotSupportedException("EGL is not currently supported outside Windows ANGLE or X11 Linux");
                }

                int major;
                int minor;
                if (eglInitialize(_eglDisplay, &major, &minor) == EGL_FALSE)
                    throw new Exception("eglInitialize failed.");

                var vendor = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_VENDOR));
                var version = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_VERSION));
                var extensions = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_EXTENSIONS));

                _sawmill.Debug("EGL initialized!");
                _sawmill.Debug($"EGL vendor: {vendor}!");
                _sawmill.Debug($"EGL version: {version}!");
                _sawmill.Debug($"EGL extensions: {extensions}!");

                if (eglBindAPI(EGL_OPENGL_ES_API) != EGL_TRUE)
                    throw new Exception("eglBindAPI failed.");

                var attribs = stackalloc int[]
                {
                    EGL_SURFACE_TYPE, EGL_WINDOW_BIT,
                    EGL_RED_SIZE, 8,
                    EGL_GREEN_SIZE, 8,
                    EGL_BLUE_SIZE, 8,
                    EGL_ALPHA_SIZE, 8,
                    EGL_STENCIL_SIZE, 8,
                    EGL_RENDERABLE_TYPE, EGL_OPENGL_ES3_BIT,
                    EGL_NONE
                };

                var numConfigs = 0;
                if (eglChooseConfig(_eglDisplay, attribs, null, 0, &numConfigs) == EGL_FALSE)
                    throw new Exception("eglChooseConfig failed.");

                var configs = stackalloc void*[numConfigs];
                if (eglChooseConfig(_eglDisplay, attribs, configs, numConfigs, &numConfigs) == EGL_FALSE)
                    throw new Exception("eglChooseConfig failed.");

                if (numConfigs == 0)
                    throw new Exception("No compatible EGL configurations returned!");

                _sawmill.Debug($"{numConfigs} EGL configs possible!");

                for (var i = 0; i < numConfigs; i++)
                {
                    _sawmill.Debug(DumpEglConfig(_eglDisplay, configs[i]));
                }

                _eglConfig = configs[0];

                var createAttribs = stackalloc int[]
                {
                    EGL_CONTEXT_CLIENT_VERSION, 3,
                    EGL_NONE
                };

                _eglContext = eglCreateContext(_eglDisplay, _eglConfig, null, createAttribs);
                if (_eglContext == (void*) EGL_NO_CONTEXT)
                    throw new Exception("eglCreateContext failed!");

                _sawmill.Debug("EGL context created!");
            }

            public override void Shutdown()
            {
                if (_eglDisplay != null)
                {
                    eglMakeCurrent(_eglDisplay, null, null, null);
                    eglTerminate(_eglDisplay);
                }
            }

            public override GLContextSpec[] SpecsToTry => Array.Empty<GLContextSpec>();
            public override bool RequireWindowGL => false;

            public override void SwapAllBuffers()
            {
                foreach (var data in _windowData.Values)
                {
                    eglSwapBuffers(_eglDisplay, data.EglSurface);
                }
            }

            public override void WindowResized(WindowReg reg, Vector2i oldSize)
            {
                // Nada..?
            }

            public override void* GetProcAddress(string name)
            {
                Span<byte> buf = stackalloc byte[128];
                var len = Encoding.UTF8.GetBytes(name, buf);
                buf[len] = 0;

                fixed (byte* ptr = &buf[0])
                {
                    return eglGetProcAddress(ptr);
                }
            }

            public override void BindWindowRenderTarget(WindowId rtWindowId)
            {
                var data = _windowData[rtWindowId];
                var result = eglMakeCurrent(_eglDisplay, data.EglSurface, data.EglSurface, _eglContext);
                if (result == EGL_FALSE)
                    throw new Exception("eglMakeCurrent failed.");
            }

            private static string DumpEglConfig(void* display, void* config)
            {
                var sb = new StringBuilder();

                sb.Append($"cfg: {Get(EGL_CONFIG_ID):00} | ");
                sb.AppendFormat(
                    "R/G/B/A/D/S: {0}/{1}/{2}/{3}/{4:00}/{5} | ",
                    Get(EGL_RED_SIZE), Get(EGL_GREEN_SIZE), Get(EGL_BLUE_SIZE), Get(EGL_ALPHA_SIZE),
                    Get(EGL_DEPTH_SIZE), Get(EGL_STENCIL_SIZE));

                // COLOR_BUFFER_TYPE
                sb.Append($"CBT: {Get(EGL_COLOR_BUFFER_TYPE)} | ");
                sb.Append($"CC: {Get(EGL_CONFIG_CAVEAT)} | ");
                sb.Append($"CONF: {Get(EGL_CONFORMANT)} | ");
                sb.Append($"NAT: {Get(EGL_NATIVE_VISUAL_ID)} | ");
                sb.Append($"SAMPLES: {Get(EGL_SAMPLES)} | ");
                sb.Append($"SAMPLE_BUFFERS: {Get(EGL_SAMPLE_BUFFERS)}");

                return sb.ToString();

                int Get(int attrib)
                {
                    int value;
                    if (eglGetConfigAttrib(display, config, attrib, &value) == EGL_FALSE)
                        throw new Exception("eglGetConfigAttrib failed!");

                    return value;
                }
            }

            private sealed class WindowData
            {
                public WindowReg Reg = default!;

                // ReSharper disable once InconsistentNaming
                // Windows DC for this window.
                // Only used for main window.
                public nint DC;

                public void* EglSurface;
            }
        }
    }
}
