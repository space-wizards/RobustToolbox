using System;

namespace SS14.Server.Services.ServerConsole.Commands
{
    public class TestCommand : ConsoleCommand
    {
        public override string Command => "test";
        public override string Help => "This is a test command.";
        public override string Description => "This is a dummy test command.";

        public override void Execute(params string[] args)
        {
            Console.WriteLine("Test!");
        }
    }
}
