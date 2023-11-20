using System;
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

    public void ProcessNow(IJobParallelFor job, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            job.Execute(i);
        }

        job.Finish();
    }

    public JobHandle Process(IJobParallelFor job, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            job.Execute(i);
        }

        job.Finish();
        return new JobHandle();
    }
}
