using System;
using System.Threading;
using System.Threading.Tasks;
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
    Task Process(IRobustJob job);

    /// <summary>
    /// Takes in a parallel job and runs it the specified amount.
    /// </summary>
    void ProcessNow(IParallelRobustJob jobs, int amount);

    /// <summary>
    /// Takes in a parallel job and runs it without blocking.
    /// </summary>
    Task[] Process(IParallelRobustJob jobs, int amount);
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

    public void Initialize()
    {
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

    public Task Process(IRobustJob job)
    {
        // TODO: Does this have a race condition where it returns too early maybe?
        // I think it would be better with a TCS.
        var wrapper = new JobWrapper(job);
        ThreadPool.UnsafeQueueUserWorkItem(wrapper, false);
        return wrapper.Tcs.Task;
    }

    public void ProcessNow(IParallelRobustJob job, int amount)
    {
        var handles = Process(job, amount);
        Task.WaitAll(handles);
    }

    public Task[] Process(IParallelRobustJob job, int amount)
    {
        var batchSize = Math.Min(1, job.BatchSize);
        var batches = (int) Math.Ceiling(amount / (float) batchSize);
        var handles = new Task[batches];

        for (var i = 0; i < batches; i++)
        {
            var start = i * batchSize;
            var end = Math.Min(start + batchSize, amount);

            var wrapper = new ParallelJobWrapper(job, start, end);

            ThreadPool.UnsafeQueueUserWorkItem(wrapper, false);
            handles[i] = wrapper.Tcs.Task;
        }

        return handles;
    }

    /// <summary>
    /// Wraps the underlying IRobustJob so caller doesn't need to worry about handling the reset event.
    /// </summary>
    private readonly record struct JobWrapper : IThreadPoolWorkItem
    {
        private readonly IRobustJob _job;
        public readonly TaskCompletionSource Tcs = new();

        public JobWrapper(IRobustJob job)
        {
            _job = job;
        }

        public void Execute()
        {
            _job.Execute();
            Tcs.SetResult();
        }
    }

    /// <summary>
    /// Wraps an IParallelRobustJob and executes each item in a batch for the specified thread.
    /// </summary>
    private readonly record struct ParallelJobWrapper : IThreadPoolWorkItem
    {
        private readonly IParallelRobustJob _job;
        public readonly TaskCompletionSource Tcs = new();

        public readonly int Start;
        public readonly int End;

        public ParallelJobWrapper(IParallelRobustJob job, int start, int end)
        {
            _job = job;
            Start = start;
            End = end;
        }

        public void Execute()
        {
            for (var i = Start; i < End; i++)
            {
                _job.Execute(i);
            }

            Tcs.SetResult();
        }
    }
}

