using System;
using System.Threading;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Threading;

public interface IParallelManager
{
    event Action ParallelCountChanged;

    int ParallelProcessCount { get; }

    /// <summary>
    /// Add the delegate to <see cref="ParallelCountChanged"/> and immediately invoke it.
    /// </summary>
    void AddAndInvokeParallelCountChanged(Action changed);

    /// <summary>
    /// Takes in a job that gets flushed.
    /// </summary>
    /// <param name="job"></param>
    WaitHandle Process<T>(T job) where T : RobustJob;

    public void ProcessNow<T>(T job) where T : RobustJob;

    /// <summary>
    /// Takes in a parallel job and runs it the specified amount.
    /// </summary>
    void ProcessNow<T>(T jobs, int amount) where T : ParallelRobustJob;

    /// <summary>
    /// Processes a robust job sequentially if desired.
    /// </summary>
    void ProcessSerialNow<T>(T job, int amount) where T : ParallelRobustJob;

    /// <summary>
    /// Takes in a parallel job and runs it without blocking.
    /// </summary>
    WaitHandle Process<T>(T jobs, int amount) where T : ParallelRobustJob;
}

internal interface IParallelManagerInternal : IParallelManager
{
    void Initialize();
}

internal sealed class ParallelManager : IParallelManagerInternal
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logs = default!;

    public event Action? ParallelCountChanged;
    public int ParallelProcessCount { get; private set; }

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logs.GetSawmill("parallel");
        _cfg.OnValueChanged(CVars.ThreadParallelCount, UpdateCVar, true);
    }

    public void AddAndInvokeParallelCountChanged(Action changed)
    {
        ParallelCountChanged += changed;
        changed();
    }

    private void UpdateCVar(int value)
    {
        var oldCount = ParallelProcessCount;
        ThreadPool.GetAvailableThreads(out var oldWorker, out var oldCompletion);
        ParallelProcessCount = value == 0 ? oldWorker : value;

        if (oldCount != ParallelProcessCount)
        {
            ParallelCountChanged?.Invoke();
            ThreadPool.SetMaxThreads(ParallelProcessCount, oldCompletion);
        }
    }

    /// <inheritdoc/>
    public WaitHandle Process<T>(T job) where T : RobustJob
    {
        job.Tracker.Event.Reset();

        // From what I can tell preferLocal is more of a !forceGlobal flag.
        // Also UnsafeQueue should be fine as long as we don't use async locals.
        ThreadPool.UnsafeQueueUserWorkItem(Execute, job, true);
        return job.Tracker.Event.WaitHandle;
    }

    private static void Execute(RobustJob job)
    {
        try
        {
            job.Execute();
        }
        catch (Exception exc)
        {
            job.Sawmill.Error($"Exception in ParallelManager: {exc.StackTrace}");
        }
        finally
        {
            job.Tracker.Event.Set();
        }
    }

    public void ProcessNow<T>(T job) where T : RobustJob
    {
        job.Execute();
    }

    /// <inheritdoc/>
    public void ProcessNow<T>(T job, int amount) where T : ParallelRobustJob
    {
        var batches = amount / (float) job.BatchSize;

        // Below the threshold so just do it now.
        if (batches <= job.MinimumBatchParallel)
        {
            ProcessSerialNow(job, amount);
            return;
        }

        var tracker = InternalProcess(job, amount);
        tracker.Event.WaitHandle.WaitOne();
        DebugTools.Assert(tracker.PendingTasks == 0);
    }

    /// <inheritdoc/>
    public void ProcessSerialNow<T>(T job, int amount) where T : ParallelRobustJob
    {
        // No point having threading overhead just slam it.
        for (var i = 0; i < amount; i++)
        {
            job.Execute(i);
        }
    }

    /// <inheritdoc/>
    public WaitHandle Process<T>(T job, int amount) where T : ParallelRobustJob
    {
        var tracker = InternalProcess(job, amount);
        return tracker.Event.WaitHandle;
    }

    /// <summary>
    /// Runs a parallel job internally. Used so we can pool the tracker task for ProcessParallelNow
    /// and not rely on external callers to return it where they don't want to wait.
    /// </summary>
    private ParallelTracker InternalProcess<T>(T job, int amount) where T : ParallelRobustJob
    {
        var batches = (int) MathF.Ceiling(amount / (float) job.BatchSize);
        var batchSize = job.BatchSize;

        // Need to set this up front to avoid firing too early.
        job.Tracker.Event.Reset();

        if (amount <= 0)
        {
            job.Tracker.Event.Set();
            return job.Tracker;
        }

        job.Tracker.PendingTasks = batches;

        for (var i = 0; i < batches; i++)
        {
            var start = i * batchSize;
            var end = Math.Min(start + batchSize, amount);
            var subJob = job.Clone();
            subJob.Start = start;
            subJob.End = end;

            // From what I can tell preferLocal is more of a !forceGlobal flag.
            // Also UnsafeQueue should be fine as long as we don't use async locals.
            ThreadPool.UnsafeQueueUserWorkItem(Execute, subJob, true);
        }

        return job.Tracker;
    }

    private static void Execute(ParallelRobustJob job)
    {
        var index = 0;

        try
        {
            for (index = job.Start; index < job.End; index++)
            {
                job.Execute(index);
            }
        }
        catch (Exception exc)
        {
            job.Sawmill.Error($"Exception in ParallelManager on job {index}: {exc.StackTrace}");
        }
        finally
        {
            job.Tracker.Set();
            job.Shutdown();
        }
    }
}

/// <summary>
/// Tracks jobs internally. This is because WaitHandle has a max limit of 64 tasks.
/// So we'll just decrement PendingTasks in lieu.
/// </summary>
internal sealed class ParallelTracker
{
    public readonly ManualResetEventSlim Event = new();
    public int PendingTasks;

    /// <summary>
    /// Marks the tracker as having 1 less pending task.
    /// </summary>
    public void Set()
    {
        // We should atomically get new value of PendingTasks
        // as the result of Decrement call and use it to prevent data race.
        if (Interlocked.Decrement(ref PendingTasks) <= 0)
            Event.Set();
    }
}
