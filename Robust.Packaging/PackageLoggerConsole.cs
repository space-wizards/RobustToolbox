using Robust.Shared.Log;

namespace Robust.Packaging;

public sealed class PackageLoggerConsole : IPackageLogger
{
    public void Log(LogLevel level, string msg)
    {
        Console.Write(ConsoleLogHandler.LogLevelToString(level));
        Console.WriteLine(msg);
    }
}
