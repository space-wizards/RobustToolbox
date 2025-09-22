namespace Robust.Shared.Threading;

/// <summary>
/// Runs the job with the specified batch size per thread; Execute is still called per index.
/// </summary>
public interface IParallelRobustJob
{
    /// <summary>
    /// Minimum amount of batches required to engage in parallelism.
    /// </summary>
    int MinimumBatchParallel => 2;

    int BatchSize => 1;

    void Execute(int index);
}
