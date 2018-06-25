using SS14.Server.Interfaces.ServerConsole;
using System;
using SS14.Server.Interfaces.Player;

namespace SS14.Server.ServerConsole.Commands
{
    public class TestCommand : IConsoleCommand
    {
        public string Command => "test";
        public string Help => "This is a test command.";
        public string Description => "This is a dummy test command.";

        public void Execute(IConsoleManager host, IPlayerSession player, string[] args)
        {
            Console.WriteLine("Test!");
        }
    }
}
