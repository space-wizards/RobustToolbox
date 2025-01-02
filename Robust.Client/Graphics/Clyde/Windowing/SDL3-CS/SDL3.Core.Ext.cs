using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SDL3;

public static partial class SDL
{
    // Extensions to SDL3-CS that aren't part of the main library.

    [LibraryImport(nativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial void SDL_SetLogOutputFunction(delegate* unmanaged[Cdecl] <void*, int, SDL_LogPriority, byte*, void> callback, void* userdata);

    [LibraryImport(nativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial SDLBool SDL_AddEventWatch(delegate* unmanaged[Cdecl] <void*, SDL_Event*, byte> filter, void* userdata);

    [LibraryImport(nativeLibName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial void SDL_RemoveEventWatch(delegate* unmanaged[Cdecl] <void*, SDL_Event*, byte> filter, void* userdata);

    public const byte SDL_BUTTON_LEFT = 1;
    public const byte SDL_BUTTON_MIDDLE = 2;
    public const byte SDL_BUTTON_RIGHT = 3;
    public const byte SDL_BUTTON_X1 = 4;
    public const byte SDL_BUTTON_X2 = 5;

    public const int SDL_GL_CONTEXT_PROFILE_CORE = 0x0001;
    public const int SDL_GL_CONTEXT_PROFILE_COMPATIBILITY = 0x0002;
    public const int SDL_GL_CONTEXT_PROFILE_ES = 0x0004;

    public const int SDL_GL_CONTEXT_DEBUG_FLAG = 0x0001;
    public const int SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG = 0x0002;
    public const int SDL_GL_CONTEXT_ROBUST_ACCESS_FLAG = 0x0004;
    public const int SDL_GL_CONTEXT_RESET_ISOLATION_FLAG = 0x0008;

    public static int SDL_VERSIONNUM_MAJOR(int version) => version / 1000000;
    public static int SDL_VERSIONNUM_MINOR(int version) => version / 1000 % 1000;
    public static int SDL_VERSIONNUM_MICRO(int version) => version % 1000;
}
