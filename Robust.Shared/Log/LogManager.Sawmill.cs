using Robust.Shared.Interfaces.Log;
using System;
using System.Collections.Generic;
using System.Threading;
using Serilog;
using Serilog.Events;
using SLogger = Serilog.Core.Logger;

namespace Robust.Shared.Log
{
    public sealed partial class LogManager
    {
        private sealed class Sawmill : ISawmill, IDisposable
        {
            // Need this to act as a proxy for some internal Serilog APIs related to message parsing.
            private readonly SLogger _sLogger = new LoggerConfiguration().CreateLogger();

            public string Name { get; }

            public Sawmill? Parent { get; }

            private bool _disposed;

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

            public List<ILogHandler> Handlers { get; } = new();
            private readonly ReaderWriterLockSlim _handlerLock = new();

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
                    Handlers.Add(handler);
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
                    Handlers.Remove(handler);
                }
                finally
                {
                    _handlerLock.ExitWriteLock();
                }
            }

            public void Log(LogLevel level, Exception? exception, string message, params object?[] args)
            {
                _sLogger.BindMessageTemplate(message, args, out var parsedTemplate, out var properties);
                var msg = new LogEvent(DateTimeOffset.Now, level.ToSerilog(), exception, parsedTemplate, properties);
                LogInternal(Name, msg);
            }

            public void Log(LogLevel level, string message, params object?[] args)
            {
                if (args.Length != 0 && message.Contains("{0"))
                {
                    // Fallback for logs that still use the string.Format approach.
                    message = string.Format(message, args);
                    args = Array.Empty<object>();
                }

                Log(level, null, message, args);
            }

            public void Log(LogLevel level, string message)
            {
                Log(level, message, Array.Empty<object>());
            }

            private void LogInternal(string sourceSawmill, LogEvent message)
            {
                if (message.Level.ToRobust() < GetPracticalLevel())
                {
                    return;
                }

                _handlerLock.EnterReadLock();
                try
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(nameof(Sawmill));
                    }

                    foreach (var handler in Handlers)
                    {
                        handler.Log(sourceSawmill, message);
                    }
                }
                finally
                {
                    _handlerLock.ExitReadLock();
                }

                Parent?.LogInternal(sourceSawmill, message);
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

            public void Dispose()
            {
                _handlerLock.EnterWriteLock();
                try
                {
                    _disposed = true;

                    foreach (ILogHandler handler in Handlers)
                    {
                        if (handler is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                finally
                {
                    _handlerLock.ExitWriteLock();
                }
            }
        }
    }
}
