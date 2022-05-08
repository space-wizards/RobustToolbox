using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Robust.Shared.Debugging;

[Virtual]
public abstract class SharedDebugRayDrawingSystem : EntitySystem
{
#if DEBUG
    private ConcurrentBag<DebugRayData> _rayDataThreadShuntBag = new();
#endif

    public override void FrameUpdate(float frameTime)
    {
#if DEBUG
        // Pull rays into main thread for distribution.
        var arr = _rayDataThreadShuntBag.ToArray();
        _rayDataThreadShuntBag.Clear();
        foreach (var drd in arr)
        {
            ReceiveLocalRayAtMainThread(drd);
        }
#endif
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
#if DEBUG
        _rayDataThreadShuntBag.Add(drd);
#endif
    }

    /// <summary>
    /// Receives locally sourced ray debug information.
    /// This is only called at the main thread.
    /// Note that on release builds (!DEBUG), this function is never called.
    /// </summary>
    protected abstract void ReceiveLocalRayAtMainThread(DebugRayData drd);
}

