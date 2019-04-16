using Robust.Client.Interfaces.Console;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Client.Console.Commands
{
    class LogSetLevelCommand : IConsoleCommand
    {
        public string Command => "loglevel";

        public string Description => "Changes the log level for a provided sawmill.";

        public string Help => "Usage: loglevel <sawmill> <level>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 2)
            {
                console.AddLine("Invalid argument amount. Expected 2 arguments.", Color.Red);
                return false;
            }

            var name = args[0];
            var levelname = args[1];
            LogLevel? level;
            if (levelname == "null")
            {
                level = null;
            }
            else
            {
                level = (LogLevel)Enum.Parse(typeof(LogLevel), levelname);
            }
            Logger.GetSawmill(name).Level = level;
            return false;
        }
    }

    class TestLog : IConsoleCommand
    {
        public string Command => "testlog";
        public string Description => "Writes a test log to a sawmill.";
        public string Help => "Usage: testlog <sawmill> <level> <messagage>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 3)
            {
                console.AddLine("Invalid argument amount. Expected 3 arguments.", Color.Red);
                return false;
            }

            var name = args[0];
            var levelname = args[1];
            var message = args[2]; // yes this doesn't support spaces idgaf.
            var level = (LogLevel)Enum.Parse(typeof(LogLevel), levelname);

            Logger.LogS(level, name, message);
            return false;
        }
    }
}
