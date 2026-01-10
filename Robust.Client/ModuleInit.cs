using System.Runtime.CompilerServices;
using Robust.Shared.Interop.RobustNative;

namespace Robust.Client;

internal static class ModuleInit
{
    [ModuleInitializer]
    public static void Initialize()
    {
        RobustNativeDll.IsClientProcess = true;
    }
}
