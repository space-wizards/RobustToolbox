using System;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Threading;

public interface IParallelManager
{
    event Action ParallelCountChanged;

    int ParallelProcessCount { get; }

    /// <summary>
    /// Add the delegate to <see cref="ParallelCountChanged"/> and immediately invoke it.
    /// </summary>
    void AddAndInvokeParallelCountChanged(Action changed);
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

    public void Initialize()
    {
        _cfg.OnValueChanged(CVars.ThreadParallelCount, UpdateCVar, true);
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
            ParallelCountChanged?.Invoke();
    }
}

