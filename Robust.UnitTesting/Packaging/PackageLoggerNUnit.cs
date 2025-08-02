using System.IO;
using Robust.Packaging;
using Robust.Shared.Log;

namespace Robust.UnitTesting.Packaging;

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
