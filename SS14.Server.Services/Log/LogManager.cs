using SS14.Server.Interfaces;
using SS14.Shared;
using SS14.Shared.ServerEnums;
using System;
using System.IO;

namespace SS14.Server.Services.Log
{
    public class LogManager : IService
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

        #region IService Members

        public ServerServiceType ServiceType
        {
            get { return ServerServiceType.LogManager; }
        }

        #endregion

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
            logStream = new StreamWriter(LogPath, true);
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

            if ((int) logLevel >= (int) singleton.currentLogLevel)
            {
                string logType = logLevel.ToString();
                if (logType == "Information")
                    logType = "Info";
                logStream.WriteLine(DateTime.Now.ToString("o") + " - " + logType + ": " + Message);
                Console.Write(logType + ": " + Message + "\n");
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
    }
}