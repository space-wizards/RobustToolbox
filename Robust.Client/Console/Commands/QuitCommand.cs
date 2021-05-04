using System;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    class HardQuitCommand : IConsoleCommand
    {
        public string Command => "hardquit";
        public string Description => "Kills the game client instantly.";
        public string Help => "Kills the game client instantly, leaving no traces. No telling the server goodbye";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            Environment.Exit(0);
        }
    }

    class QuitCommand : IConsoleCommand
    {
        public string Command => "quit";
        public string Description => "Shuts down the game client gracefully.";
        public string Help => "Properly shuts down the game client, notifying the connected server and such.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IGameController>().Shutdown("quit command used");
        }
    }
}
