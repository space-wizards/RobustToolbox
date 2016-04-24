using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Commands;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.Services.Chat.Commands
{
    public class ListCommands : ChatCommand
    {
        public override string Command
        {
            get
            {
                return "list";
            }
        }

        public override string Description
        {
            get
            {
                return "Lists all available commands.";
            }
        }

        public override string Help
        {
            get
            {
                return "Outputs a list of all commands which are currently available to you, and a total command number.";
            }
        }

        public override void Execute(IClient client, params string[] args)
        {
            string message = "Available commands:\n";
            foreach (IClientCommand command in IoCManager.Resolve<IChatManager>().GetCommands().Values)
            {
                message += String.Format("{0}: {1}\n", command.Command, command.Description);
            }
            Console.WriteLine(message);
        }
    }
}