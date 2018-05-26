using SS14.Shared.Interfaces.Log;
using System;
using System.IO;
using System.Text;

namespace SS14.Shared.Log
{
    public sealed class FileLogHandler : ILogHandler, IDisposable
    {
        private readonly StreamWriter writer;

        public FileLogHandler(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            writer = new StreamWriter(path, true, Encoding.UTF8);
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        public void Log(LogMessage message)
        {
            var name = message.LogLevelToName();
            writer.WriteLine("{0} [{1}] {2}: {3}", DateTime.Now.ToString("o"), name, message.SawmillName, message.Message);

            // This probably isn't the best idea.
            // Remove this flush if it becomes a problem (say performance).
            writer.Flush();
        }
    }
}
