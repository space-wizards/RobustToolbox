using SS14.Server.Interfaces.ServerConsole;
using System;

namespace SS14.Server.ServerConsole.Commands
{
    public class ThrowException : IConsoleCommand
    {
        public string Command => "shit";
        public string Help => "Throws a bare Exception for debugging purposes.";
        public string Description => "Throws an exception.";

        public void Execute(params string[] args)
        {
            throw new Exception("Debug exception");
        }
    }
}
