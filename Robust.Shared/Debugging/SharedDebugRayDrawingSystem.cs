using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Robust.Shared.Debugging;

public abstract class SharedDebugRayDrawingSystem : EntitySystem
{
#if DEBUG
    private ConcurrentBag<DebugRayData> _rayDataThreadShuntBag = new();

    public override void FrameUpdate(float frameTime)
    {
        Process();
    }

    public override void Update(float frameTime)
    {
        Process();
    }

    private void Process()
    {
        if (_rayDataThreadShuntBag.Count == 0)
            return;

        var arr = _rayDataThreadShuntBag.ToArray();
        _rayDataThreadShuntBag.Clear();

        foreach (var drd in arr)
        {
            ReceiveLocalRayAtMainThread(drd);
        }
    }

    /// <summary>
    /// Receives locally sourced ray debug information.
    /// This may be called on any thread.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReceiveLocalRayFromAnyThread(DebugRayData drd)
    {
        // If not on DEBUG, we're not going to use this anyway.
        // Let the inlining DCE this away.
        _rayDataThreadShuntBag.Add(drd);
    }

    /// <summary>
    /// Receives locally sourced ray debug information.
    /// This is only called at the main thread.
    /// Note that on release builds (!DEBUG), this function is never called.
    /// </summary>
    protected abstract void ReceiveLocalRayAtMainThread(DebugRayData drd);

    public readonly record struct DebugRayData(Ray Ray, float MaxLength, RayCastResults? Results, bool ServerSide, MapId Map);
#endif
}
