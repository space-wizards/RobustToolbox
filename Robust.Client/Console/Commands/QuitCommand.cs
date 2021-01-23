using System;

namespace Robust.Client.Console.Commands
{
    class QuitCommand : IClientCommand
    {
        public string Command => "quit";
        public string Description => "Kills the game client instantly.";
        public string Help => "Kills the game client instantly, leaving no traces. No telling the server goodbye";

        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            Environment.Exit(0);
            return false;
        }
    }
}
