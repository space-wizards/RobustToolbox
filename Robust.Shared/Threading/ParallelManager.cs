using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    JobHandle Process(IRobustJob job);

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
    JobHandle Process(IParallelRobustJob jobs, int amount);

    /// <summary>
    /// Waits for the specified job handles to finish.
    /// </summary>
    void Wait(IReadOnlyList<JobHandle> handles);
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

    private JobScheduler _scheduler = default!;

    public void Initialize()
    {
        _cfg.OnValueChanged(CVars.ThreadParallelCount, UpdateCVar, true);

        _scheduler = new JobScheduler(new JobScheduler.Config()
        {
            ThreadCount = ParallelProcessCount,
        });
    }

    public void AddAndInvokeParallelCountChanged(Action changed)
    {
        ParallelCountChanged += changed;
        changed();
    }

    private void UpdateCVar(int value)
    {
        var oldCount = ParallelProcessCount;
        ParallelProcessCount = value == 0 ? Environment.ProcessorCount : value;

        if (oldCount != ParallelProcessCount)
        {
            ParallelCountChanged?.Invoke();

            // TODO: Need to do this
        }
    }

    public JobHandle Process(IRobustJob job)
    {
        var handle = _scheduler.Schedule(job);
        _scheduler.Flush();
        return handle;
    }

    public void ProcessNow(IParallelRobustJob job, int amount)
    {
        var handle = Process(job, amount);
        handle.Complete();
    }

    public void ProcessSerialNow(IParallelRobustJob jobs, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            jobs.Execute(i);
        }
    }

    public JobHandle Process(IParallelRobustJob job, int amount)
    {
        var handle = _scheduler.Schedule(job, amount);
        _scheduler.Flush();
        return handle;
    }

    public void Wait(IReadOnlyList<JobHandle> handles)
    {
        JobHandle.CompleteAll(handles);
    }
}

