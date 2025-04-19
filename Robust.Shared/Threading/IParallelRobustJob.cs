using Robust.Shared.Log;

namespace Robust.Shared.Threading;

/// <summary>
/// Runs the job with the specified batch size per thread; Execute is still called per index.
/// </summary>
public abstract class ParallelRobustJob
{
    /// <summary>
    /// Minimum amount of batches required to engage in parallelism.
    /// </summary>
    public virtual int MinimumBatchParallel => 2;

    /// <summary>
    /// Size of each batch per job.
    /// </summary>
    public virtual int BatchSize => 1;

    internal readonly ParallelTracker Tracker = default!;
    internal readonly ISawmill Sawmill = default!;

    internal int Start;
    internal int End;

    internal ParallelRobustJob()
    {

    }

    public abstract void Execute(int index);

    public abstract ParallelRobustJob Clone();

    /// <summary>
    /// Run on SubJobs after a parallel job has run.
    /// </summary>
    public virtual void Shutdown()
    {

    }
}
