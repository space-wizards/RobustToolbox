using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    sealed class ListCommand : IConsoleCommand
    {
        public string Command => "list";
        public string Help => "Usage: list [filter]\n" +
                              "Lists all available commands, and their short descriptions.\n" +
                              "If a filter is provided, " +
                              "only commands that contain the given string in their name will be listed.";
        public string Description => "List all commands, optionally with a filter.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var filter = "";
            if (args.Length == 1)
            {
                filter = args[0];
            }

            var conGroup = IoCManager.Resolve<IClientConGroupController>();
            foreach (var command in shell.ConsoleHost.RegisteredCommands.Values
                .Where(p => p.Command.Contains(filter) && (p is not ServerDummyCommand || conGroup.CanCommand(p.Command)))
                .OrderBy(c => c.Command))
            {
                shell.WriteLine(command.Command + ": " + command.Description);
            }
        }
    }
}
