using System;
using System.Threading;
using Robust.Shared.Threading;

namespace Robust.UnitTesting;

/// <summary>
/// Only allows 1 parallel process for testing purposes.
/// </summary>j
public sealed class TestingParallelManager : IParallelManagerInternal
{
    public event Action? ParallelCountChanged { add { } remove { } }
    public int ParallelProcessCount => 1;

    public void AddAndInvokeParallelCountChanged(Action changed)
    {
        // Gottem
        return;
    }

    public WaitHandle Process<T>(T job) where T : IRobustJob
    {
        job.Execute();
        var ev = new ManualResetEventSlim();
        ev.Set();
        return ev.WaitHandle;
    }

    public void ProcessNow<T>(T job) where T: IRobustJob
    {
        job.Execute();
    }

    /// <inheritdoc/>
    public void ProcessNow<T>(T jobs, int amount) where T : IParallelRobustJob
    {
        for (var i = 0; i < amount; i++)
        {
            jobs.Execute(i);
        }
    }

    /// <inheritdoc/>
    public void ProcessSerialNow<T>(T job, int amount) where T : IParallelRobustJob
    {
        for (var i = 0; i < amount; i++)
        {
            job.Execute(i);
        }
    }

    /// <inheritdoc/>
    public WaitHandle Process<T>(T jobs, int amount) where T : IParallelRobustJob
    {
        ProcessSerialNow(jobs, amount);
        var ev = new ManualResetEventSlim();
        ev.Set();
        return ev.WaitHandle;
    }

    public void Initialize()
    {
    }
}
