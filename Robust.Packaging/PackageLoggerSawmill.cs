using Robust.Shared.Log;

namespace Robust.Packaging;

public sealed class PackageLoggerSawmill : IPackageLogger
{
    private readonly ISawmill _sawmill;

    public PackageLoggerSawmill(ISawmill sawmill)
    {
        _sawmill = sawmill;
    }

    public void Log(LogLevel level, string msg) => _sawmill.Log(level, msg);

    public void Log(LogLevel level, string msg, params object?[] fmt) => _sawmill.Log(level, msg, fmt);
}
