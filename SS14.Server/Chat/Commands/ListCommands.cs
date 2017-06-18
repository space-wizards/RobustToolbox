using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.Chat.Commands
{
    public class ListCommands : IChatCommand
    {
        public string Command => "list";
        public string Description => "Lists all available commands.";
        public string Help => "Outputs a list of all commands which are currently available to you, and a total command number.";

        public void Execute(IChatManager manager, IClient client, params string[] args)
        {
            string message = "Available commands:\n";
            foreach (IChatCommand command in manager.Commands.Values)
            {
                message += String.Format("{0}: {1}\n", command.Command, command.Description);
            }
            message = message.Trim(' ', '\n');
            manager.SendPrivateMessage(client, Shared.ChatChannel.Default, message, "Server", null);
        }
    }
}
