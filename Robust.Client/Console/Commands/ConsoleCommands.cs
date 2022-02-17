// This file is for commands that do something to the console itself.
// Not some generic console command type.
// Couldn't think of a better name sorry.

using Robust.Shared.Console;

namespace Robust.Client.Console.Commands
{
    sealed class ClearCommand : IConsoleCommand
    {
        public string Command => "cls";
        public string Help => "Clears the debug console of all messages.";
        public string Description => "Clears the console.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            shell.Clear();
        }
    }

    sealed class FillCommand : IConsoleCommand
    {
        public string Command => "fill";
        public string Help => "Fills the console with some nonsense for debugging.";
        public string Description => "Fill up the console for debugging.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            for (int x = 0; x < 50; x++)
            {
                shell.WriteLine("filling...");
            }
        }
    }
}
