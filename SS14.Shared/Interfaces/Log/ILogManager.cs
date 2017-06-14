using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Shared.Interfaces.Log
{
    /// <summary>
    /// Handles logging of messages on specific warning levels.
    /// Output method is dependent on implementation.
    /// </summary>
    public interface ILogManager : IIoCInterface
    {
        /// <summary>
        /// The minimum log level of messages to allow them through.
        /// </summary>
        LogLevel CurrentLevel { get; set; }

        /// <summary>
        /// Log a message, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        void Log(string message, LogLevel level = LogLevel.Information, params object[] args);

        /// <summary>
        /// Log a message as debug, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log.Log" />
        void Debug(string message, params object[] args);

        /// <summary>
        /// Log a message as info, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log.Log" />
        void Info(string message, params object[] args);

        /// <summary>
        /// Log a message as warning, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log.Log" />
        void Warning(string message, params object[] args);

        /// <summary>
        /// Log a message as error, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log.Log" />
        void Error(string message, params object[] args);

        /// <summary>
        /// Log a message as fatal, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log.Log" />
        void Fatal(string message, params object[] args);
    }
}
