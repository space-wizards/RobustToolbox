using Robust.Shared.Log;

namespace Robust.Packaging;

public sealed class PackageLoggerConsole : IPackageLogger
{
    public LogLevel MinimumLevel { get; init; } = LogLevel.Debug;

    public void Log(LogLevel level, string msg)
    {
        if (level < MinimumLevel)
            return;

        Console.Write(ConsoleLogHandler.LogLevelToString(level));
        Console.WriteLine(msg);
    }
}
