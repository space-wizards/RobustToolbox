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
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Server.ClientConsoleHost
{
    public class ClientConsoleHost : IClientConsoleHost, IPostInjectInit
    {
        [Dependency]
        private readonly IReflectionManager reflectionManager;
        private readonly Dictionary<string, IClientCommand> availableCommands = new Dictionary<string, IClientCommand>();
        public IDictionary<string, IClientCommand> AvailableCommands => availableCommands;

        public void HandleRegistrationRequest(INetChannel senderConnection)
        {
            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var message = netMgr.CreateNetMessage<MsgConCmdReg>();

            var counter = 0;
            message.Commands = new MsgConCmdReg.Command[AvailableCommands.Count];
            foreach (var command in AvailableCommands.Values)
            {
                message.Commands[counter++] = new MsgConCmdReg.Command()
                {
                    Name = command.Command,
                    Description = command.Description,
                    Help = command.Help
                };
            }
            
            netMgr.ServerSendMessage(message, senderConnection);
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

        public void ProcessCommand(MsgConCmd message)
        {
            string text = message.Text;
            INetChannel sender = message.MsgChannel;
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
                command.Execute(this, sender, args.ToArray());
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

        public void SendConsoleReply(string text, INetChannel target)
        {
            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var replyMsg = netMgr.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            netMgr.ServerSendMessage(replyMsg, target);
        }
    }
}
