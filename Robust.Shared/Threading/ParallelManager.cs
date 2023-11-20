using System;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Schedulers;

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
    JobHandle Process(IJob job);

    /// <summary>
    /// Takes in a parallel job and runs it the specified amount.
    /// </summary>
    void ProcessNow(IJobParallelFor jobs, int amount);

    /// <summary>
    /// Takes in a parallel job and runs it without blocking.
    /// </summary>
    JobHandle Process(IJobParallelFor jobs, int amount);

    void Wait(JobHandle handle);
}

internal interface IParallelManagerInternal : IParallelManager
{
    void Initialize();
}

internal sealed class ParallelManager : IParallelManagerInternal
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private JobScheduler _scheduler = default!;

    public event Action? ParallelCountChanged;
    public int ParallelProcessCount { get; private set; }

    public void Initialize()
    {
        _cfg.OnValueChanged(CVars.ThreadParallelCount, UpdateCVar, true);

        _scheduler = new JobScheduler(new JobScheduler.Config()
        {
            ThreadCount = ParallelProcessCount,
            // Keep in mind parallel jobs count as 1.
            MaxExpectedConcurrentJobs = Math.Max(32, ParallelProcessCount),
            StrictAllocationMode = false,
        });
    }

    public void AddAndInvokeParallelCountChanged(Action changed)
    {
        ParallelCountChanged += changed;
        changed();
    }

    public JobHandle Process(IJob job)
    {
        var handle = _scheduler.Schedule(job);
        _scheduler.Flush();
        return handle;
    }

    private void UpdateCVar(int value)
    {
        var oldCount = ParallelProcessCount;
        ParallelProcessCount = value == 0 ? Environment.ProcessorCount : value;

        if (oldCount != ParallelProcessCount)
            ParallelCountChanged?.Invoke();
    }

    public void ProcessNow(IJobParallelFor job, int amount)
    {
        var handle = _scheduler.Schedule(job, amount);
        _scheduler.Flush();
        handle.Complete();
    }

    public JobHandle Process(IJobParallelFor job, int amount)
    {
        var handle = _scheduler.Schedule(job, amount);
        _scheduler.Flush();
        return handle;
    }

    public void Wait(JobHandle handle)
    {
        handle.Complete();
    }
}

