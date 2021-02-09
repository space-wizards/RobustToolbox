using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <inheritdoc cref="IServerConsoleHost" />
    internal class ServerConsoleHost : ConsoleHost, IServerConsoleHost
    {
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;

        /// <inheritdoc />
        public override void ExecuteCommand(ICommonSession? session, string command)
        {
            var shell = new ConsoleShell(this, session);
            ExecuteInShell(shell, command);
        }

        /// <inheritdoc />
        public override void RemoteExecuteCommand(ICommonSession? session, string command)
        {
            if (!NetManager.IsConnected || session is null)
                return;

            var msg = NetManager.CreateNetMessage<MsgConCmd>();
            msg.Text = command;
            NetManager.ServerSendMessage(msg, ((IPlayerSession)session).ConnectedClient);
        }

        /// <inheritdoc />
        public override void WriteLine(ICommonSession? session, string text)
        {
            if (session is IPlayerSession playerSession)
                OutputText(playerSession, text, false);
            else
                OutputText(null, text, false);
        }

        /// <inheritdoc />
        public override void WriteError(ICommonSession? session, string text)
        {
            if (session is IPlayerSession playerSession)
                OutputText(playerSession, text, true);
            else
                OutputText(null, text, true);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            RegisterCommand("sudo", "sudo make me a sandwich", "sudo <command>",(shell, _, args) =>
            {
                string command = args[0];
                var cArgs = args[1..].Select(CommandParsing.Escape).Select(c => $"\"{c}\"");

                var localShell = shell.ConsoleHost.LocalShell;
                var sudoShell = new SudoShell(this, localShell, shell);
                ExecuteInShell(sudoShell, $"{command} {string.Join(' ', cArgs)}");
            });
            
            LoadConsoleCommands();

            // setup networking with clients
            NetManager.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, ProcessCommand);
            NetManager.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME);

            NetManager.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME,
                message => HandleRegistrationRequest(message.MsgChannel));
        }

        private void ExecuteInShell(IConsoleShell shell, string command)
        {
            try
            {
                var args = new List<string>();
                CommandParsing.ParseArguments(command, args);

                // missing cmdName
                if (args.Count == 0)
                    return;

                string? cmdName = args[0];

                if (AvailableCommands.TryGetValue(cmdName, out var conCmd)) // command registered
                {
                    if (shell.Player != null) // remote client
                    {
                        if (_groupController.CanCommand((IPlayerSession) shell.Player, cmdName)) // client has permission
                        {
                            args.RemoveAt(0);
                            conCmd.Execute(shell, command, args.ToArray());
                        }
                        else
                            shell.WriteError($"Unknown command: '{cmdName}'");
                    }
                    else // system console
                    {
                        args.RemoveAt(0);
                        conCmd.Execute(shell, command, args.ToArray());
                    }
                }
                else
                    shell.WriteError($"Unknown command: '{cmdName}'");
            }
            catch (Exception e)
            {
                LogManager.GetSawmill(SawmillName).Warning($"{FormatPlayerString(shell.Player)}: ExecuteError - {command}:\n{e}");
                shell.WriteError($"There was an error while executing the command: {e}");
            }
        }

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

        private void ProcessCommand(MsgConCmd message)
        {
            string? text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);

            LogManager.GetSawmill(SawmillName).Info($"{FormatPlayerString(session)}:{text}");

            ExecuteCommand(session, text);
        }

        private void OutputText(IPlayerSession? session, string text, bool error)
        {
            if (session != null)
            {
                var replyMsg = NetManager.CreateNetMessage<MsgConCmdAck>();
                replyMsg.Error = error;
                replyMsg.Text = text;
                NetManager.ServerSendMessage(replyMsg, session.ConnectedClient);
            }
            else
                _systemConsole.Print(text + "\n");
        }

        private static string FormatPlayerString(IBaseSession? session)
        {
            return session != null ? $"{session.Name}" : "[HOST]";
        }

        private sealed class SudoShell : IConsoleShell
        {
            private readonly ServerConsoleHost _host;
            private readonly IConsoleShell _owner;
            private readonly IConsoleShell _sudoer;

            public SudoShell(ServerConsoleHost host, IConsoleShell owner, IConsoleShell sudoer)
            {
                _host = host;
                _owner = owner;
                _sudoer = sudoer;
            }

            public IConsoleHost ConsoleHost => _host;
            public bool IsServer => _owner.IsServer;
            public ICommonSession? Player => _owner.Player;

            public void ExecuteCommand(string command)
            {
                _host.ExecuteInShell(this, command);
            }

            public void RemoteExecuteCommand(string command)
            {
                _owner.RemoteExecuteCommand(command);
            }

            public void WriteLine(string text)
            {
                _owner.WriteLine(text);
                _sudoer.WriteLine(text);
            }

            public void WriteError(string text)
            {
                _owner.WriteError(text);
                _sudoer.WriteError(text);
            }

            public void Clear()
            {
                _owner.Clear();
                _sudoer.Clear();
            }
        }
    }
}
