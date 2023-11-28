using System;
using System.Threading;
using Robust.Shared.Threading;

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

    WaitHandle IParallelManager.Process(IRobustJob job)
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

    public WaitHandle Process(IParallelRobustJob jobs, int amount)
    {
        throw new NotImplementedException();
    }
}
