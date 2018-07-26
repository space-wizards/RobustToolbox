using System.Text;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.Player;

namespace SS14.Server.Console.Commands
{
    public class ListCommands : IClientCommand
    {
        public string Command => "list";
        public string Description => "Outputs a list of all commands which are currently available to you.";
        public string Help => "list";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var builder = new StringBuilder("SIDE NAME            DESC\n-------------------------\n");
            foreach (var command in shell.AvailableCommands.Values)
            {
                //TODO: Make this actually check permissions.

                builder.AppendLine($"S {command.Command,-16}{command.Description}");
            }
            var message = builder.ToString().Trim(' ', '\n');
            shell.SendText(player, message);
        }
    }
}
