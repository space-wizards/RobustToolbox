using Robust.Shared.Interfaces.Timers;
using System.Collections.Generic;
using System.Threading;
using Robust.Shared.Exceptions;
using Robust.Shared.IoC;

namespace Robust.Shared.Timers
{
    internal sealed class TimerManager : ITimerManager
    {
#pragma warning disable 649
        [Dependency] private readonly IRuntimeLog _runtimeLog;
#pragma warning restore 649

        private readonly List<(Timer, CancellationToken)> _timers
            = new List<(Timer, CancellationToken)>();

        public void AddTimer(Timer timer, CancellationToken cancellationToken = default)
        {
            _timers.Add((timer, cancellationToken));
        }

        public void UpdateTimers(float frameTime)
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

                timer.Update(frameTime, _runtimeLog);
            }

            _timers.RemoveAll(timer => !timer.Item1.IsActive || timer.Item2.IsCancellationRequested);
        }
    }
}
