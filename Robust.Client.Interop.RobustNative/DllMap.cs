using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Client.Interop.RobustNative;


internal static class DllMap
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initializer()
    {
        if (Environment.GetEnvironmentVariable("ROBUST_NATIVE_PATH") is not { } nativePath)
            return;

        NativeLibrary.SetDllImportResolver(
            typeof(DllMap).Assembly,
            (_, _, _) => NativeLibrary.Load(nativePath));
    }
}
