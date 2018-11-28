using System.Collections.Generic;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Log;

namespace SS14.UnitTesting
{
    /// <summary>
    ///     Utility class for testing that logs are indeed being thrown.
    /// </summary>
    public class LogCatcher : ILogHandler
    {
        /// <summary>
        ///     Read only list of every log message that was caught since the last flush.
        /// </summary>
        public IReadOnlyList<LogMessage> CaughtLogs => _logs;
        private readonly List<LogMessage> _logs = new List<LogMessage>();

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

        void ILogHandler.Log(in LogMessage message)
        {
            lock (_logs)
            {
                _logs.Add(message);
            }
        }
    }
}
