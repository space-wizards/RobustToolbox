using System.Text;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;

namespace SS14.Server.ClientConsoleHost.Commands
{
    public class ListCommands : IClientCommand
    {
        public string Command => "list";
        public string Description => "Outputs a list of all commands which are currently available to you.";
        public string Help => "list";

        public void Execute(IClientConsoleHost host, IPlayerSession player, string[] args)
        {
            var builder = new StringBuilder("SIDE NAME            DESC\n-------------------------\n");
            foreach (var command in host.AvailableCommands.Values)
            {
                builder.AppendLine($"S    {command.Command.PadRight(16)}{command.Description}");
            }
            var message = builder.ToString().Trim(' ', '\n');
            host.SendText(player, message);
        }
    }
}
