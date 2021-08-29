using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Robust.Client.Graphics.Clyde
{
    /// <summary>
    ///     Minimal ANGLE EGL API P/Invokes.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static unsafe class Egl
    {
        // ANGLE exports all the functions we need directly on the dll. No need to do eglGetProcAddress for EGL itself.
        // Still need it for OpenGL functions, however.

        private const string LibraryName = "libEGL.dll";

        public const int EGL_FALSE = 0;
        public const int EGL_TRUE = 1;

        public const int EGL_NONE = 0x3038;
        public const int EGL_VENDOR = 0x3053;
        public const int EGL_VERSION = 0x3054;
        public const int EGL_EXTENSIONS = 0x3055;
        public const int EGL_CONFIG_ID = 0x3028;
        public const int EGL_COLOR_BUFFER_TYPE = 0x303F;
        public const int EGL_SAMPLES = 0x3031;
        public const int EGL_SAMPLE_BUFFERS = 0x3032;
        public const int EGL_CONFIG_CAVEAT = 0x3027;
        public const int EGL_CONFORMANT = 0x3042;
        public const int EGL_NATIVE_VISUAL_ID = 0x302E;
        public const int EGL_SURFACE_TYPE = 0x3033;
        public const int EGL_ALPHA_SIZE = 0x3021;
        public const int EGL_BLUE_SIZE = 0x3022;
        public const int EGL_GREEN_SIZE = 0x3023;
        public const int EGL_RED_SIZE = 0x3024;
        public const int EGL_DEPTH_SIZE = 0x3025;
        public const int EGL_STENCIL_SIZE = 0x3026;
        public const int EGL_WINDOW_BIT = 0x0004;
        public const int EGL_OPENGL_ES_API = 0x30A0;
        public const int EGL_RENDERABLE_TYPE = 0x3040;
        public const int EGL_OPENGL_ES3_BIT = 0x00000040;
        public const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
        public const int EGL_TEXTURE_FORMAT = 0x3080;
        public const int EGL_TEXTURE_RGBA = 0x305E;
        public const int EGL_TEXTURE_TARGET = 0x3081;
        public const int EGL_TEXTURE_2D = 0x305F;
        public const int EGL_GL_COLORSPACE = 0x309D;
        public const int EGL_GL_COLORSPACE_SRGB = 0x3089;
        public const int EGL_PLATFORM_ANGLE_ANGLE = 0x3202;

        public const nint EGL_NO_CONTEXT = 0;
        public const nint EGL_NO_DEVICE_EXT = 0;
        public const nint EGL_NO_SURFACE = 0;

        public const int EGL_D3D_TEXTURE_ANGLE = 0x33A3;
        public const int EGL_D3D11_DEVICE_ANGLE = 0x33A1;
        public const int EGL_PLATFORM_DEVICE_EXT = 0x313F;
        public const int EGL_OPTIMAL_SURFACE_ORIENTATION_ANGLE = 0x33A7;

        [DllImport(LibraryName)]
        public static extern int eglInitialize(void* display, int* major, int* minor);

        [DllImport(LibraryName)]
        public static extern int eglTerminate(void* display);

        [DllImport(LibraryName)]
        public static extern void* eglGetDisplay(void* display);

        [DllImport(LibraryName)]
        public static extern void* eglGetPlatformDisplayEXT(int platform, void* native_display, nint* attrib_list);

        [DllImport(LibraryName)]
        public static extern int eglBindAPI(int api);

        [DllImport(LibraryName)]
        public static extern void* eglCreateDeviceANGLE(int device_type, void* native_device, nint* attrib_list);

        [DllImport(LibraryName)]
        public static extern int eglReleaseDeviceANGLE(void* device);

        [DllImport(LibraryName)]
        public static extern void* eglGetProcAddress(byte* procname);

        [DllImport(LibraryName)]
        public static extern int eglChooseConfig(
            void* display,
            int* attrib_list,
            void** configs,
            int config_size,
            int* num_config);

        [DllImport(LibraryName)]
        public static extern int eglGetConfigAttrib(void* display, void* config, int attribute, int* value);

        [DllImport(LibraryName)]
        public static extern void* eglCreateContext(void* display, void* config, void* share_context, int* attrib_list);

        [DllImport(LibraryName)]
        public static extern int eglGetError();

        [DllImport(LibraryName)]
        public static extern byte* eglQueryString(void* display, int name);

        [DllImport(LibraryName)]
        public static extern void* eglCreatePbufferFromClientBuffer(
            void* display,
            int buftype,
            void* buffer,
            void* config,
            int* attrib_list);

        [DllImport(LibraryName)]
        public static extern void* eglCreateWindowSurface(
            void* display,
            void* config,
            void* native_window,
            int* attrib_list);

        [DllImport(LibraryName)]
        public static extern int eglMakeCurrent(
            void* display,
            void* draw,
            void* read,
            void* context);

        [DllImport(LibraryName)]
        public static extern void* eglGetCurrentContext();

        [DllImport(LibraryName)]
        public static extern int eglSwapBuffers(void* display, void* surface);

        [DllImport(LibraryName)]
        public static extern int eglDestroySurface(void* display, void* surface);
    }
}
