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

            public LogLevel? Level
            {
                get => _level;
                set
                {
                    if (Name == "root" && value == null)
                    {
                        throw new ArgumentException("Cannot set root sawmill level to null.");
                    }
                    _level = value;
                }
            }
            private LogLevel? _level = null;

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

            public void Log(LogLevel level, string message, params object[] args)
            {
                Log(level, string.Format(message, args));
            }

            public void Log(LogLevel level, string message)
            {
                var msg = new LogMessage(message, level, Name);
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

                Parent?.LogInternal(ref message);
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
                Log(LogLevel.Debug, message, args);
            }

            public void Debug(string message)
            {
                Log(LogLevel.Debug, message);
            }

            public void Info(string message, params object[] args)
            {
                Log(LogLevel.Info, message, args);
            }

            public void Info(string message)
            {
                Log(LogLevel.Info, message);
            }

            public void Warning(string message, params object[] args)
            {
                Log(LogLevel.Warning, message, args);
            }

            public void Warning(string message)
            {
                Log(LogLevel.Warning, message);
            }

            public void Error(string message, params object[] args)
            {
                Log(LogLevel.Error, message, args);
            }

            public void Error(string message)
            {
                Log(LogLevel.Error, message);
            }

            public void Fatal(string message, params object[] args)
            {
                Log(LogLevel.Fatal, message, args);
            }

            public void Fatal(string message)
            {
                Log(LogLevel.Fatal, message);
            }
        }
    }
}
