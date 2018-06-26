using System;
using System.Collections.Generic;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Console;
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
        [Dependency]
        private readonly ISystemConsoleManager _systemConsole;


        private readonly Dictionary<string, IClientCommand> availableCommands = new Dictionary<string, IClientCommand>();
        public IReadOnlyDictionary<string, IClientCommand> AvailableCommands => availableCommands;

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

                availableCommands[instance.Command] = instance;
            }

            _net.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, ProcessCommand);
            _net.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME);
            _net.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME, message => HandleRegistrationRequest(message.MsgChannel));
        }

        public void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);

            Logger.Info($"[CON] {FormatPlayerString(session)}:{text}");

            ExecuteCommand(session, text);
        }

        public void ExecuteHostCommand(string command)
        {
            ExecuteCommand(null, command);
        }

        public void ExecuteCommand(IPlayerSession session, string command)
        {
            try
            {
                var args = new List<string>();
                CommandParsing.ParseArguments(command, args);

                if (args.Count == 0)
                    return;
                var cmdName = args[0];

                if (availableCommands.TryGetValue(cmdName, out var conCmd))
                {
                    //TODO: Authentication

                    args.RemoveAt(0);
                    conCmd.Execute(this, session, args.ToArray());
                }
                else
                    SendText(session, $"Unknown command: '{cmdName}'");
            }
            catch (Exception e)
            {
                Logger.Warning($"[CON] {FormatPlayerString(session)}: ExecuteError - {command}");
                SendText(session, $"There was an error while executing the command: {e.Message}");
            }
        }

        public void SendText(IPlayerSession session, string text)
        {
            if (session != null)
                SendText(session, text);
            else
                _systemConsole.Print(text + "\n");
        }

        public void SendConsoleText(INetChannel target, string text)
        {
            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var replyMsg = netMgr.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            netMgr.ServerSendMessage(replyMsg, target);
        }

        private string FormatPlayerString(IPlayerSession session)
        {
            return session != null ? $"[{session.Index}]{session.Name}" : "[HOST]";
        }
    }
}
