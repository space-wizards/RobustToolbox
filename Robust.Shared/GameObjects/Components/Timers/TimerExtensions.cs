using System;
using System.Threading;
using System.Threading.Tasks;
using Timer = Robust.Shared.Timers.Timer;

namespace Robust.Shared.GameObjects
{
    public static class TimerExtensions
    {
        public static void AddTimer(this IEntity entity, Timer timer, CancellationToken cancellationToken = default)
        {
            entity
                .EnsureComponent<TimerComponent>()
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
        public static Task DelayTask(this IEntity entity, int milliseconds, CancellationToken cancellationToken = default)
        {
            return entity
                .EnsureComponent<TimerComponent>()
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
        public static Task DelayTask(this IEntity entity, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            return entity
                .EnsureComponent<TimerComponent>()
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
        public static void SpawnTimer(this IEntity entity, int milliseconds, Action onFired, CancellationToken cancellationToken = default)
        {
            entity
                .EnsureComponent<TimerComponent>()
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
        public static void SpawnTimer(this IEntity entity, TimeSpan duration, Action onFired, CancellationToken cancellationToken = default)
        {
            entity
                .EnsureComponent<TimerComponent>()
                .Spawn((int) duration.TotalMilliseconds, onFired, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action that repeatedly fires after a delay specified in milliseconds.
        /// </summary>
        /// <param name="entity">The entity to add the timer to.</param>
        /// <param name="milliseconds">The length of time, in milliseconds, to delay before firing the repeated action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken">The CancellationToken for stopping the Timer.</param>
        public static void SpawnRepeatingTimer(this IEntity entity, int milliseconds, Action onFired, CancellationToken cancellationToken)
        {
            entity
                .EnsureComponent<TimerComponent>()
                .SpawnRepeating(milliseconds, onFired, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action that repeatedly fires after a delay.
        /// </summary>
        /// <param name="entity">The entity to add the timer to.</param>
        /// <param name="duration">The length of time to delay before firing the repeated action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken">The CancellationToken for stopping the Timer.</param>
        public static void SpawnRepeatingTimer(this IEntity entity, TimeSpan duration, Action onFired, CancellationToken cancellationToken)
        {
            entity
                .EnsureComponent<TimerComponent>()
                .SpawnRepeating(duration, onFired, cancellationToken);
        }
    }
}
