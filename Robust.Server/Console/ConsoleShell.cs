using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Network.Messages;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <inheritdoc />
    internal class ConsoleShell : IConsoleShell
    {
        private const string SawmillName = "con";

        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IServerNetManager _net = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;
        [Dependency] private readonly ILogManager _logMan = default!;
        [Dependency] private readonly IConGroupController _groupController = default!;

        private readonly Dictionary<string, IClientCommand> _availableCommands =
            new Dictionary<string, IClientCommand>();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IClientCommand> AvailableCommands => _availableCommands;

        private void HandleRegistrationRequest(INetChannel senderConnection)
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

        /// <inheritdoc />
        public void Initialize()
        {
            ReloadCommands();

            // setup networking with clients
            _net.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, ProcessCommand);
            _net.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME);
            _net.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME,
                message => HandleRegistrationRequest(message.MsgChannel));
        }

        /// <inheritdoc />
        public void ReloadCommands()
        {
            // search for all client commands in all assemblies, and register them
            _availableCommands.Clear();
            foreach (var type in _reflectionManager.GetAllChildren<IClientCommand>())
            {
                var instance = (IClientCommand) Activator.CreateInstance(type, null)!;
                if (AvailableCommands.TryGetValue(instance.Command, out var duplicate))
                    throw new InvalidImplementationException(instance.GetType(), typeof(IClientCommand),
                        $"Command name already registered: {instance.Command}, previous: {duplicate.GetType()}");

                _availableCommands[instance.Command] = instance;
            }
        }

        private void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);

            _logMan.GetSawmill(SawmillName).Info($"{FormatPlayerString(session)}:{text}");

            ExecuteCommand(session, text);
        }

        /// <inheritdoc />
        public void ExecuteCommand(string command)
        {
            ExecuteCommand(null, command);
        }

        /// <inheritdoc />
        public void ExecuteCommand(IPlayerSession? session, string command)
        {
            try
            {
                var args = new List<string>();
                CommandParsing.ParseArguments(command, args);

                // missing cmdName
                if (args.Count == 0)
                    return;

                var cmdName = args[0];

                if (_availableCommands.TryGetValue(cmdName, out var conCmd)) // command registered
                {
                    if (session != null) // remote client
                    {
                        if (_groupController.CanCommand(session, cmdName)) // client has permission
                        {
                            args.RemoveAt(0);
                            conCmd.Execute(this, session, args.ToArray());
                        }
                        else
                            SendText(session, $"Unknown command: '{cmdName}'");
                    }
                    else // system console
                    {
                        args.RemoveAt(0);
                        conCmd.Execute(this, null, args.ToArray());
                    }
                }
                else
                    SendText(session, $"Unknown command: '{cmdName}'");
            }
            catch (Exception e)
            {
                _logMan.GetSawmill(SawmillName).Warning($"{FormatPlayerString(session)}: ExecuteError - {command}:\n{e}");
                SendText(session, $"There was an error while executing the command: {e}");
            }
        }

        /// <inheritdoc />
        public void SendText(IPlayerSession? session, string? text)
        {
            if (session != null)
                SendText(session.ConnectedClient, text);
            else
                _systemConsole.Print(text + "\n");
        }

        /// <inheritdoc />
        public void SendText(INetChannel target, string? text)
        {
            var replyMsg = _net.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            _net.ServerSendMessage(replyMsg, target);
        }

        private static string FormatPlayerString(IPlayerSession? session)
        {
            return session != null ? $"{session.Name}" : "[HOST]";
        }

        private class SudoCommand : IClientCommand
        {
            public string Command => "sudo";
            public string Description => "sudo make me a sandwich";
            public string Help => "sudo";

            public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
            {
                var command = args[0];
                var cArgs = args[1..].Select(CommandParsing.Escape);

                shell.ExecuteCommand($"{command} {string.Join(' ', cArgs)}");
            }
        }
    }
}
