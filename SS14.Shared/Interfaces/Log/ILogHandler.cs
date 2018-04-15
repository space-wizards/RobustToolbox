using SS14.Shared.Log;

namespace SS14.Shared.Interfaces.Log
{
    public interface ILogHandler
    {
        void Log(LogMessage message);
    }
}
