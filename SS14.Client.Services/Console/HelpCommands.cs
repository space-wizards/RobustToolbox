using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.Console;
using SS14.Shared.IoC;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Interfaces.Network;
using SFML.Graphics;

namespace SS14.Client.Services.Console
{
    class HelpCommand : IConsoleCommand
    {
        public string Command => "help";
        public string Help => "When no arguments are provided, displays a generic help text. When an argument is passed, display the help text for the command with that name.";
        public string Description => "Display help text.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    console.AddLine("To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'.", Color.White);
                    break;

                case 1:
                    string commandname = args[0];
                    if (!console.Commands.ContainsKey(commandname))
                    {
                        if (!IoCManager.Resolve<INetworkManager>().IsConnected)
                        {
                            // No server so nothing to respond with unknown command.
                            console.AddLine("Unknown command: " + commandname, Color.Red);
                            return false;
                        }
                        return false; // return true;
                    }
                    IConsoleCommand command = console.Commands[commandname];
                    console.AddLine(string.Format("{0} - {1}", command.Command, command.Description), Color.White);
                    console.AddLine(command.Help, Color.White);
                    break;

                default:
                    console.AddLine("Invalid amount of arguments.", Color.Red);
                    break;
            }
            return false;
        }
    }
}
