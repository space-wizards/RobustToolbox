using System.Linq;
using System.Text;
using Robust.Server.Interfaces.Player;

namespace Robust.Server.Console.Commands
{
    public class ListCommands : IServerCommand
    {
        public string Command => "list";

        public string Description => "Outputs a list of all commands which are currently available to you. " +
                                     "If a filter is provided, " +
                                     "only commands that contain the given string in their name will be listed.";

        public string Help => "Usage: list [filter]";

        public void Execute(IServerConsoleShell shell, string argStr, string[] args)
        {
            var filter = "";
            if (args.Length == 1)
            {
                filter = args[0];
            }

            var builder = new StringBuilder("SIDE NAME            DESC\n-------------------------\n");
            foreach (var command in shell.RegisteredCommands.Values
                .Where(p => p.Command.Contains(filter))
                .OrderBy(c => c.Command))
            {
                //TODO: Make this actually check permissions.

                builder.AppendLine($"S {command.Command,-16}{command.Description}");
            }

            var message = builder.ToString().Trim(' ', '\n');
            shell.WriteLine(message);
        }
    }
}
