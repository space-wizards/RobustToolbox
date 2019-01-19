using SS14.Shared.Log;

namespace SS14.Shared.Interfaces.Log
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
        /// <param name="message">The message to log.</param>
        void Log(in LogMessage message);
    }
}
