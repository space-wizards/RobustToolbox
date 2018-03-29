using System;

namespace SS14.Shared.Timing
{
    /// <summary>
    ///     Arguments of the GameLoop frame event.
    /// </summary>
    public class FrameEventArgs : EventArgs
    {
        /// <summary>
        ///     Seconds passed since this event was last called.
        /// </summary>
        public float DeltaSeconds { get; protected set; }

        /// <summary>
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="deltaSeconds">Seconds passed since this event was last called.</param>
        public FrameEventArgs(float deltaSeconds)
        {
            DeltaSeconds = deltaSeconds;
        }
    }

    /// <summary>
    ///     A mutable version of <see cref="FrameEventArgs"/>.
    /// </summary>
    internal class MutableFrameEventArgs : FrameEventArgs
    {
        /// <summary>
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="deltaSeconds">Seconds passed since this event was last called.</param>
        public MutableFrameEventArgs(float deltaSeconds)
            : base(deltaSeconds) { }

        /// <summary>
        ///     Sets the seconds passed since this event was last called.
        /// </summary>
        public void SetDeltaSeconds(float seconds)
        {
            DeltaSeconds = seconds;
        }
    }
}
