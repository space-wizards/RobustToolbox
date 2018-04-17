using SS14.Shared.Log;

namespace SS14.Shared.Interfaces.Log
{
    /// <summary>
    ///     A sawmill is an object-oriented logging "category".
    /// </summary>
    /// <seealso cref="ILogManager"/>
    public interface ISawmill
    {
        /// <summary>
        ///     The name of this sawmill. This determines its parent(s) and is printed with logs.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The level of messages to allow through. If null, the value from the parent is used.
        ///     If a value, a message's level must be greater or equal to the level to pass.
        /// </summary>
        LogLevel? Level { get; set; }

        /// <summary>
        ///     Adds a handler to handle incoming messages.
        /// </summary>
        /// <param name="handler">The handler to add.</param>
        void AddHandler(ILogHandler handler);

        /// <summary>
        ///     Adds a handler from handling incoming messages.
        /// </summary>
        /// <param name="handler">The haandler to remove.</param>
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
