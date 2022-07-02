using Robust.Shared.Log;

namespace Robust.Packaging;

// This is separate from ISawmill to avoid some of the baggage in standalone packaging operations.
// ACZ just pipes into a sawmill.

/// <summary>
/// Simple logging interface for packaging operations.
/// </summary>
public interface IPackageLogger
{
    void Log(LogLevel level, string msg);
    void Log(LogLevel level, string msg, params object?[] fmt) => Log(level, string.Format(msg, fmt));

    void Verbose(string msg) => Log(LogLevel.Verbose, msg);
    void Verbose(string msg, params object?[] fmt) => Log(LogLevel.Verbose, msg, fmt);

    void Debug(string msg) => Log(LogLevel.Debug, msg);
    void Debug(string msg, params object?[] fmt) => Log(LogLevel.Debug, msg, fmt);

    void Info(string msg) => Log(LogLevel.Info, msg);
    void Info(string msg, params object?[] fmt) => Log(LogLevel.Info, msg, fmt);

    void Warning(string msg) => Log(LogLevel.Warning, msg);
    void Warning(string msg, params object?[] fmt) => Log(LogLevel.Warning, msg, fmt);

    void Error(string msg) => Log(LogLevel.Error, msg);
    void Error(string msg, params object?[] fmt) => Log(LogLevel.Error, msg, fmt);

    void Fatal(string msg) => Log(LogLevel.Fatal, msg);
    void Fatal(string msg, params object?[] fmt) => Log(LogLevel.Fatal, msg, fmt);
}
