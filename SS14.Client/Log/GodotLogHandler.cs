using Godot;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Log;

namespace SS14.Client.Log
{
    class GodotLogHandler : ILogHandler
    {
        public void Log(LogMessage message)
        {
            var name = message.LogLevelToName();
            var msg = $"[{name}] {message.SawmillName}: {message.Message}";
            GD.Print(msg);
        }
    }
}
