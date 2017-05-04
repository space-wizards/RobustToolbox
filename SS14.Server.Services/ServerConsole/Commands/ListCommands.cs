using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using Con = System.Console;

namespace SS14.Server.Services.ServerConsole.Commands
{
    public class ListCommands : ConsoleCommand
    {
        public override string Command => "list";
        public override string Description => "Lists all available commands";
        public override string Help => "Lists all available commands and their short description.";

        public override void Execute(params string[] args)
        {
            var availableCommands = IoCManager.Resolve<IConsoleManager>().AvailableCommands;
            Con.ForegroundColor = ConsoleColor.Yellow;
            Con.WriteLine("\nAvailable commands:\n");

            List<string> names = availableCommands.Keys.ToList();
            names.Sort();
            foreach (string c in names)
            {
                string name = String.Format("{0, 16}", c);
                Con.ForegroundColor = ConsoleColor.Cyan;
                Con.SetCursorPosition(0, Con.CursorTop);
                Con.Write(name);
                Con.ForegroundColor = ConsoleColor.Green;
                Con.Write(" - ");
                Con.ForegroundColor = ConsoleColor.White;
                switch (c)
                {
                    case "help":
                        Con.WriteLine("Lists general help. Type 'help <command>' for specific help on a command.");
                        break;

                    default:
                        Con.WriteLine(availableCommands[c].Description);
                        break;
                }
                Con.ResetColor();
            }
            Con.ForegroundColor = ConsoleColor.White;
            Con.Write("\n\t\t\t" + availableCommands.Count);
            Con.ForegroundColor = ConsoleColor.Yellow;
            Con.WriteLine(" commands available.\n");
            Con.ResetColor();
        }
    }
}
