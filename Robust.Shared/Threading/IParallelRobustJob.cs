using Robust.Shared.Log;

namespace Robust.Shared.Threading;

/// <summary>
/// Runs the job with the specified batch size per thread; Execute is still called per index.
/// </summary>
public interface IParallelRobustJob
{
    /// <summary>
    /// Minimum amount of batches required to engage in parallelism.
    /// </summary>
    public int MinimumBatchParallel => 2;

    /// <summary>
    /// Size of each batch per job.
    /// </summary>
    public virtual int BatchSize => 1;

    public void Execute(int index);
}
