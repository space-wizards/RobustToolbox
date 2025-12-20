namespace Robust.Shared.Threading;

/// <summary>
/// Represents a generic parallel job that processes a range of indices.
/// </summary>
public interface IParallelRangeRobustJob
{
    /// <summary>
    /// Minimum amount of batches required to engage in parallelism.
    /// If the total number of batches is less than this, the job will run serially.
    /// </summary>
    int MinimumBatchParallel => 2;

    /// <summary>
    /// The amount of elements to process in each batch.
    /// </summary>
    int BatchSize => 1;

    /// <summary>
    /// Processes a range of indices from startIndex to endIndex.
    /// </summary>
    /// <param name="startIndex">The starting index of the range.</param>
    /// <param name="endIndex">The ending index of the range.</param>
    void ExecuteRange(int startIndex, int endIndex);
}

/// <summary>
/// Represents a parallel job that processes individual indices.
/// </summary>
public interface IParallelRobustJob : IParallelRangeRobustJob
{
    /// <summary>
    /// Default implementation that executes the job for each index in the specified range.
    /// </summary>
    void IParallelRangeRobustJob.ExecuteRange(int startIndex, int endIndex)
    {
        for (var i = startIndex; i < endIndex; i++)
        {
            Execute(i);
        }
    }

    /// <summary>
    /// Executes the job for the specified index.
    /// </summary>
    /// <param name="index">The index to process.</param>
    void Execute(int index);
}

/// <summary>
/// Represents a parallel job that processes a bulk range of indices.
/// Good for jobs that can operate on ranges more efficiently (SIMD) than individual indices.
/// </summary>
public interface IParallelBulkRobustJob : IParallelRangeRobustJob;
