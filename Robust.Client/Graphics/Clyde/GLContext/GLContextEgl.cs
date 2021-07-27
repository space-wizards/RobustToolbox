using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using OpenToolkit;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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

            private void* _eglDisplay;
            private void* _eglContext;
            private void* _eglConfig;

            public GLContextEgl(Clyde clyde) : base(clyde)
            {
            }

            public override GLContextSpec? SpecWithOpenGLVersion(RendererOpenGLVersion version)
            {
                return null;
            }

            public override void UpdateVSync()
            {
                throw new System.NotImplementedException();
            }

            public override void WindowCreated(WindowReg reg)
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
                else
                {
                    throw new NotSupportedException("EGL is not currently supported outside Windows ANGLE");
                }

                if (reg.IsMainWindow)
                {
                    var result = eglMakeCurrent(_eglDisplay, data.EglSurface, data.EglSurface, _eglContext);
                    if (result == EGL_FALSE)
                        throw new Exception("eglMakeCurrent failed.");
                }

                var procName = Marshal.StringToCoTaskMemUTF8("glGetString");
                var getString = (delegate* unmanaged<int, byte*>) eglGetProcAddress((byte*) procName);
            }

            public override void WindowDestroyed(WindowReg reg)
            {
                throw new System.NotImplementedException();
            }

            private void Initialize(WindowData mainWindow)
            {
                if (OperatingSystem.IsWindows())
                {
                    // Setting up ANGLE without manually selecting a D3D11 device requires a windows DC.
                    mainWindow.DC = GetDC(Clyde._windowing!.WindowGetWin32Window(mainWindow.Reg)!.Value);

                    _eglDisplay = eglGetPlatformDisplayEXT(EGL_PLATFORM_ANGLE_ANGLE, (void*) mainWindow.DC, null);
                    if (_eglDisplay == null)
                        throw new Exception("eglGetPlatformDisplayEXT failed.");
                }
                else
                {
                    throw new NotSupportedException("EGL is not currently supported outside Windows ANGLE");
                }

                int major;
                int minor;
                if (eglInitialize(_eglDisplay, &major, &minor) == EGL_FALSE)
                    throw new Exception("eglInitialize failed.");

                var vendor = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_VENDOR));
                var version = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_VERSION));
                var extensions = Marshal.PtrToStringUTF8((nint) eglQueryString(_eglDisplay, EGL_EXTENSIONS));

                Logger.DebugS("clyde.ogl.egl", "EGL initialized!");
                Logger.DebugS("clyde.ogl.egl", $"EGL vendor: {vendor}!");
                Logger.DebugS("clyde.ogl.egl", $"EGL version: {version}!");
                Logger.DebugS("clyde.ogl.egl", $"EGL extensions: {extensions}!");

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

                Logger.DebugS("clyde.ogl.egl", $"{numConfigs} EGL configs possible!");

                for (var i = 0; i < numConfigs; i++)
                {
                    Logger.DebugS("clyde.ogl.egl", DumpEglConfig(_eglDisplay, configs[i]));
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

                Logger.DebugS("clyde.ogl.egl", "EGL context created!");
            }

            public override void Shutdown()
            {
                throw new System.NotImplementedException();
            }

            public override bool GlesOnly => true;

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
                // Only used for main window.
                public nint DC;

                public void* EglSurface;
            }


            [DllImport("user32.dll")]
            private static extern nint GetDC(nint hWnd);

            private sealed class EglBindingsContext : IBindingsContext
            {
                public IntPtr GetProcAddress(string procName)
                {
                    Span<byte> buf = stackalloc byte[128];
                    buf.Clear();
                    Encoding.UTF8.GetBytes(procName, buf);

                    fixed (byte* b = &buf.GetPinnableReference())
                    {
                        return (nint) eglGetProcAddress(b);
                    }
                }
            }
        }
    }
}
