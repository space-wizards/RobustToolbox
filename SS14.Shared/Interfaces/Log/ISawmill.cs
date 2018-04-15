using SS14.Shared.Log;

namespace SS14.Shared.Interfaces.Log
{
    public interface ISawmill
    {
        string Name { get; }

        LogLevel? Level { get; set; }

        void AddHandler(ILogHandler handler);
        void RemoveHandler(ILogHandler handler);

        /// <summary>
        ///     Log a message, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        void Log(string message, LogLevel level = LogLevel.Info, params object[] args);

        /// <summary>
        ///     Log a message as debug, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        void Debug(string message, params object[] args);

        /// <summary>
        ///     Log a message as info, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        void Info(string message, params object[] args);

        /// <summary>
        ///     Log a message as warning, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        void Warning(string message, params object[] args);

        /// <summary>
        ///     Log a message as error, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        void Error(string message, params object[] args);

        /// <summary>
        ///     Log a message as fatal, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        void Fatal(string message, params object[] args);

    }
}
