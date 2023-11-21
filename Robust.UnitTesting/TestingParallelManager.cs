using System;
using System.Collections.Generic;
using Robust.Shared.Threading;
using Schedulers;

namespace Robust.UnitTesting;

/// <summary>
/// Only allows 1 parallel process for testing purposes.
/// </summary>j
public sealed class TestingParallelManager : IParallelManager
{
    public event Action? ParallelCountChanged;
    public int ParallelProcessCount => 1;

    public void AddAndInvokeParallelCountChanged(Action changed)
    {
        // Gottem
        return;
    }

    public JobHandle Process(IRobustJob job)
    {
        throw new NotImplementedException();
    }

    public void ProcessNow(IParallelRobustJob jobs, int amount)
    {
        throw new NotImplementedException();
    }

    public void ProcessSerialNow(IParallelRobustJob jobs, int amount)
    {
        throw new NotImplementedException();
    }

    public JobHandle Process(IParallelRobustJob jobs, int amount)
    {
        throw new NotImplementedException();
    }

    public void Wait(IReadOnlyList<JobHandle> handles)
    {
        throw new NotImplementedException();
    }
}
