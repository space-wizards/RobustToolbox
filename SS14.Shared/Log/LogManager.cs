using SS14.Shared.ServerEnums;
using System;
using System.IO;

namespace SS14.Shared.Log
{
    public class LogManager
    {
        /// <summary>
        /// Singleton
        /// </summary>
        private static LogManager singleton;

        private LogLevel currentLogLevel;
        private StreamWriter logStream;

        public string LogPath { get; set; }

        public static LogManager Singleton
        {
            get
            {
                if (singleton == null)
                    throw new TypeInitializationException("LogManager Not Initialized.", null);
                return singleton;
            }
        }

        /// <summary>
        /// Initialize log
        /// </summary>
        /// <param name="_logPath"></param>
        public static void Initialize(string _logPath, LogLevel logLevel = LogLevel.Information)
        {
            singleton = new LogManager();
            singleton.LogPath = _logPath;
            singleton.currentLogLevel = logLevel;
            singleton.Start();
            Log("Logging at level: " + logLevel.ToString());
        }

        /// <summary>
        /// Start logging, open the log file etc.
        /// </summary>
        public void Start()
        {
            try
            {
                if (!Directory.Exists(LogPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                }

                logStream = new StreamWriter(LogPath, true);
            }
            catch (IOException)
            {
                Console.WriteLine("Log file ('{0}') in use, unable to open file for logging.", LogPath);
                Environment.Exit(1);
            }

            logStream.AutoFlush = true;
            LogOne("LogManager started.", LogLevel.Information);
        }

        /// <summary>
        /// Log dat shit
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="logLevel"></param>
        private void LogOne(string Message, LogLevel logLevel)
        {
            if (singleton == null)
                throw new TypeInitializationException("LogManager Not Initialized.", null);

            if ((int)logLevel >= (int)singleton.currentLogLevel)
            {
                string logType = logLevel.ToString();
                if (logType == "Information")
                    logType = "Info";
                logStream.WriteLine(DateTime.Now.ToString("o") + " - " + logType + ": " + Message);
                Console.ForegroundColor = LogLevelToConsoleColor(logLevel);
                Console.Write(logType);
                Console.ResetColor();
                Console.WriteLine(": " + Message);
            }
        }

        public static ConsoleColor LogLevelToConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return ConsoleColor.DarkBlue;

                case LogLevel.Information:
                    return ConsoleColor.Cyan;

                case LogLevel.Warning:
                    return ConsoleColor.Yellow;

                case LogLevel.Error:
                case LogLevel.Fatal:
                    return ConsoleColor.Red;

                default:
                    return ConsoleColor.White;
            }
        }

        /// <summary>
        /// Log a message. Only works if the logmanager is initialized. Static method because its easier to type
        /// </summary>
        /// <param name="Message">the message</param>
        /// <param name="logLevel">the level of the log item</param>
        public static void Log(string Message, LogLevel logLevel = LogLevel.Information)
        {
            try
            {
                singleton.LogOne(Message, logLevel);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine(Message);
            }
        }

        /// <summary>
        /// Log a message, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        public static void Log(string message, LogLevel logLevel = LogLevel.Information, params object[] args)
        {
            Log(string.Format(message, args), logLevel);
        }

        /// <summary>
        /// Log a message as debug, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="LogManager.Log" />
        public static void Debug(string message, params object[] args) => Log(message, LogLevel.Debug, args);

        /// <summary>
        /// Log a message as info, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="LogManager.Log" />
        public static void Info(string message, params object[] args) => Log(message, LogLevel.Information, args);

        /// <summary>
        /// Log a message as warning, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="LogManager.Log" />
        public static void Warning(string message, params object[] args) => Log(message, LogLevel.Warning, args);

        /// <summary>
        /// Log a message as error, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="LogManager.Log" />
        public static void Error(string message, params object[] args) => Log(message, LogLevel.Error, args);

        /// <summary>
        /// Log a message as fatal, taking in a format string and format list using the regular <see cref="string.Format" /> syntax.
        /// </summary>
        /// <seealso cref="LogManager.Log" />
        public static void Fatal(string message, params object[] args) => Log(message, LogLevel.Fatal, args);
    }
}
