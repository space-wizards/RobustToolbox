// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace Robust.Shared.Interop.RobustNative;

internal static partial class Libc
{
    public const int RTLD_LAZY = 0x00001;
    public const int RTLD_NOW = 0x00002;
    public const int RTLD_BINDING_MASK = 0x3;
    public const int RTLD_NOLOAD = 0x00004;
    public const int RTLD_DEEPBIND = 0x00008;
    public const int RTLD_GLOBAL = 0x00100;
    public const int RTLD_LOCAL = 0;
    public const int RTLD_NODELETE = 0x01000;

    [LibraryImport("dl", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr dlopen(string name, int flags);
}
