using SS14.Shared.Interfaces.Log;
using SS14.Shared.IoC;
using System;
using System.IO;

namespace SS14.Shared.Log
{
    /// <summary>
    /// Static logging API front end. This is here as a convenience to prevent the need to resolve the manager manually.
    /// </summary>
    /// <remarks>
    /// This type is simply a proxy to an IoC based <see cref="ILogManager"/>.
    /// As such, no methods or properties will work if IoC has not been initialized yet.
    /// </remarks>
    /// <seealso cref="ILogManager"/>
    /// <seealso cref="LogManager"/>
    public static class Logger
    {
        /// <summary>
        /// The minimum log level of messages to allow them through.
        /// </summary>
        public static LogLevel CurrentLevel => Singleton.CurrentLevel;
        /// <summary>
        /// The instance we're using. As it's a direct proxy to IoC this will not work if IoC is not functional.
        /// </summary>
        private static ILogManager Singleton => IoCManager.Resolve<ILogManager>();

        /// <summary>
        /// Log a message, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        public static void Log(string message, LogLevel logLevel = LogLevel.Information, params object[] args)
        {
            Singleton.Log(message, logLevel, args);
        }

        /// <summary>
        /// Log a message as debug, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Debug(string message, params object[] args) => Singleton.Debug(message, args);

        /// <summary>
        /// Log a message as info, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Info(string message, params object[] args) => Singleton.Info(message, args);

        /// <summary>
        /// Log a message as warning, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Warning(string message, params object[] args) => Singleton.Warning(message, args);

        /// <summary>
        /// Log a message as error, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Error(string message, params object[] args) => Singleton.Error(message, args);

        /// <summary>
        /// Log a message as fatal, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="Log" />
        public static void Fatal(string message, params object[] args) => Singleton.Fatal(message, args);
    }
}
