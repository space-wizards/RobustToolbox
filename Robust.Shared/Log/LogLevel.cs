using Serilog.Events;

namespace Robust.Shared.Log
{
    /// <remarks>
    ///     The value associated with the level determines the order in which they are filtered,
    ///     Under the default <see cref="LogManager"/>.
    /// </remarks>
    public enum LogLevel
    {
        /// <summary>
        ///     When you're *really* trying to track down that bug.
        /// </summary>
        Verbose = LogEventLevel.Verbose,

        /// <summary>
        ///     Diagnostic information usually only necessary when something broke.
        /// </summary>
        Debug = LogEventLevel.Debug,

        /// <summary>
        ///     General info that can confirm that something is working.
        /// </summary>
        Info = LogEventLevel.Information,

        /// <summary>
        ///     Issues that can easily be worked around but should still be fixed.
        /// </summary>
        Warning = LogEventLevel.Warning,

        /// <summary>
        ///     Errors that need fixing and are probably gonna break something.
        /// </summary>
        Error = LogEventLevel.Error,

        /// <summary>
        ///     Errors that are REALLY BAD and break EVERYTHING.
        /// </summary>
        Fatal = LogEventLevel.Fatal
    }
}
