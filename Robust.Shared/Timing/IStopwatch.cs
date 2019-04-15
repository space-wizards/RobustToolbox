using System;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     Provides a set of methods and properties that you can use to accurately
    ///     measure elapsed time.
    /// </summary>
    public interface IStopwatch
    {
        /// <summary>
        ///     Gets the total elapsed time measured by the current instance.
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        ///     Stops time interval measurement, resets the elapsed time to zero,
        ///     and starts measuring elapsed time.
        /// </summary>
        void Restart();

        /// <summary>
        ///     Starts, or resumes, measuring elapsed time for an interval.
        /// </summary>
        void Start();
    }
}
