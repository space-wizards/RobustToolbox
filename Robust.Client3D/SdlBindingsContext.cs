using System;
using OpenToolkit;
using SDL3;

namespace Robust.Client3D;

internal sealed class SdlBindingsContext : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
    {
        return SDL.SDL_GL_GetProcAddress(procName);
    }
}
