using System.Collections.Generic;
using System.Threading;
using Robust.Shared.Exceptions;
using Robust.Shared.IoC;
using Timer = Robust.Shared.Timers.Timer;

namespace Robust.Shared.GameObjects.Components.Timers
{
    public class TimerComponent : Component
    {
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

        public override string Name => "Timer";

        private readonly List<(Timer timer, CancellationTokenSource? source)>
            _timers = new List<(Timer timer, CancellationTokenSource? source)>();

        public override void OnRemove()
        {
            foreach (var (timer, token) in _timers)
            {
                token?.Cancel();
            }
            
            _timers.Clear();

            base.OnRemove();
        }

        public void Update(float frameTime)
        {
            if (Owner.Paused)
            {
                return;
            }
            
            // Manual for loop so we can modify the list while enumerating.
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _timers.Count; i++)
            {
                var (timer, cancellationToken) = _timers[i];

                if (cancellationToken?.IsCancellationRequested ?? false)
                {
                    continue;
                }

                timer.Update(frameTime, _runtimeLog);
            }

            _timers.RemoveAll(timer => !timer.Item1.IsActive || (timer.source?.IsCancellationRequested ?? false));
        }
        
        public void AddTimer(Timer timer, CancellationTokenSource? cancellationToken = null)
        {
            _timers.Add((timer, cancellationToken));
        }
    }
}
