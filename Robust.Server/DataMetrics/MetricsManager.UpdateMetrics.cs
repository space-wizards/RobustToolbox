using System;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Server.DataMetrics;

internal sealed partial class MetricsManager
{
    //
    // Handles the implementation of the "UpdateMetrics" callback.
    //

    public event Action? UpdateMetrics;

    private TimeSpan _fixedUpdateInterval;
    private TimeSpan _nextFixedUpdate;

    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private void InitializeUpdateMetrics()
    {
        _cfg.OnValueChanged(
            CVars.MetricsUpdateInterval,
            seconds =>
            {
                _fixedUpdateInterval = TimeSpan.FromSeconds(seconds);
                _nextFixedUpdate = _gameTiming.RealTime + _fixedUpdateInterval;
            },
            true);
    }

    public void FrameUpdate()
    {
        if (_fixedUpdateInterval == TimeSpan.Zero)
            return;

        var time = _gameTiming.RealTime;

        if (_nextFixedUpdate > time)
            return;

        _nextFixedUpdate = time + _fixedUpdateInterval;

        _sawmill.Verbose("Running fixed metrics update");
        UpdateMetrics?.Invoke();
    }

    private async Task BeforeCollectCallback(CancellationToken cancel)
    {
        if (UpdateMetrics == null)
            return;

        await _taskManager.TaskOnMainThread(() =>
        {
            UpdateMetrics?.Invoke();
        });
    }
}
