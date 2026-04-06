using System;

namespace Robust.Shared.Timing
{
    /// <inheritdoc/>
    public sealed class Stopwatch : IStopwatch
    {
        private readonly System.Diagnostics.Stopwatch _stopwatch;

        /// <summary>
        ///     Constructs a new instance of this object.
        /// </summary>
        public Stopwatch()
        {
            _stopwatch = new System.Diagnostics.Stopwatch();
        }

        /// <inheritdoc/>
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        /// <inheritdoc/>
        public void Start()
        {
            _stopwatch.Start();
        }

        /// <inheritdoc/>
        public void Restart()
        {
            _stopwatch.Restart();
        }
    }
}
