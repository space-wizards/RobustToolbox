using Robust.Shared.Log;

namespace Robust.Packaging.Tests;

/// <summary>
/// Package logger for writing to NUnit's test context.
/// </summary>
/// <param name="writer"></param>
public sealed class PackageLoggerNUnit(TextWriter writer) : IPackageLogger
{
    public void Log(LogLevel level, string msg)
    {
        writer.WriteLine($"[{level}] {msg}");
    }
}
