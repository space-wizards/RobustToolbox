namespace Robust.Shared.Threading;

/// <summary>
/// Helper for <see cref="IRobustJob"/>
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

