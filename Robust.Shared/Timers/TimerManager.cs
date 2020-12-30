using Robust.Shared.Interfaces.Timers;
using System.Collections.Generic;
using System.Threading;
using Robust.Shared.Exceptions;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.Timers
{
    internal sealed class TimerManager : ITimerManager
    {
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

        private readonly List<(Timer, CancellationToken)> _timers
            = new();

        public void AddTimer(Timer timer, CancellationToken cancellationToken = default)
        {
            _timers.Add((timer, cancellationToken));
        }

        public void UpdateTimers(FrameEventArgs frameEventArgs)
        {
            // Manual for loop so we can modify the list while enumerating.
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _timers.Count; i++)
            {
                var (timer, cancellationToken) = _timers[i];

                if (cancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                timer.Update(frameEventArgs.DeltaSeconds, _runtimeLog);
            }

            _timers.RemoveAll(timer => !timer.Item1.IsActive || timer.Item2.IsCancellationRequested);
        }
    }
}
