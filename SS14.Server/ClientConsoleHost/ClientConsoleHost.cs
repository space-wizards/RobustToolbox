using System;
using System.Collections.Generic;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using SS14.Shared.Utility;

namespace SS14.Server.ClientConsoleHost
{
    public class ClientConsoleHost : IClientConsoleHost
    {
        [Dependency]
        private readonly IReflectionManager reflectionManager;
        [Dependency]
        private readonly IPlayerManager _players;
        [Dependency]
        private readonly IServerNetManager _net;


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
                message.Commands[counter++] = new MsgConCmdReg.Command
                {
                    Name = command.Command,
                    Description = command.Description,
                    Help = command.Help
                };
            }

            netMgr.ServerSendMessage(message, senderConnection);
        }

        public void Initialize()
        {
            foreach (var type in reflectionManager.GetAllChildren<IClientCommand>())
            {
                var instance = Activator.CreateInstance(type, null) as IClientCommand;
                if (AvailableCommands.TryGetValue(instance.Command, out var duplicate))
                    throw new InvalidImplementationException(instance.GetType(), typeof(IClientCommand), $"Command name already registered: {instance.Command}, previous: {duplicate.GetType()}");

                AvailableCommands[instance.Command] = instance;
            }

            _net.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, (int)MsgConCmd.ID, message => ProcessCommand((MsgConCmd)message));
            _net.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME, (int)MsgConCmdAck.ID);
            _net.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME, (int)MsgConCmdReg.ID, message => HandleRegistrationRequest(message.MsgChannel));
        }

        public void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);
            var args = new List<string>();

            Logger.Info($"[{(int)session.Index}]{session.Name}:{text}");

            CommandParsing.ParseArguments(text, args);

            if (args.Count == 0)
                return;
            var cmd = args[0];

            try
            {
                if (availableCommands.TryGetValue(cmd, out var command))
                {
                    args.RemoveAt(0);
                    command.Execute(this, session, args.ToArray());
                }
                else
                    SendConsoleReply(sender, $"Unknown command: '{cmd}'");
            }
            catch (Exception e)
            {
                SendConsoleReply(sender, $"There was an error while executing the command: {e.Message}");
            }
        }

        public void SendConsoleReply(INetChannel target, string text)
        {
            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var replyMsg = netMgr.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            netMgr.ServerSendMessage(replyMsg, target);
        }
    }
}
