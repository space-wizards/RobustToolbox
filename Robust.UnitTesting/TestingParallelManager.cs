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

    WaitHandle IParallelManager.Process(IRobustJob job)
    {
        job.Execute();
        var ev = new ManualResetEventSlim();
        ev.Set();
        return ev.WaitHandle;
    }

    public void ProcessNow(IRobustJob job)
    {
        job.Execute();
    }

    /// <inheritdoc/>
    public void ProcessNow(IParallelRobustJob jobs, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            jobs.Execute(i);
        }
    }

    /// <inheritdoc/>
    public void ProcessSerialNow(IParallelRobustJob jobs, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            jobs.Execute(i);
        }
    }

    /// <inheritdoc/>
    public WaitHandle Process(IParallelRobustJob jobs, int amount)
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
