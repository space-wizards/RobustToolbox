using SS14.Shared.Interfaces.Log;
using System;
using System.Collections.Generic;

namespace SS14.Shared.Log
{
    public sealed partial class LogManager
    {
        private sealed class Sawmill : ISawmill
        {
            public string Name { get; }

            public Sawmill Parent { get; }

            public LogLevel? Level { get; set; }

            private List<ILogHandler> handlers = new List<ILogHandler>();

            public Sawmill(Sawmill parent, string name)
            {
                Parent = parent;
                Name = name;
            }

            public void AddHandler(ILogHandler handler)
            {
                handlers.Add(handler);
            }

            public void RemoveHandler(ILogHandler handler)
            {
                handlers.Remove(handler);
            }

            public void Log(string message, LogLevel level, params object[] args)
            {
                var msg = new LogMessage(string.Format(message, args), level, Name);
                LogInternal(ref msg);
            }

            private void LogInternal(ref LogMessage message)
            {
                if (message.Level < GetPracticalLevel())
                {
                    return;
                }

                foreach (var handler in handlers)
                {
                    handler.Log(message);
                }
            }

            private LogLevel GetPracticalLevel()
            {
                if (Level.HasValue)
                {
                    return Level.Value;
                }
                return Parent.GetPracticalLevel();
            }

            public void Debug(string message, params object[] args)
            {
                Log(message, LogLevel.Debug, args);
            }

            public void Info(string message, params object[] args)
            {
                Log(message, LogLevel.Info, args);
            }

            public void Warning(string message, params object[] args)
            {
                Log(message, LogLevel.Warning, args);
            }

            public void Error(string message, params object[] args)
            {
                Log(message, LogLevel.Error, args);
            }

            public void Fatal(string message, params object[] args)
            {
                Log(message, LogLevel.Fatal, args);
            }
        }
    }
}
