using SS14.Server.Interfaces.ServerConsole;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;

namespace SS14.Server.Services.ServerConsole.Commands
{
    public class HelpCommand : IConsoleCommand
    {
        public string Command => "help";
        public string Description => "Show general or command specific help text.";
        public string Help => "help [command]\nIf command is not given, display general purpose help. If it is, print the help text for the specified command.";

        public void Execute(params string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    Console.Write("To show help on a specific command, run ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("help <command>");
                    Console.ResetColor();
                    Console.Write("To display a list of commands, run ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("list");
                    Console.ResetColor();
                    break;

                case 1:
                    string commandName = args[0].ToLower();
                    var commands = IoCManager.Resolve<IConsoleManager>().AvailableCommands;
                    if (!commands.ContainsKey(commandName))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Unknown command: {0}", commandName);
                        Console.ResetColor();
                        return;
                    }
                    var command = commands[commandName];
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(commandName);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(" - ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(command.Description);
                    Console.WriteLine();
                    Console.ResetColor();
                    Console.WriteLine(command.Help);

                    break;

                default:
                    throw new ArgumentException("Invalid amount of arguments supplied");
            }
        }
    }
}
