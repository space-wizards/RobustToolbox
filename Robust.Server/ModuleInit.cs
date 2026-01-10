using System.Runtime.CompilerServices;
using Robust.Shared.Interop.RobustNative;

namespace Robust.Server;

internal static class ModuleInit
{
    [ModuleInitializer]
    public static void Initialize()
    {
        RobustNativeDll.IsServerProcess = true;
    }
}
