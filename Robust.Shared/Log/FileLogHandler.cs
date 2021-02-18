using System;
using System.IO;
using Robust.Shared.Utility;
using Serilog.Events;

namespace Robust.Shared.Log
{
    internal sealed class FileLogHandler : ILogHandler, IDisposable
    {
        private readonly TextWriter writer;

        public FileLogHandler(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            writer = TextWriter.Synchronized(new StreamWriter(path, true, EncodingHelpers.UTF8));
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        public void Log(string sawmillName, LogEvent message)
        {
            var name = LogMessage.LogLevelToName(message.Level.ToRobust());
            writer.WriteLine("{0:o} [{1}] {2}: {3}", DateTime.Now, name, sawmillName, message.RenderMessage());

            if (message.Exception != null)
            {
                writer.WriteLine(message.Exception.ToString());
            }

            // This probably isn't the best idea.
            // Remove this flush if it becomes a problem (say performance).
            writer.Flush();
        }
    }
}
