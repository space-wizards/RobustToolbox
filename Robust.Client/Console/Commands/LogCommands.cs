using Robust.Shared.Log;
using Robust.Shared.Maths;
using System;

namespace Robust.Client.Console.Commands
{
    class LogSetLevelCommand : IClientCommand
    {
        public string Command => "loglevel";
        public string Description => "Changes the log level for a provided sawmill.";
        public string Help => "Usage: loglevel <sawmill> <level>"
                            + "\n    sawmill: A label prefixing log messages. This is the one you're setting the level for."
                            + "\n    level: The log level. Must match one of the values of the LogLevel enum.";

        public bool Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Invalid argument amount. Expected 2 arguments.", Color.Red);
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
                if (!Enum.TryParse<LogLevel>(levelname, out var result))
                {
                    shell.WriteLine("Failed to parse 2nd argument. Must be one of the values of the LogLevel enum.");
                    return false;
                }
                level = result;
            }
            Logger.GetSawmill(name).Level = level;
            return false;
        }
    }

    class TestLog : IClientCommand
    {
        public string Command => "testlog";
        public string Description => "Writes a test log to a sawmill.";
        public string Help => "Usage: testlog <sawmill> <level> <message>"
                            + "\n    sawmill: A label prefixing the logged message."
                            + "\n    level: The log level. Must match one of the values of the LogLevel enum."
                            + "\n    message: The message to be logged. Wrap this in double quotes if you want to use spaces.";

        public bool Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
            {
                shell.WriteLine("Invalid argument amount. Expected 3 arguments.", Color.Red);
                return false;
            }

            var name = args[0];
            var levelname = args[1];
            var message = args[2]; // yes this doesn't support spaces idgaf.
            if (!Enum.TryParse<LogLevel>(levelname, out var result))
            {
                shell.WriteLine("Failed to parse 2nd argument. Must be one of the values of the LogLevel enum.");
                return false;
            }
            var level = result;

            Logger.LogS(level, name, message);
            return false;
        }
    }
}
