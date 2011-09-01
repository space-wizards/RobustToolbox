using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SS3D_Server.Modules
{
    public class LogManager
    {
        StreamWriter logStream;
        LogLevel currentLogLevel;

        private string logPath;
        public string LogPath
        {
            get { return logPath; }
            set
            {
                logPath = value;
            }
        }

        /// <summary>
        /// Singleton
        /// </summary>
        private static LogManager singleton;
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

            if ((int)logLevel >= (int)singleton.currentLogLevel)
            {
                string logType = logLevel.ToString();
                if(logType == "Information")
                    logType = "Info";
                logStream.WriteLine(logType + ": " + Message);
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
            catch (NullReferenceException e)
            {
                Console.WriteLine(Message);
            }
        }

    }

    public enum LogLevel
    {
        Debug,
        Information,
        Warning, 
        Error,
        Fatal
    }
}
