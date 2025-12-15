using System;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     Provides a set of methods and properties that you can use to accurately
    ///     measure elapsed time.<br/>
    ///     <br/>
    ///     This is legacy and of low utility (it's for mocking), prefer using <see cref="RStopwatch"/>.
    /// </summary>
    /// <seealso cref="Stopwatch"/>
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
