using SS14.Shared.Log;

namespace SS14.Shared.Interfaces.Log
{
    /// <summary>
    ///     Formats and prints a log message to an output source.
    /// </summary>
    public interface ILogHandler
    {
        void Log(LogMessage message);
    }
}
