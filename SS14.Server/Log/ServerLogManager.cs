using SS14.Server.Interfaces.Log;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System.IO;
using System.Text;
using System;

namespace SS14.Server.Log
{
    public class ServerLogManager : LogManager, IServerLogManager, IDisposable
    {
        private bool disposed;
        private StreamWriter logStream;

        private string logPath;
        public string LogPath
        {
            get => logPath;
            set
            {
                if (LogPath == value)
                {
                    // Nothing changing, do nothing.
                    return;
                }

                if (logStream != null)
                {
                    // We know there'll be a change, so if we have one remove it.
                    logStream.Dispose();
                    logStream = null;
                }

                if (value != null)
                {
                    // Open a new file.
                    Directory.CreateDirectory(Path.GetDirectoryName(value));
                    try
                    {
                        logStream = new StreamWriter(value, true, Encoding.UTF8);
                    }
                    catch (IOException e)
                    {
                        Error("Unable to open log file ('{0}'): {1}", value, e);
                    }
                }

                logPath = value;
            }
        }

        protected override void LogInternal(string message, LogLevel level)
        {
            base.LogInternal(message, level);

            if (logStream == null)
            {
                return;
            }

            string name = LogLevelToName(level);
            logStream.WriteLine(string.Format("{0} - {1}: {2}", DateTime.Now.ToString("o"), name, message));
            // This probably isn't the best idea.
            // Remove this flush if it becomes a problem.
            logStream.Flush();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                logStream.Dispose();
                logStream = null;
            }

            disposed = true;
        }

        ~ServerLogManager()
        {
            Dispose(false);
        }

        #endregion IDisposable Members
    }
}
