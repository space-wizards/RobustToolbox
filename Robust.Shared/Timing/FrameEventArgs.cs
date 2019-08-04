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
