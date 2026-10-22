using System;
using System.Collections.Generic;
using Robust.Shared.Log;

namespace Robust.UnitTesting;

/// <summary>
/// Represents a log manager that spies on logged messages. Use this to check if side effect
/// like logging is being correctly done.
/// </summary>
public sealed class SpyLogManager : ILogManager
{
    private readonly SpyLogger _spyLogger = new();
    public ISawmill RootSawmill => _spyLogger;
    public ISawmill GetSawmill(string name)
    {
        return _spyLogger;
    }

    public IEnumerable<ISawmill> AllSawmills => new[]
    {
        _spyLogger
    };

    public int CountError => _spyLogger.ErrorMessages.Count;

    public void Clear()
    {
        _spyLogger.Clear();
    }
}

/// <summary>
/// Represents a logger used for spying on log messages.
/// </summary>
public sealed class SpyLogger : ISawmill
{
    public string Name { get; } = "SpyLogger";
    public LogLevel? Level { get; set; } = LogLevel.Debug;

    public List<string> DebugMessages = new();
    public List<string> ErrorMessages = new();
    public List<string> WarningMessages = new();
    public List<string> InfoMessages = new();
    public List<string> FatalMessages = new();
    public List<string> VerboseMessages = new();

    public void AddHandler(ILogHandler handler)
    {
        // NOT NEEDED
    }

    public void RemoveHandler(ILogHandler handler)
    {
        // NOT NEEDED
    }

    public void Log(LogLevel level, string message, params object?[] args)
    {
        Log(level, null, message, args);
    }

    public void Log(LogLevel level, Exception? exception, string message, params object?[] args)
    {
        var msg = string.Format(message, args);

        var list = level switch
        {
            LogLevel.Verbose => VerboseMessages,
            LogLevel.Debug => DebugMessages,
            LogLevel.Info => InfoMessages,
            LogLevel.Warning => WarningMessages,
            LogLevel.Error => ErrorMessages,
            LogLevel.Fatal => FatalMessages,
            _ => VerboseMessages,
        };

        list.Add(msg);
    }

    public void Log(LogLevel level, string message)
    {
        Log(level, null, message, []);
    }

    public void Debug(string message, params object?[] args)
    {
        Log(LogLevel.Debug, null, message, args);
    }

    public void Debug(string message)
    {
        Log(LogLevel.Debug, message);
    }

    public void Info(string message, params object?[] args)
    {
        Log(LogLevel.Info, null, message, args);
    }

    public void Info(string message)
    {
        Log(LogLevel.Debug, message);
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

    public void Clear()
    {
        DebugMessages.Clear();
        ErrorMessages.Clear();
        WarningMessages.Clear();
        InfoMessages.Clear();
        FatalMessages.Clear();
        VerboseMessages.Clear();
    }
}

