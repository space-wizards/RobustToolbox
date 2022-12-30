using System.Threading.Tasks;
using Robust.Shared;

namespace Robust.Server;

/// <summary>
/// Logic for "warming up" things like slow static constructors concurrently.
/// </summary>
internal static class ServerWarmup
{
    public static void RunWarmup()
    {
        Task.Run(WarmupCore);
    }

    private static void WarmupCore()
    {
        SharedWarmup.WarmupCore();
    }
}
