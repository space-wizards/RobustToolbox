using System;
using Robust.Shared.Console;

namespace Robust.Client.Console.Commands
{
    class QuitCommand : IConsoleCommand
    {
        public string Command => "quit";
        public string Description => "Kills the game client instantly.";
        public string Help => "Kills the game client instantly, leaving no traces. No telling the server goodbye";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            Environment.Exit(0);
        }
    }
}
