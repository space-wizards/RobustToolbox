// This file is for commands that do something to the console itself.
// Not some generic console command type.
// Couldn't think of a better name sorry.

using Robust.Shared.Console;

namespace Robust.Client.Console.Commands
{
    sealed class ClearCommand : LocalizedCommands
    {
        public override string Command => "cls";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            shell.Clear();
        }
    }

    sealed class FillCommand : LocalizedCommands
    {
        public override string Command => "fill";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            for (int x = 0; x < 50; x++)
            {
                shell.WriteLine("filling...");
            }
        }
    }
}
