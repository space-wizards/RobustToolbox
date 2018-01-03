using System.Text;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;

namespace SS14.Server.ClientConsoleHost.Commands
{
    public class ListCommands : IClientCommand
    {
        public string Command => "sv_list";
        public string Description => "Lists all available commands.";
        public string Help => "Outputs a list of all commands which are currently available to you, and a total command number.";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var builder = new StringBuilder("Available commands:\n");
            foreach (var command in host.AvailableCommands.Values)
            {
                builder.AppendFormat("{0}: {1}\n", command.Command, command.Description);
            }
            var message = builder.ToString().Trim(' ', '\n');
            host.SendConsoleReply(player.ConnectedClient, message);
        }
    }
}
