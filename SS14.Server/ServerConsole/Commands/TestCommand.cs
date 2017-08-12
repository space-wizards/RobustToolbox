using SS14.Server.Interfaces.ServerConsole;
using System;

namespace SS14.Server.ServerConsole.Commands
{
    public class TestCommand : IConsoleCommand
    {
        public string Command => "test";
        public string Help => "This is a test command.";
        public string Description => "This is a dummy test command.";

        public void Execute(params string[] args)
        {
            Console.WriteLine("Test!");
        }
    }
}
