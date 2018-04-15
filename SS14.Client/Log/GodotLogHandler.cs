using Godot;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Log
{
    public class GodotLogHandler : ILogHandler
    {
        public void Log(LogMessage message)
        {
            var name = LogMessage.LogLevelToName(message.Level);
            var msg = $"[{name}] {message.SawmillName}: {message.Message}";
            GD.Print(msg);
        }
    }
}
