using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <inheritdoc cref="IServerConsoleHost" />
    internal class ServerConsoleHost : ConsoleHost, IServerConsoleHost
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;
        [Dependency] private readonly IConGroupController _groupController = default!;

        /// <inheritdoc />
        public override void ExecuteCommand(ICommonSession? session, string command)
        {
            var shell = new ConsoleShell(this, session);
            ExecuteInShell(shell, command);
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

                var cmdName = args[0];

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
                            shell.WriteLine($"Unknown command: '{cmdName}'");
                    }
                    else // system console
                    {
                        args.RemoveAt(0);
                        conCmd.Execute(shell, command, args.ToArray());
                    }
                }
                else
                    shell.WriteLine($"Unknown command: '{cmdName}'");
            }
            catch (Exception e)
            {
                LogManager.GetSawmill(SawmillName).Warning($"{FormatPlayerString(shell.Player)}: ExecuteError - {command}:\n{e}");
                shell.WriteLine($"There was an error while executing the command: {e}");
            }
        }

        public override void RemoteExecuteCommand(ICommonSession? session, string command)
        {
            //TODO: Server -> Client remote execute, just like how the client forwards the command
        }

        public override void WriteLine(ICommonSession? session, string text)
        {
            if (session is IPlayerSession playerSession)
                SendText(playerSession, text);
            else
                SendText(null as IPlayerSession, text);
        }

        public override void WriteLine(ICommonSession? session, string text, Color color)
        {
            //TODO: Make colors work.
            WriteLine(session, text);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            RegisterCommand("sudo", "sudo make me a sandwich", "sudo <command>", (shell, _, args) =>
            {
                var command = args[0];
                var cArgs = args[1..].Select(CommandParsing.Escape);

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
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);

            LogManager.GetSawmill(SawmillName).Info($"{FormatPlayerString(session)}:{text}");

            ExecuteCommand(session, text);
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

            public void WriteLine(string text, Color color)
            {
                _owner.WriteLine(text, color);
                _sudoer.WriteLine(text, color);
            }

            public void Clear()
            {
                _owner.Clear();
                _sudoer.Clear();
            }
        }
    }
}
