using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Modules
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
                    singleton = new LogManager();
                return singleton;
            }
        }
    }
}
