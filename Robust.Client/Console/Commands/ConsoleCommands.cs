// This file is for commands that do something to the console itself.
// Not some generic console command type.
// Couldn't think of a better name sorry.

using System;
using Robust.Client.Interfaces.Console;
using Robust.Shared.Console;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands
{
    class ClearCommand : IConsoleCommand
    {
        public string Command => "cls";
        public string Help => "Clears the debug console of all messages.";
        public string Description => "Clears the console.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            console.Clear();
            return false;
        }
    }

    class FillCommand : IConsoleCommand
    {
        public string Command => "fill";
        public string Help => "Fills the console with some nonsense for debugging.";
        public string Description => "Fill up the console for debugging.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            Color[] colors = { Color.Green, Color.Blue, Color.Red };
            Random random = new Random();
            for (int x = 0; x < 50; x++)
            {
                console.AddLine("filling...", colors[random.Next(0, colors.Length)]);
            }
            return false;
        }
    }
}
