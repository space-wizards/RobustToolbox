using System;
using JetBrains.Annotations;
using Robust.Shared.IoC;

namespace Robust.Shared.Log
{
    /// <summary>
    ///     Static logging API front end.
    ///     This is here as a convenience to prevent the need to resolve the manager manually.
    /// </summary>
    /// <remarks>
    ///     This type is simply a proxy to an IoC based <see cref="ILogManager"/> and its sawmills.
    ///     As such, no methods or properties will work if IoC has not been initialized yet.
    /// </remarks>
    /// <seealso cref="ILogManager"/>
    /// <seealso cref="ISawmill"/>
    public static class Logger
    {
        /// <summary>
        ///     The instance we're using.
        ///     As it's a direct proxy to IoC this will not work if IoC is not functional.
        /// </summary>
        // TODO: Maybe cache this to improve performance.
        private static ILogManager LogManagerSingleton => IoCManager.Resolve<ILogManager>();

        /// <summary>
        ///     Gets a sawmill by name. Equivalent to <see cref="ILogManager.GetSawmill(string)"/>.
        /// </summary>
        /// <param name="name">The name of the sawmill to get.</param>
        /// <returns>The sawmill with specified name. Creates a new one if it does not exist.</returns>
        public static ISawmill GetSawmill(string name)
        {
            return LogManagerSingleton.GetSawmill(name);
        }

        /// <summary>
        ///     Log a message, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        public static void LogS(LogLevel logLevel, string sawmillname, string message, params object?[] args)
        {
            var sawmill = LogManagerSingleton.GetSawmill(sawmillname);
            sawmill.Log(logLevel, message, args);
        }

        /// <summary>
        ///     Log a message, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        public static void LogS(LogLevel logLevel, string sawmillname, Exception? exception, string message, params object?[] args)
        {
            var sawmill = LogManagerSingleton.GetSawmill(sawmillname);
            sawmill.Log(logLevel, exception, message, args);
        }

        /// <summary>
        ///     Log a message.
        /// </summary>
        public static void LogS(LogLevel logLevel, string sawmillname, string message)
        {
            var sawmill = LogManagerSingleton.GetSawmill(sawmillname);
            sawmill.Log(logLevel, message);
        }

        /// <summary>
        /// Log a message, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        public static void Log(LogLevel logLevel, string message, params object?[] args)
        {
            LogManagerSingleton.RootSawmill.Log(logLevel, message, args);
        }

        public static void Log(LogLevel logLevel, Exception exception, string message, params object?[] args)
        {
            LogManagerSingleton.RootSawmill.Log(logLevel, message, args);
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        public static void Log(LogLevel logLevel, string message)
        {
            LogManagerSingleton.RootSawmill.Log(logLevel, message);
        }

        /// <summary>
        /// Log a message as debug, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void DebugS(string sawmill, string message, params object?[] args) => LogS(LogLevel.Debug, sawmill, message, args);

        /// <summary>
        /// Log a message as debug.
        /// </summary>
        /// <seealso cref="Log" />
        public static void DebugS(string sawmill, string message) => LogS(LogLevel.Debug, sawmill, message);

        /// <summary>
        /// Log a message as debug, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Debug(string message, params object?[] args) => Log(LogLevel.Debug, message, args);

        /// <summary>
        /// Log a message as debug.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Debug(string message) => Log(LogLevel.Debug, message);

        /// <summary>
        /// Log a message as info, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void InfoS(string sawmill, string message, params object?[] args) => LogS(LogLevel.Info, sawmill, message, args);

        /// <summary>
        /// Log a message as info.
        /// </summary>
        /// <seealso cref="Log" />
        public static void InfoS(string sawmill, string message) => LogS(LogLevel.Info, sawmill, message);

        /// <summary>
        /// Log a message as info, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Info(string message, params object?[] args) => Log(LogLevel.Info, message, args);

        /// <summary>
        /// Log a message as info.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Info(string message) => Log(LogLevel.Info, message);

        /// <summary>
        /// Log a message as warning, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void WarningS(string sawmill, string message, params object?[] args) => LogS(LogLevel.Warning, sawmill, message, args);

        /// <summary>
        /// Log a message as warning.
        /// </summary>
        /// <seealso cref="Log" />
        public static void WarningS(string sawmill, string message) => LogS(LogLevel.Warning, sawmill, message);

        /// <summary>
        /// Log a message as warning, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Warning(string message, params object?[] args) => Log(LogLevel.Warning, message, args);

        /// <summary>
        /// Log a message as warning.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Warning(string message) => Log(LogLevel.Warning, message);

        /// <summary>
        /// Log a message as error, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void ErrorS(string sawmill, string message, params object?[] args) => LogS(LogLevel.Error, sawmill, message, args);

        /// <summary>
        /// Log a message as error, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void ErrorS(string sawmill, Exception exception, string message, params object?[] args) => LogS(LogLevel.Error, sawmill, exception, message, args);

        /// <summary>
        /// Log a message as error.
        /// </summary>
        /// <seealso cref="Log" />
        public static void ErrorS(string sawmill, string message) => LogS(LogLevel.Error, sawmill, message);

        /// <summary>
        /// Log a message as error, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Error(string message, params object?[] args) => Log(LogLevel.Error, message, args);

        /// <summary>
        /// Log a message as error.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Error(string message) => Log(LogLevel.Error, message);

        /// <summary>
        /// Log a message as fatal, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void FatalS(string sawmill, string message, params object?[] args) => LogS(LogLevel.Fatal, sawmill, message, args);

        /// <summary>
        /// Log a message as fatal.
        /// </summary>
        /// <seealso cref="Log" />
        public static void FatalS(string sawmill, string message) => LogS(LogLevel.Fatal, sawmill, message);

        /// <summary>
        /// Log a message as fatal, taking in a format string and format list using the regular <see cref="Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Fatal(string message, params object?[] args) => Log(LogLevel.Fatal, message, args);

        /// <summary>
        /// Log a message as fatal.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Fatal(string message) => Log(LogLevel.Fatal, message);
    }
}
