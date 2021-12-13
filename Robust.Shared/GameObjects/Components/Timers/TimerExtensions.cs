using System;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.IoC;
using Timer = Robust.Shared.Timing.Timer;

namespace Robust.Shared.GameObjects
{
    public static class TimerExtensions
    {
        private static TimerComponent EnsureTimerComponent(this EntityUid entity)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            return entMan.EnsureComponent<TimerComponent>(entity);
        }

        public static void AddTimer(this EntityUid entity, Timer timer, CancellationToken cancellationToken = default)
        {
            entity
                .EnsureTimerComponent()
                .AddTimer(timer, cancellationToken);
        }

        /// <summary>
        ///     Creates a task that will complete after a given delay.
        ///     The task is resumed on the main game logic thread.
        /// </summary>
        /// <param name="entity">The entity to add the timer to.</param>
        /// <param name="milliseconds">The length of time, in milliseconds, to delay for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task that can be awaited.</returns>
        public static Task DelayTask(this EntityUid entity, int milliseconds, CancellationToken cancellationToken = default)
        {
            return entity
                .EnsureTimerComponent()
                .Delay(milliseconds, cancellationToken);
        }

        /// <summary>
        ///     Creates a task that will complete after a given delay.
        ///     The task is resumed on the main game logic thread.
        /// </summary>
        /// <param name="entity">The entity to add the timer to.</param>
        /// <param name="duration">The length of time to delay for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task that can be awaited.</returns>
        public static Task DelayTask(this EntityUid entity, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            return entity
                .EnsureTimerComponent()
                .Delay((int) duration.TotalMilliseconds, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action to be fired after a certain delay.
        ///     The action will be resumed on the main game logic thread.
        /// </summary>
        /// <param name="entity">The entity to add the timer to.</param>
        /// <param name="milliseconds">The length of time, in milliseconds, to wait before firing the action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken"></param>
        public static void SpawnTimer(this EntityUid entity, int milliseconds, Action onFired, CancellationToken cancellationToken = default)
        {
            entity
                .EnsureTimerComponent()
                .Spawn(milliseconds, onFired, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action to be fired after a certain delay.
        ///     The action will be resumed on the main game logic thread.
        /// </summary>
        /// <param name="entity">The entity to add the timer to.</param>
        /// <param name="duration">The length of time, to wait before firing the action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken"></param>
        public static void SpawnTimer(this EntityUid entity, TimeSpan duration, Action onFired, CancellationToken cancellationToken = default)
        {
            entity
                .EnsureTimerComponent()
                .Spawn((int) duration.TotalMilliseconds, onFired, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action that repeatedly fires after a delay specified in milliseconds.
        /// </summary>
        /// <param name="entity">The entity to add the timer to.</param>
        /// <param name="milliseconds">The length of time, in milliseconds, to delay before firing the repeated action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken">The CancellationToken for stopping the Timer.</param>
        public static void SpawnRepeatingTimer(this EntityUid entity, int milliseconds, Action onFired, CancellationToken cancellationToken)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            entity
                .EnsureTimerComponent()
                .SpawnRepeating(milliseconds, onFired, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action that repeatedly fires after a delay.
        /// </summary>
        /// <param name="entity">The entity to add the timer to.</param>
        /// <param name="duration">The length of time to delay before firing the repeated action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken">The CancellationToken for stopping the Timer.</param>
        public static void SpawnRepeatingTimer(this EntityUid entity, TimeSpan duration, Action onFired, CancellationToken cancellationToken)
        {
            entity
                .EnsureTimerComponent()
                .SpawnRepeating(duration, onFired, cancellationToken);
        }
    }
}
