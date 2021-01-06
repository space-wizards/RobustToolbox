#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        static Clyde()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                try
                {
                    // We force load nvapi64.dll so nvidia gives us the dedicated GPU on optimus laptops.
                    // This is 100x easier than nvidia's documented approach of NvOptimusEnablement,
                    // and works while developing.
                    NativeLibrary.Load("nvapi64.dll");
                }
                catch (Exception)
                {
                    // If this fails whatever.
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            NativeLibrary.SetDllImportResolver(typeof(GL).Assembly, (name, assembly, path) =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    && _dllMapLinux.TryGetValue(name, out var mappedName))
                {
                    return NativeLibrary.Load(mappedName);
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    && _dllMapMacOS.TryGetValue(name, out mappedName))
                {
                    return NativeLibrary.Load(mappedName);
                }

                return IntPtr.Zero;
            });
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static readonly Dictionary<string, string> _dllMapLinux = new()
        {
            {"opengl32.dll", "libGL.so.1"},
            {"glu32.dll", "libGLU.so.1"},
            {"openal32.dll", "libopenal.so.1"},
            {"alut.dll", "libalut.so.0"},
            {"opencl.dll", "libOpenCL.so"},
            {"libX11", "libX11.so.6"},
            {"libXi", "libXi.so.6"},
            {"SDL2.dll", "libSDL2-2.0.so.0"}
        };

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static readonly Dictionary<string, string> _dllMapMacOS = new()
        {
            {"opengl32.dll", "/System/Library/Frameworks/OpenGL.framework/OpenGL"},
            {"openal32.dll", "/System/Library/Frameworks/OpenAL.framework/OpenAL"},
            {"alut.dll", "/System/Library/Frameworks/OpenAL.framework/OpenAL"},
            {"libGLES.dll", "/System/Library/Frameworks/OpenGLES.framework/OpenGLES"},
            {"libGLESv1_CM.dll", "/System/Library/Frameworks/OpenGLES.framework/OpenGLES"},
            {"libGLESv2.dll", "/System/Library/Frameworks/OpenGLES.framework/OpenGLES"},
            {"opencl.dll", "/System/Library/Frameworks/OpenCL.framework/OpenCL"},
            {"SDL2.dll", "libSDL2.dylib"},
            {"libGL.so.1", "/usr/X11/lib/libGL.dylib"},
            {"libXcursor.so.1", "/usr/X11/lib/libXcursor.dylib"},
            {"libXinerama", "/usr/X11/lib/libXinerama.dylib"},
            {"libX11", "/usr/X11/lib/libX11.dylib"},
            {"libXrandr.so.2", "/usr/X11/lib/libXrandr.dylib"},
            {"libXi", "/usr/X11/lib/libXi.dylib"}
        };
    }
}
#endif
