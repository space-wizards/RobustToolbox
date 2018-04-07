using SS14.Client.Interfaces.Console;
using System;

namespace SS14.Client.Console.Commands
{
    class QuitCommand : IConsoleCommand
    {
        public string Command => "quit";
        public string Description => "Kills the game client instantly.";
        public string Help => "Kills the game client instantly, leaving no traces. No telling the server goodbye";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            Environment.Exit(0);
            return false;
        }
    }
}
