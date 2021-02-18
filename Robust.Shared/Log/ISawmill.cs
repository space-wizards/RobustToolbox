using System;

namespace Robust.Shared.Log
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
        ///     Log a message, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        void Log(LogLevel level, string message, params object?[] args);

        void Log(LogLevel level, Exception? exception, string message, params object?[] args);

        /// <summary>
        ///     Log a message.
        /// </summary>
        void Log(LogLevel level, string message);

        /// <summary>
        ///     Log a message as debug, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Debug(string message, params object?[] args);

        /// <summary>
        ///     Log a message as debug.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Debug(string message);

        /// <summary>
        ///     Log a message as info, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Info(string message, params object?[] args);

        /// <summary>
        ///     Log a message as info.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Info(string message);

        /// <summary>
        ///     Log a message as warning, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Warning(string message, params object?[] args);
        /// <summary>
        ///     Log a message as warning, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Warning(string message);

        /// <summary>
        ///     Log a message as error, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Error(string message, params object?[] args);

        /// <summary>
        ///     Log a message as error.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Error(string message);

        /// <summary>
        ///     Log a message as fatal, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Fatal(string message, params object?[] args);

        /// <summary>
        ///     Log a message as fatal.
        /// </summary>
        /// <seealso cref="Serilog.Log" />
        void Fatal(string message);
    }
}
