using System;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Exceptions;
using Robust.Shared.IoC;

namespace Robust.Shared.Timing
{
    public class Timer
    {
        /// <summary>
        /// Counts the time (in milliseconds) before firing again.
        /// </summary>
        private int _timeCounter;

        /// <summary>
        /// Time (in milliseconds) between firings.
        /// </summary>
        public int Time { get; }

        /// <summary>
        /// Whether or not this timer should repeat.
        /// </summary>
        public bool IsRepeating { get; }

        /// <summary>
        /// Whether or not this timer is still running.
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// Called when the timer is fired.
        /// </summary>
        public Action OnFired { get; }

        public Timer(int milliseconds, bool isRepeating, Action onFired)
        {
            _timeCounter = Time = milliseconds;
            IsRepeating = isRepeating;
            OnFired = onFired;
        }

        // Parameter is used only on release.
        // ReSharper disable once UnusedParameter.Global
        public void Update(float frameTime, IRuntimeLog runtimeLog)
        {
            if (IsActive)
            {
                _timeCounter -= (int)(frameTime * 1000);

                if (_timeCounter <= 0)
                {
#if EXCEPTION_TOLERANCE
                    try
#endif
                    {
                        OnFired();
                    }
#if EXCEPTION_TOLERANCE
                    catch (Exception e)
                    {
                        runtimeLog.LogException(e, "Timer Callback");
                    }
#endif

                    if (IsRepeating)
                    {
                        _timeCounter += Time;
                    }
                    else
                    {
                        IsActive = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Creates a task that will complete after a given delay.
        ///     The task is resumed on the main game logic thread.
        /// </summary>
        /// <param name="milliseconds">The length of time, in milliseconds, to delay for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The task that can be awaited.</returns>
        public static Task Delay(int milliseconds, CancellationToken cancellationToken = default)
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
        public static Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
        {
            return Delay((int)duration.TotalMilliseconds, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action to be fired after a certain delay.
        ///     The action will be resumed on the main game logic thread.
        /// </summary>
        /// <param name="milliseconds">The length of time, in milliseconds, to wait before firing the action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken"></param>
        public static void Spawn(int milliseconds, Action onFired, CancellationToken cancellationToken = default)
        {
            var timer = new Timer(milliseconds, false, onFired);
            IoCManager.Resolve<ITimerManager>().AddTimer(timer, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action to be fired after a certain delay.
        ///     The action will be resumed on the main game logic thread.
        /// </summary>
        /// <param name="duration">The length of time, to wait before firing the action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken"></param>
        public static void Spawn(TimeSpan duration, Action onFired, CancellationToken cancellationToken = default)
        {
            Spawn((int)duration.TotalMilliseconds, onFired, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action that repeatedly fires after a delay specified in milliseconds.
        /// </summary>
        /// <param name="milliseconds">The length of time, in milliseconds, to delay before firing the repeated action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken">The CancellationToken for stopping the Timer.</param>
        public static void SpawnRepeating(int milliseconds, Action onFired, CancellationToken cancellationToken)
        {
            var timer = new Timer(milliseconds, true, onFired);
            IoCManager.Resolve<ITimerManager>().AddTimer(timer, cancellationToken);
        }

        /// <summary>
        ///     Schedule an action that repeatedly fires after a delay.
        /// </summary>
        /// <param name="duration">The length of time to delay before firing the repeated action.</param>
        /// <param name="onFired">The action to fire.</param>
        /// <param name="cancellationToken">The CancellationToken for stopping the Timer.</param>
        public static void SpawnRepeating(TimeSpan duration, Action onFired, CancellationToken cancellationToken)
        {
            SpawnRepeating((int)duration.TotalMilliseconds, onFired, cancellationToken);
        }
    }
}
