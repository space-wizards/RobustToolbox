using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Threading;

// ReSharper disable once CheckNamespace
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

    public void ProcessNow(IParallelBulkRobustJob jobs, int amount)
    {
        jobs.ExecuteRange(0, amount);
    }

    public void ProcessSerialNow(IParallelBulkRobustJob jobs, int amount)
    {
        jobs.ExecuteRange(0, amount);
    }

    public WaitHandle Process(IParallelBulkRobustJob jobs, int amount)
    {
        ProcessSerialNow(jobs, amount);
        var ev = new ManualResetEventSlim();
        ev.Set();
        return ev.WaitHandle;
    }

    public void ProcessNow<TComp>(IParallelEnumerateEntitiesRobustJob<TComp> jobs) where TComp : IComponent
    {
        jobs.ExecuteRange(0, IoCManager.Instance?.Resolve<EntityManager>()?.DictionaryInternalCount<TComp>() ?? 0);
    }

    public void ProcessSerialNow<TComp>(IParallelEnumerateEntitiesRobustJob<TComp> jobs) where TComp : IComponent
    {
        jobs.ExecuteRange(0, IoCManager.Instance?.Resolve<EntityManager>()?.DictionaryInternalCount<TComp>() ?? 0);
    }

    public WaitHandle Process<TComp>(IParallelEnumerateEntitiesRobustJob<TComp> jobs) where TComp : IComponent
    {
        ProcessSerialNow(jobs);
        var ev = new ManualResetEventSlim();
        ev.Set();
        return ev.WaitHandle;
    }

    public void Initialize()
    {
    }
}
