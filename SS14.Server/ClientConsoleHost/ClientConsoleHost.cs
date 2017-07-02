using Lidgren.Network;
using SFML.System;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using SS14.Shared.Utility;
using System.Collections.Generic;
using System.Reflection;
using System;
using SS14.Shared.Interfaces.Network;

namespace SS14.Server.ClientConsoleHost
{
    public class ClientConsoleHost : IClientConsoleHost, IPostInjectInit
    {
        [Dependency]
        private readonly IReflectionManager reflectionManager;
        private readonly Dictionary<string, IClientCommand> availableCommands = new Dictionary<string, IClientCommand>();
        public IDictionary<string, IClientCommand> AvailableCommands => availableCommands;

        public void HandleRegistrationRequest(NetConnection senderConnection)
        {
            var netMgr = IoCManager.Resolve<INetworkServer>();
            var message = netMgr.Server.CreateMessage();
            message.Write((byte)NetMessages.ConsoleCommandRegister);
            message.Write((UInt16)AvailableCommands.Count);
            foreach (var command in AvailableCommands.Values)
            {
                message.Write(command.Command);
                message.Write(command.Description);
                message.Write(command.Help);
            }

            netMgr.SendMessage(message, senderConnection);
        }

        public void PostInject()
        {
            foreach (Type type in reflectionManager.GetAllChildren<IClientCommand>())
            {
                var instance = Activator.CreateInstance(type, null) as IClientCommand;
                if (AvailableCommands.TryGetValue(instance.Command, out IClientCommand duplicate))
                {
                    throw new InvalidImplementationException(instance.GetType(), typeof(IClientCommand), $"Command name already registered: {instance.Command}, previous: {duplicate.GetType()}");
                }

                AvailableCommands[instance.Command] = instance;
            }
        }

        public void ProcessCommand(string text, NetConnection sender)
        {
            var args = new List<string>();

            CommandParsing.ParseArguments(text, args);

            if (args.Count == 0)
            {
                return;
            }
            string cmd = args[0];

            try
            {
                IClientCommand command = AvailableCommands[cmd];
                args.RemoveAt(0);
                var client = IoCManager.Resolve<INetworkServer>().GetChannel(sender);
                command.Execute(this, client, args.ToArray());
            }
            catch (KeyNotFoundException)
            {
                SendConsoleReply(string.Format("Unknown command: '{0}'", cmd), sender);
            }
            catch (Exception e)
            {
                SendConsoleReply(string.Format("There was an error while executing the command: {0}", e.Message), sender);
            }
        }

        public void SendConsoleReply(string text, NetConnection target)
        {
            var netMgr = IoCManager.Resolve<INetworkServer>();
            NetOutgoingMessage replyMsg = netMgr.Server.CreateMessage();
            replyMsg.Write((byte)NetMessages.ConsoleCommandReply);
            replyMsg.Write(text);
            netMgr.Server.SendMessage(replyMsg, target, NetDeliveryMethod.ReliableUnordered);
        }
    }
}
