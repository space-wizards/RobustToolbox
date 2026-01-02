using System.Threading;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     Manages <see cref="Timer"/>-based timing, allowing you to register new timers with optional cancellation.
    /// </summary>
    [NotContentImplementable]
    public interface ITimerManager
    {
        /// <summary>
        ///     Registers a timer with the manager, which will be executed on the main thread when its duration has
        ///     elapsed.
        /// </summary>
        /// <remarks>
        ///     Due to the granularity of the game simulation, the wait time for timers is will (effectively) round to
        ///     the nearest multiple of a tick, as they can only be processed on tick.
        /// </remarks>
        void AddTimer(Timer timer, CancellationToken cancellationToken = default);

        void UpdateTimers(FrameEventArgs frameEventArgs);
    }
}
