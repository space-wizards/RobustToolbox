using Robust.Shared.Interfaces.Log;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Robust.Shared.Log
{
    public sealed partial class LogManager
    {
        private sealed class Sawmill : ISawmill
        {
            public string Name { get; }

            public Sawmill? Parent { get; }

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

            private readonly List<ILogHandler> _handlers = new List<ILogHandler>();
            private readonly ReaderWriterLockSlim _handlerLock = new ReaderWriterLockSlim();

            public Sawmill(Sawmill? parent, string name)
            {
                Parent = parent;
                Name = name;
            }

            public void AddHandler(ILogHandler handler)
            {
                _handlerLock.EnterWriteLock();
                try
                {
                    _handlers.Add(handler);
                }
                finally
                {
                    _handlerLock.ExitWriteLock();
                }
            }

            public void RemoveHandler(ILogHandler handler)
            {
                _handlerLock.EnterWriteLock();
                try
                {
                    _handlers.Remove(handler);
                }
                finally
                {
                    _handlerLock.ExitWriteLock();
                }
            }

            public void Log(LogLevel level, string message, params object?[] args)
            {
                Log(level, string.Format(message, args));
            }

            public void Log(LogLevel level, string message)
            {
                var msg = new LogMessage(message, level, Name);
                LogInternal(msg);
            }

            private void LogInternal(in LogMessage message)
            {
                if (message.Level < GetPracticalLevel())
                {
                    return;
                }

                _handlerLock.EnterReadLock();
                try
                {
                    foreach (var handler in _handlers)
                    {
                        handler.Log(message);
                    }
                }
                finally
                {
                    _handlerLock.ExitReadLock();
                }

                Parent?.LogInternal(message);
            }

            private LogLevel GetPracticalLevel()
            {
                if (Level.HasValue)
                {
                    return Level.Value;
                }
                return Parent?.GetPracticalLevel() ?? default;
            }

            public void Debug(string message, params object?[] args)
            {
                Log(LogLevel.Debug, message, args);
            }

            public void Debug(string message)
            {
                Log(LogLevel.Debug, message);
            }

            public void Info(string message, params object?[] args)
            {
                Log(LogLevel.Info, message, args);
            }

            public void Info(string message)
            {
                Log(LogLevel.Info, message);
            }

            public void Warning(string message, params object?[] args)
            {
                Log(LogLevel.Warning, message, args);
            }

            public void Warning(string message)
            {
                Log(LogLevel.Warning, message);
            }

            public void Error(string message, params object?[] args)
            {
                Log(LogLevel.Error, message, args);
            }

            public void Error(string message)
            {
                Log(LogLevel.Error, message);
            }

            public void Fatal(string message, params object?[] args)
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
