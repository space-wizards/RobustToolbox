using System.Linq;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands
{
    class HelpCommand : IClientCommand
    {
        public string Command => "help";
        public string Help => "When no arguments are provided, displays a generic help text. When an argument is passed, display the help text for the command with that name.";
        public string Description => "Display help text.";

        public bool Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    shell.WriteLine("To display help for a specific command, write 'help <command>'. To list all available commands, write 'list'.");
                    break;

                case 1:
                    string commandname = args[0];
                    if (!shell.RegisteredCommands.ContainsKey(commandname))
                    {
                        if (!IoCManager.Resolve<IClientNetManager>().IsConnected)
                        {
                            // No server so nothing to respond with unknown command.
                            shell.WriteLine("Unknown command: " + commandname, Color.Red);
                            return false;
                        }
                        // TODO: Maybe have a server side help?
                        return false;
                    }
                    IClientCommand command = shell.RegisteredCommands[commandname];
                    shell.WriteLine(string.Format("{0} - {1}", command.Command, command.Description));
                    shell.WriteLine(command.Help);
                    break;

                default:
                    shell.WriteLine("Invalid amount of arguments.", Color.Red);
                    break;
            }
            return false;
        }
    }

    class ListCommand : IClientCommand
    {
        public string Command => "list";
        public string Help => "Usage: list [filter]\n" +
                              "Lists all available commands, and their short descriptions.\n" +
                              "If a filter is provided, " +
                              "only commands that contain the given string in their name will be listed.";
        public string Description => "List all commands, optionally with a filter.";

        public bool Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            var filter = "";
            if (args.Length == 1)
            {
                filter = args[0];
            }

            var conGroup = IoCManager.Resolve<IClientConGroupController>();
            foreach (var command in shell.RegisteredCommands.Values
                .Where(p => p.Command.Contains(filter) && conGroup.CanCommand(p.Command))
                .OrderBy(c => c.Command))
            {
                shell.WriteLine(command.Command + ": " + command.Description);
            }

            return false;
        }
    }
}
