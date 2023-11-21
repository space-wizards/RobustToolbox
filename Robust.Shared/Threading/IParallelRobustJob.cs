namespace Robust.Shared.Threading;

/// <summary>
/// Runs the job with the specified batch size per thread; Execute is still called per index.
/// </summary>
public interface IParallelRobustJob
{
    /// <summary>
    /// How many jobs to run in one batch on a thread.
    /// </summary>
    int BatchSize { get; }

    /// <summary>
    /// Called whenever the job is executed with the specified index to run.
    /// </summary>
    public void Execute(int index);
}

