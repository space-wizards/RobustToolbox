using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <inheritdoc cref="IServerConsoleHost"/>
    internal class ServerConsoleHost : ConsoleHost, IServerConsoleHost
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;
        [Dependency] private readonly IConGroupController _groupController = default!;
        
        private void HandleRegistrationRequest(INetChannel senderConnection)
        {
            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var message = netMgr.CreateNetMessage<MsgConCmdReg>();

            var counter = 0;
            message.Commands = new MsgConCmdReg.Command[RegisteredCommands.Count];
            foreach (var command in RegisteredCommands.Values)
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
            NetManager.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, ProcessCommand);
            NetManager.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME);
            NetManager.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME,
                message => HandleRegistrationRequest(message.MsgChannel));
        }

        private void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);

            LogManager.GetSawmill(SawmillName).Info($"{FormatPlayerString(session)}:{text}");

            ExecuteCommand(session, text);
        }

        /// <inheritdoc />
        public override void ExecuteCommand(string command)
        {
            ExecuteCommand(null, command);
        }

        /// <inheritdoc />
        public override void ExecuteCommand(ICommonSession? session, string command)
        {
            var svSession = session as IPlayerSession;
            try
            {
                var args = new List<string>();
                CommandParsing.ParseArguments(command, args);

                // missing cmdName
                if (args.Count == 0)
                    return;

                var cmdName = args[0];

                if (AvailableCommands.TryGetValue(cmdName, out var conCmd)) // command registered
                {
                    if (svSession != null) // remote client
                    {
                        if (_groupController.CanCommand(svSession, cmdName)) // client has permission
                        {
                            args.RemoveAt(0);
                            conCmd.Execute(new ConsoleShell(this, session), command, args.ToArray());
                        }
                        else
                            SendText(svSession, $"Unknown command: '{cmdName}'");
                    }
                    else // system console
                    {
                        args.RemoveAt(0);
                        conCmd.Execute(new ConsoleShell(this, null), command, args.ToArray());
                    }
                }
                else
                    SendText(svSession, $"Unknown command: '{cmdName}'");
            }
            catch (Exception e)
            {
                LogManager.GetSawmill(SawmillName).Warning($"{FormatPlayerString(svSession)}: ExecuteError - {command}:\n{e}");
                SendText(svSession, $"There was an error while executing the command: {e}");
            }
        }

        public override void RemoteExecuteCommand(ICommonSession? session, string command)
        {
            //TODO: Server -> Client remote execute, just like how the client forwards the command
        }

        /// <summary>
        /// Sends a text string to the remote player.
        /// </summary>
        /// <param name="session">
        /// Remote player to send the text message to. If this is null, the text is sent to the local
        /// console.
        /// </param>
        /// <param name="text">Text message to send.</param>
        private void SendText(IPlayerSession? session, string text)
        {
            if (session != null)
                SendText(session.ConnectedClient, text);
            else
                _systemConsole.Print(text + "\n");
        }

        /// <summary>
        /// Sends a text string to the remote console.
        /// </summary>
        /// <param name="target">Net channel to send the text string to.</param>
        /// <param name="text">Text message to send.</param>
        private void SendText(INetChannel target, string text)
        {
            var replyMsg = NetManager.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            NetManager.ServerSendMessage(replyMsg, target);
        }
        
        private static string FormatPlayerString(IPlayerSession? session)
        {
            return session != null ? $"{session.Name}" : "[HOST]";
        }

        private class SudoCommand : IConsoleCommand
        {
            public string Command => "sudo";
            public string Description => "sudo make me a sandwich";
            public string Help => "sudo";

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                var command = args[0];
                var cArgs = args[1..].Select(CommandParsing.Escape);

                var localShell = shell.ConsoleHost.LocalShell;
                localShell.ExecuteCommand($"{command} {string.Join(' ', cArgs)}");
            }
        }

        IConsoleShell IConsoleHost.LocalShell => LocalShell;

        /// <inheritdoc />
        public override IConsoleShell GetSessionShell(ICommonSession session)
        {
            if (session.Status >= SessionStatus.Disconnected)
                throw new InvalidOperationException("Tried to get the session shell of a disconnected peer.");

            return new ConsoleShell(this, session);
        }

        public override void WriteLine(ICommonSession? session, string text)
        {
            if (session is IPlayerSession playerSession)
            {
                SendText(playerSession, text);
            }
            else
            {
                SendText(null as IPlayerSession, text);
            }
        }

        public override void WriteLine(ICommonSession? session, string text, Color color)
        {
            //TODO: Make colors work.
            WriteLine(session, text);
        }

        public override void ClearLocalConsole()
        {
            throw new NotImplementedException();
        }
    }
}
