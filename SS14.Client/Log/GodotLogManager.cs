using Godot;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Log
{
    public class GodotLogManager : LogManager
    {
        protected override void LogInternal(string message, LogLevel level)
        {
#if WINDOWS
            base.LogInternal(message, level);
#else
            // Something about the way Godot does its thing
            // causes color setting on the console to break on MacOS (and probably Linux).
            // So don't do that!
            string name = LogLevelToName(level);
            Console.WriteLine($"{name}: {message}");
#endif
        }
    }
}
