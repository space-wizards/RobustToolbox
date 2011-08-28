using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Modules
{
    public class LogManager
    {
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
        }
    }
}
