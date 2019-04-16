using Robust.Shared.Interfaces.Log;
using System;
using System.IO;
using System.Text;
using Robust.Shared.Utility;

namespace Robust.Shared.Log
{
    public sealed class FileLogHandler : ILogHandler, IDisposable
    {
        private readonly TextWriter writer;

        public FileLogHandler(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            writer = TextWriter.Synchronized(new StreamWriter(path, true, EncodingHelpers.UTF8));
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        public void Log(in LogMessage message)
        {
            var name = message.LogLevelToName();
            writer.WriteLine("{0:o} [{1}] {2}: {3}", DateTime.Now, name, message.SawmillName, message.Message);

            // This probably isn't the best idea.
            // Remove this flush if it becomes a problem (say performance).
            writer.Flush();
        }
    }
}
