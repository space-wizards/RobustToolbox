using System;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     Arguments of the GameLoop frame event.
    /// </summary>
    public readonly struct FrameEventArgs
    {
        /// <summary>
        ///     Seconds passed since this event was last called.
        /// </summary>
        /// <remarks>
        ///     Acceptable and simple to use for basic timing code, but accumulators/etc should prefer to use
        ///     <see cref="TimeSpan"/>s and <see cref="IGameTiming.CurTime"/> to avoid loss of precision.
        /// </remarks>
        public float DeltaSeconds { get; }

        /// <summary>
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="deltaSeconds">Seconds passed since this event was last called.</param>
        public FrameEventArgs(float deltaSeconds)
        {
            DeltaSeconds = deltaSeconds;
        }
    }
}
