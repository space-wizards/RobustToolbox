using System;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Log;

namespace SS14.Server.Console.Commands
{
    class LogSetLevelCommand : IClientCommand
    {
        public string Command => "loglevel";
        public string Description => "Changes the log level for a provided sawmill.";
        public string Help => "loglevel <sawmill> <level>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 2)
            {
                shell.SendText(player, "Invalid argument amount. Expected 2 arguments.");
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

    class TestLog : IClientCommand
    {
        public string Command => "testlog";
        public string Description => "Writes a test log to a sawmill.";
        public string Help => "testlog <sawmill> <level> <messagage>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 3)
            {
                shell.SendText(player, "Invalid argument amount. Expected exactly 3 arguments.");
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
