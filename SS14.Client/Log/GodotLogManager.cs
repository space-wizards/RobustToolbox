using Godot;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Log
{
    /// <summary>
    ///     Logs messages using GD.Print() so they appear inside Godot's editor.
    ///     This does mean no colored logging on stdout, sadly.
    /// </summary>
    public class GodotLogManager : LogManager
    {
        protected override void LogInternal(string message, LogLevel level)
        {
            var levelname = LogLevelToName(level);
            GD.Print($"{levelname}: {message}");
        }
    }
}
