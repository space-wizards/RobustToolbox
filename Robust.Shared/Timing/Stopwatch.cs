using System;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     Provides a set of methods and properties that you can use to accurately
    ///     measure elapsed time.
    /// </summary>
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

        /// <summary>
        ///     Gets the total elapsed time measured by the current instance.
        /// </summary>
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        /// <summary>
        ///     Starts, or resumes, measuring elapsed time for an interval.
        /// </summary>
        public void Start()
        {
            _stopwatch.Start();
        }

        /// <summary>
        ///     Stops time interval measurement, resets the elapsed time to zero,
        ///     and starts measuring elapsed time.
        /// </summary>
        public void Restart()
        {
            _stopwatch.Restart();
        }
    }
}
