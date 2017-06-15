using Lidgren.Network;
using SFML.System;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Network;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace SS14.Server.ClientConsoleHost
{
    [IoCTarget]
    class ClientConsoleHost : IClientConsoleHost
    {
        private Dictionary<string, IClientCommand> availableCommands = new Dictionary<string, IClientCommand>();
        public IDictionary<string, IClientCommand> AvailableCommands => availableCommands;

        public void HandleRegistrationRequest(NetConnection senderConnection)
        {
            var netMgr = IoCManager.Resolve<ISS14NetServer>();
            var message = netMgr.CreateMessage();
            message.Write((byte)NetMessage.ConsoleCommandRegister);
            message.Write((UInt16)AvailableCommands.Count);
            foreach (var command in AvailableCommands.Values)
            {
                message.Write(command.Command);
                message.Write(command.Description);
                message.Write(command.Help);
            }

            netMgr.SendMessage(message, senderConnection);
        }

        public ClientConsoleHost()
        {
            foreach(Type type in IoCManager.ResolveEnumerable<IClientCommand>())
            {
                var instance = Activator.CreateInstance(type, null) as IClientCommand;
                if (AvailableCommands.ContainsKey(instance.Command))
                {
                    throw new Exception("Command name already registered: " + instance.Command);
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
                var client = IoCManager.Resolve<ISS14Server>().GetClient(sender);
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
            var netMgr = IoCManager.Resolve<ISS14NetServer>();
            NetOutgoingMessage replyMsg = netMgr.CreateMessage();
            replyMsg.Write((byte)NetMessage.ConsoleCommandReply);
            replyMsg.Write(text);
            netMgr.SendMessage(replyMsg, target, NetDeliveryMethod.ReliableUnordered);
        }
    }
}
