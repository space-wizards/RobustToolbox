using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared;

/// <summary>
/// Logic for "warming up" things like slow static constructors concurrently.
/// </summary>
internal static class SharedWarmup
{
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void WarmupCore()
    {
        // Color's cctor unironically takes ~12ms.
        RuntimeHelpers.RunClassConstructor(typeof(Color).TypeHandle);

        // This ends up initializing prometheus-net which takes quite a while.
        RuntimeHelpers.RunClassConstructor(typeof(EntitySystemManager).TypeHandle);
    }
}
