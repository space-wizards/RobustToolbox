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

        public static void Initialize(string _logPath)
        {
            singleton = new LogManager();
            singleton.LogPath = _logPath;
            singleton.currentLogLevel = LogLevel.Debug;
            singleton.Start();
        }

        public void Start()
        {
            logStream = new StreamWriter(LogPath, true);
            logStream.AutoFlush = true;

            LogOne("LogManager started.", LogLevel.Information);
        }

        public void LogOne(string Message, LogLevel logLevel)
        {
            if (singleton == null)
                throw new TypeInitializationException("LogManager Not Initialized.", null);

            if ((int)logLevel >= (int)singleton.currentLogLevel)
            {
                logStream.WriteLine(logLevel.ToString() + ": " + Message);
                Console.Write(logLevel.ToString() + ": " + Message + "\n");
            }
        }

        public static void Log(string Message, LogLevel logLevel)
        {
            singleton.LogOne(Message, logLevel);
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
