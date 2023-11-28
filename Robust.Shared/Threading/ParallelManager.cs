using System;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;

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
    WaitHandle Process(IRobustJob job);

    /// <summary>
    /// Takes in a parallel job and runs it the specified amount.
    /// </summary>
    void ProcessNow(IParallelRobustJob jobs, int amount);

    /// <summary>
    /// Processes a robust job sequentially if desired.
    /// </summary>
    void ProcessSerialNow(IParallelRobustJob jobs, int amount);

    /// <summary>
    /// Takes in a parallel job and runs it without blocking.
    /// </summary>
    WaitHandle Process(IParallelRobustJob jobs, int amount);
}

internal interface IParallelManagerInternal : IParallelManager
{
    void Initialize();
}

internal sealed class ParallelManager : IParallelManagerInternal
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public event Action? ParallelCountChanged;
    public int ParallelProcessCount { get; private set; }

    // Without pooling it's hard to keep task allocations down for classes
    // This lets us avoid re-allocating the ManualResetEventSlims constantly when we just need a way to signal job completion.

    private readonly ObjectPool<InternalJob> _jobPool =
        new DefaultObjectPool<InternalJob>(new DefaultPooledObjectPolicy<InternalJob>(), 256);

    private readonly ObjectPool<InternalParallelJob> _parallelPool =
        new DefaultObjectPool<InternalParallelJob>(new DefaultPooledObjectPolicy<InternalParallelJob>(), 256);

    /// <summary>
    /// Used internally for Parallel jobs, for external callers it gets garbage collected.
    /// </summary>
    private readonly ObjectPool<ParallelTracker> _trackerPool =
        new DefaultObjectPool<ParallelTracker>(new DefaultPooledObjectPolicy<ParallelTracker>());

    public void Initialize()
    {
        _cfg.OnValueChanged(CVars.ThreadParallelCount, UpdateCVar, true);
    }

    public void AddAndInvokeParallelCountChanged(Action changed)
    {
        ParallelCountChanged += changed;
        changed();
    }

    private InternalJob GetJob(IRobustJob job)
    {
        var robustJob = _jobPool.Get();
        robustJob.Event.Reset();
        robustJob.Set(job, _jobPool);
        return robustJob;
    }

    private InternalParallelJob GetParallelJob(IParallelRobustJob job, int start, int end, ParallelTracker tracker)
    {
        var internalJob = _parallelPool.Get();
        internalJob.Set(job, start, end, tracker, _parallelPool);
        return internalJob;
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

    public WaitHandle Process(IRobustJob job)
    {
        var subJob = GetJob(job);
        ThreadPool.UnsafeQueueUserWorkItem(subJob, true);
        return subJob.Event.WaitHandle;
    }

    public void ProcessNow(IParallelRobustJob job, int amount)
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

    public void ProcessSerialNow(IParallelRobustJob jobs, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            jobs.Execute(i);
        }
    }

    public WaitHandle Process(IParallelRobustJob job, int amount)
    {
        var tracker = InternalProcess(job, amount);
        return tracker.Event.WaitHandle;
    }

    private ParallelTracker InternalProcess(IParallelRobustJob job, int amount)
    {
        var batches = (int) MathF.Ceiling(amount / (float) job.BatchSize);
        var batchSize = job.BatchSize;
        var tracker = _trackerPool.Get();

        // Need to set this up front to avoid firing too early.
        tracker.Event.Reset();
        tracker.PendingTasks = batches;

        for (var i = 0; i < batches; i++)
        {
            var start = i * batchSize;
            var end = Math.Min(start + batchSize, amount);
            var subJob = GetParallelJob(job, start, end, tracker);
            ThreadPool.UnsafeQueueUserWorkItem(subJob, true);
        }

        return tracker;
    }

    #region Jobs

    private sealed class InternalJob : IRobustJob
    {
        private IRobustJob _robust = default!;

        public readonly ManualResetEventSlim Event = new();
        private ObjectPool<InternalJob> _parentPool = default!;

        public void Set(IRobustJob job, ObjectPool<InternalJob> parentPool)
        {
            _robust = job;
            _parentPool = parentPool;
        }

        public void Execute()
        {
            _robust.Execute();
            Event.Set();
            _parentPool.Return(this);
        }
    }

    private sealed class InternalParallelJob : IRobustJob
    {
        private IParallelRobustJob _robust = default!;
        private int _start;
        private int _end;

        private ParallelTracker _tracker = default!;
        private ObjectPool<InternalParallelJob> _parentPool = default!;

        public void Set(IParallelRobustJob robust, int start, int end, ParallelTracker tracker, ObjectPool<InternalParallelJob> parentPool)
        {
            _robust = robust;
            _start = start;
            _end = end;

            _tracker = tracker;
            _parentPool = parentPool;
        }

        public void Execute()
        {
            for (var i = _start; i < _end; i++)
            {
                _robust.Execute(i);
            }

            // Set the event and return it to the pool for re-use.
            _tracker.Set();
            _parentPool.Return(this);
        }
    }

    private sealed class ParallelTracker
    {
        public readonly ManualResetEventSlim Event = new();
        public int PendingTasks;

        /// <summary>
        /// Marks the tracker as having 1 less pending task.
        /// </summary>
        public void Set()
        {
            Interlocked.Decrement(ref PendingTasks);

            if (PendingTasks <= 0)
                Event.Set();
        }
    }

    #endregion
}

