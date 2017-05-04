using System;

namespace SS14.Server.Services.ServerConsole.Commands
{
    public class ThrowException : ConsoleCommand
    {
        public override string Command => "shit";
        public override string Help => "Throws a bare Exception for debugging purposes.";
        public override string Description => "Throws an exception.";

        public override void Execute(params string[] args)
        {
            throw new Exception("Debug exception");
        }
    }
}
