using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.ServerConsole.Commands
{
    class LogSetLevelCommand : IConsoleCommand
    {
        public string Command => "loglevel";

        public string Description => "Changes the log level for a provided sawmill.";

        public string Help => "Usage: loglevel <sawmill> <level>";

        public void Execute(params string[] args)
        {
            if (args.Length != 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid argument amount. Expected 2 arguments.");
                Console.ResetColor();
                return;
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
        }
    }

    class TestLog : IConsoleCommand
    {
        public string Command => "testlog";
        public string Description => "Writes a test log to a sawmill.";
        public string Help => "Usage: testlog <sawmill> <level> <messagage>";

        public void Execute(params string[] args)
        {
            if (args.Length != 3)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid argument amount. Expected exactly 3 arguments.");
                Console.ResetColor();
                return;
            }

            var name = args[0];
            var levelname = args[1];
            var message = args[2]; // yes this doesn't support spaces idgaf.
            var level = (LogLevel)Enum.Parse(typeof(LogLevel), levelname);

            Logger.LogS(level, name, message, level);
        }
    }
}
