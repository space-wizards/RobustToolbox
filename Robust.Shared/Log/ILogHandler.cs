using Serilog.Events;

namespace Robust.Shared.Log
{
    /// <summary>
    ///     Formats and prints a log message to an output source.
    /// </summary>
    public interface ILogHandler
    {
        /// <summary>
        ///     Logs a message to.. somewhere.
        ///     You choose that somewhere.
        /// </summary>
        /// <remarks>
        ///     This method can be called from multiple threads so make sure it's thread safe!
        /// </remarks>
        /// <param name="sawmillName">The name of the sawmill that this message was raised on.</param>
        /// <param name="message">The message to log.</param>
        void Log(string sawmillName, LogEvent message);
    }
}
