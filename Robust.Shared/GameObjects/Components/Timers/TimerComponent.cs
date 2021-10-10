using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Exceptions;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timing.Timer;

namespace Robust.Shared.GameObjects
{
    public class TimerComponent : Component
    {
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

        public override string Name => "Timer";

        private readonly List<(Timer timer, CancellationToken source)>
            _timers = new();

        public int TimerCount => _timers.Count;

        /// <summary>
        /// Should this component be removed when no more timers are running?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool RemoveOnEmpty { get; set; } = true;

        public void Update(float frameTime)
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

            _timers.RemoveAll(timer => !timer.Item1.IsActive || timer.source.IsCancellationRequested);
        }

        public void AddTimer(Timer timer, CancellationToken cancellationToken = default)
        {
            _timers.Add((timer, cancellationToken));
        }

        /// <summary>
        ///     Creates a task that will complete after a given delay.
        ///     The task is resumed on the main game logic thread.
        /// </summary>
        /// <param name="milliseconds">The length of time, in milliseconds, to delay for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task that can be awaited.</returns>
        public Task Delay(int milliseconds, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<object?>();
            Spawn(milliseconds, () => tcs.SetResult(null), cancellationToken);
            return tcs.Task;
        }

        /// <summary>
        ///     Creates a task that will complete after a given delay.
        ///     The task is resumed on the main game logic thread.
        /// </summary>
        /// <param name="duration">The length of time to delay for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task that can be awaited.</returns>
        public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
        {
            return Delay((int) duration.TotalMilliseconds, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action to be fired after a certain delay.
        ///     The action will be resumed on the main game logic thread.
        /// </summary>
        /// <param name="milliseconds">The length of time, in milliseconds, to wait before firing the action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken"></param>
        public void Spawn(int milliseconds, Action onFired, CancellationToken cancellationToken = default)
        {
            var timer = new Timer(milliseconds, false, onFired);
            AddTimer(timer, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action to be fired after a certain delay.
        ///     The action will be resumed on the main game logic thread.
        /// </summary>
        /// <param name="duration">The length of time, to wait before firing the action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken"></param>
        public void Spawn(TimeSpan duration, Action onFired, CancellationToken cancellationToken = default)
        {
            Spawn((int) duration.TotalMilliseconds, onFired, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action that repeatedly fires after a delay specified in milliseconds.
        /// </summary>
        /// <param name="milliseconds">The length of time, in milliseconds, to delay before firing the repeated action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken">The CancellationToken for stopping the Timer.</param>
        public void SpawnRepeating(int milliseconds, Action onFired, CancellationToken cancellationToken)
        {
            var timer = new Timer(milliseconds, true, onFired);
            AddTimer(timer, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action that repeatedly fires after a delay.
        /// </summary>
        /// <param name="duration">The length of time to delay before firing the repeated action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken">The CancellationToken for stopping the Timer.</param>
        public void SpawnRepeating(TimeSpan duration, Action onFired, CancellationToken cancellationToken)
        {
            SpawnRepeating((int) duration.TotalMilliseconds, onFired, cancellationToken);
        }
    }
}
