using System;
using Robust.Shared.Threading;

namespace Robust.UnitTesting;

/// <summary>
/// Only allows 1 parallel process for testing purposes.
/// </summary>
public sealed class TestingParallelManager : IParallelManager
{
    public event Action? ParallelCountChanged;
    public int ParallelProcessCount => 1;
    public void AddAndInvokeParallelCountChanged(Action changed)
    {
        // Gottem
        return;
    }
}
