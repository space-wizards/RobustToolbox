using System;
using Robust.Shared.Log;

namespace Robust.UnitTesting;

/// <summary>
/// Implementation of <see cref="ISawmill"/> that does absolutely nothing.
/// </summary>
public sealed class DummySawmill : ISawmill
{
    public string Name => "dummy";

    public LogLevel? Level { get; set; }

    public void AddHandler(ILogHandler handler)
    {
    }

    public void RemoveHandler(ILogHandler handler)
    {
    }

    public void Log(LogLevel level, string message, params object?[] args)
    {
    }

    public void Log(LogLevel level, Exception? exception, string message, params object?[] args)
    {
    }

    public void Log(LogLevel level, string message)
    {
    }

    public void Debug(string message, params object?[] args)
    {
    }

    public void Debug(string message)
    {
    }

    public void Info(string message, params object?[] args)
    {
    }

    public void Info(string message)
    {
    }

    public void Warning(string message, params object?[] args)
    {
    }

    public void Warning(string message)
    {
    }

    public void Error(string message, params object?[] args)
    {
    }

    public void Error(string message)
    {
    }

    public void Fatal(string message, params object?[] args)
    {
    }

    public void Fatal(string message)
    {
    }
}
