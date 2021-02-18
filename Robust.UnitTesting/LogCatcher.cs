using System.Collections.Generic;
using Robust.Shared.Log;
using Serilog.Events;

namespace Robust.UnitTesting
{
    /// <summary>
    ///     Utility class for testing that logs are indeed being thrown.
    /// </summary>
    public class LogCatcher : ILogHandler
    {
        /// <summary>
        ///     Read only list of every log message that was caught since the last flush.
        /// </summary>
        public IReadOnlyList<LogEvent> CaughtLogs => _logs;
        private readonly List<LogEvent> _logs = new();

        /// <summary>
        ///     Clears all currently caught logs
        /// </summary>
        public void Flush()
        {
            lock (_logs)
            {
                _logs.Clear();
            }
        }

        void ILogHandler.Log(string sawmillName, LogEvent message)
        {
            lock (_logs)
            {
                _logs.Add(message);
            }
        }
    }
}
