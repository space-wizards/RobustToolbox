using System;
using System.Threading.Tasks;
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

    public Task Process(IRobustJob job)
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

    public Task[] Process(IParallelRobustJob jobs, int amount)
    {
        throw new NotImplementedException();
    }
}
