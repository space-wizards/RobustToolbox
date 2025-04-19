using System.Threading;
using Robust.Shared.Log;

namespace Robust.Shared.Threading;

/// <summary>
/// Implement for code that needs to be runnable on a threadpool.
/// </summary>
public abstract class RobustJob
{
    internal readonly ParallelTracker Tracker = default!;

    internal readonly ISawmill Sawmill = default!;

    internal RobustJob() {}

    public abstract void Execute();
}
