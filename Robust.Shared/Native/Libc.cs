using System;
using System.Runtime.InteropServices;

namespace Robust.Shared.Native;

internal static class Libc
{
    public const int RTLD_LAZY = 0x00001;
    public const int RTLD_NOW = 0x00002;
    public const int RTLD_BINDING_MASK = 0x3;
    public const int RTLD_NOLOAD = 0x00004;
    public const int RTLD_DEEPBIND = 0x00008;
    public const int RTLD_GLOBAL = 0x00100;
    public const int RTLD_LOCAL = 0;
    public const int RTLD_NODELETE = 0x01000;

    [DllImport("libdl.so.2")]
    public static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string name, int flags);
}
