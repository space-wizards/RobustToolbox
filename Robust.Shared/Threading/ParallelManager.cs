using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;

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
    WaitHandle Process<T>(T job) where T : IRobustJob;

    public void ProcessNow<T>(T job) where T : IRobustJob;

    /// <summary>
    /// Takes in a parallel job and runs it the specified amount.
    /// </summary>
    void ProcessNow<T>(T jobs, int amount) where T : IParallelRobustJob;

    /// <summary>
    /// Processes a robust job sequentially if desired.
    /// </summary>
    void ProcessSerialNow<T>(T job, int amount) where T : IParallelRobustJob;

    /// <summary>
    /// Takes in a parallel job and runs it without blocking.
    /// </summary>
    WaitHandle Process<T>(T jobs, int amount) where T : IParallelRobustJob;
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

    /// <summary>
    /// Used internally for Parallel jobs, for external callers it gets garbage collected.
    /// </summary>
    private readonly ObjectPool<ParallelTracker> _trackerPool =
        new DefaultObjectPool<ParallelTracker>(new DefaultPooledObjectPolicy<ParallelTracker>(), 1024);

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
    public WaitHandle Process<T>(T job) where T : IRobustJob
    {
        var tracker = _trackerPool.Get();
        tracker.Event.Reset();
        var subJob = new InternalJob<T>(job, tracker, _sawmill);

        // From what I can tell preferLocal is more of a !forceGlobal flag.
        // Also UnsafeQueue should be fine as long as we don't use async locals.
        ThreadPool.UnsafeQueueUserWorkItem(subJob, true);
        return subJob.Tracker.Event.WaitHandle;
    }

    public void ProcessNow<T>(T job) where T : IRobustJob
    {
        job.Execute();
    }

    /// <inheritdoc/>
    public void ProcessNow<T>(T job, int amount) where T : IParallelRobustJob
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
        _trackerPool.Return(tracker);
    }

    /// <inheritdoc/>
    public void ProcessSerialNow<T>(T job, int amount) where T : IParallelRobustJob
    {
        // No point having threading overhead just slam it.
        for (var i = 0; i < amount; i++)
        {
            job.Execute(i);
        }
    }

    /// <inheritdoc/>
    public WaitHandle Process<T>(T job, int amount) where T : IParallelRobustJob
    {
        var tracker = InternalProcess(job, amount);
        return tracker.Event.WaitHandle;
    }

    /// <summary>
    /// Runs a parallel job internally. Used so we can pool the tracker task for ProcessParallelNow
    /// and not rely on external callers to return it where they don't want to wait.
    /// </summary>
    private ParallelTracker InternalProcess<T>(T job, int amount) where T : IParallelRobustJob
    {
        var batches = (int) MathF.Ceiling(amount / (float) job.BatchSize);
        var batchSize = job.BatchSize;
        var tracker = _trackerPool.Get();

        // Need to set this up front to avoid firing too early.
        tracker.Event.Reset();
        if (amount <= 0)
        {
            tracker.Event.Set();
            return tracker;
        }

        tracker.PendingTasks = batches;

        for (var i = 0; i < batches; i++)
        {
            var start = i * batchSize;
            var end = Math.Min(start + batchSize, amount);
            var subJob = new InternalParallelJob<T>(job, tracker, _sawmill, start, end);

            // From what I can tell preferLocal is more of a !forceGlobal flag.
            // Also UnsafeQueue should be fine as long as we don't use async locals.
            ThreadPool.UnsafeQueueUserWorkItem(subJob, true);
        }

        return tracker;
    }

    private readonly record struct InternalJob<T> : IThreadPoolWorkItem
        where T : IRobustJob
    {
        internal readonly ParallelTracker Tracker;

        private readonly T _job;
        private readonly ISawmill _sawmill;

        public InternalJob(T job, ParallelTracker tracker, ISawmill sawmill)
        {
            _job = job;
            Tracker = tracker;
            _sawmill = sawmill;
        }

        public void Execute()
        {
            try
            {
                _job.Execute();
            }
            catch (Exception exc)
            {
                _sawmill.Error($"Exception in ParallelManager: {exc.StackTrace}");
            }
            finally
            {
                Tracker.Event.Set();
            }
        }
    }

    private readonly record struct InternalParallelJob<T> : IThreadPoolWorkItem
        where T : IParallelRobustJob
    {
        internal readonly ParallelTracker _tracker;

        private readonly int _start;
        private readonly int _end;

        private readonly T _job;
        private readonly ISawmill _sawmill;

        public InternalParallelJob(T job, ParallelTracker tracker, ISawmill sawmill, int start, int end)
        {
            _job = job;
            _tracker = tracker;
            _sawmill = sawmill;
            _start = start;
            _end = end;
        }

        public void Execute()
        {
            for (var i = _start; i < _end; i++)
            {
                try
                {
                    _job.Execute(i);
                }
                catch (Exception exc)
                {
                    _sawmill.Error($"Exception in ParallelManager: {exc.StackTrace}");
                }
                finally
                {
                    _tracker.Event.Set();
                }
            }
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
