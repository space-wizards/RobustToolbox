using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <inheritdoc cref="IServerConsoleHost" />
    [Virtual]
    internal class ServerConsoleHost : ConsoleHost, IServerConsoleHost, IConsoleHostInternal
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;

        public ServerConsoleHost() : base(isServer: true) {}

        /// <inheritdoc />
        public override void ExecuteCommand(ICommonSession? session, string command)
        {
            var shell = new ConsoleShell(this, session, session == null);
            ExecuteInShell(shell, command);
        }

        /// <inheritdoc />
        public override void RemoteExecuteCommand(ICommonSession? session, string command)
        {
            if (!NetManager.IsConnected || session is null)
                return;

            var msg = new MsgConCmd();
            msg.Text = command;
            NetManager.ServerSendMessage(msg, session.Channel);
        }

        /// <inheritdoc />
        public override void WriteLine(ICommonSession? session, string text)
        {
            var msg = new FormattedMessage();
            msg.AddText(text);
            OutputText(session, msg, false);
        }

        public override void WriteLine(ICommonSession? session, FormattedMessage msg)
        {
            OutputText(session, msg, false);
        }

        /// <inheritdoc />
        public override void WriteError(ICommonSession? session, string text)
        {
            var msg = new FormattedMessage();
            msg.AddText(text);
            OutputText(session, msg, true);
        }

        public bool IsCmdServer(IConsoleCommand cmd) => true;

        public override void Initialize()
        {
            base.Initialize();

            RegisterCommand("sudo", "sudo make me a sandwich", "sudo <command>", (shell, argStr, _) =>
            {
                var localShell = shell.ConsoleHost.LocalShell;
                var sudoShell = new SudoShell(this, localShell, shell);
                ExecuteInShell(sudoShell, argStr["sudo ".Length..]);
            }, (shell, args, argStr) =>
            {
                var localShell = shell.ConsoleHost.LocalShell;
                var sudoShell = new SudoShell(this, localShell, shell);

#pragma warning disable CA2012
                return CalcCompletions(sudoShell, args, argStr);
#pragma warning restore CA2012
            });

            LoadConsoleCommands();

            // setup networking with clients
            NetManager.RegisterNetMessage<MsgConCmd>(ProcessCommand);
            NetManager.RegisterNetMessage<MsgConCmdAck>();

            NetManager.RegisterNetMessage<MsgConCmdReg>(message => HandleRegistrationRequest(message.MsgChannel));
            NetManager.RegisterNetMessage<MsgConCompletion>(HandleConCompletions);
            NetManager.RegisterNetMessage<MsgConCompletionResp>();
        }

        private void HandleRegistrationRequest(INetChannel senderConnection)
        {
            var message = new MsgConCmdReg();

            message.Commands = new List<MsgConCmdReg.Command>(RegisteredCommands.Count);
            var commands = new HashSet<string>();

            foreach (var command in RegisteredCommands.Values)
            {
                var cmdName = command.Command;
                if (!commands.Add(cmdName))
                {
                    Sawmill.Error($"Duplicate command: {cmdName}");
                    continue;
                }
                message.Commands.Add(new MsgConCmdReg.Command
                {
                    Name = cmdName,
                    Description = command.Description,
                    Help = command.Help
                });
            }

            NetManager.ServerSendMessage(message, senderConnection);
        }

        private void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);
            Sawmill.Info($"{FormatPlayerString(session)}:{text}");
            ExecuteCommand(session, text);
        }

        private void OutputText(ICommonSession? session, FormattedMessage text, bool error)
        {
            if (session != null)
            {
                var replyMsg = new MsgConCmdAck();
                replyMsg.Error = error;
                replyMsg.Text = text;
                NetManager.ServerSendMessage(replyMsg, session.Channel);
            }
            else
                _systemConsole.Print(text + "\n");
        }

        private static string FormatPlayerString(ICommonSession? session)
        {
            return session != null ? $"{session.Name}" : "[HOST]";
        }

        private async void HandleConCompletions(MsgConCompletion message)
        {
            var session = _players.GetSessionByChannel(message.MsgChannel);
            var shell = new ConsoleShell(this, session, false);
            var result = await CalcCompletions(shell, message.Args, message.ArgString);

            var msg = new MsgConCompletionResp
            {
                Result = result,
                Seq = message.Seq
            };

            if (!message.MsgChannel.IsConnected)
                return;

            NetManager.ServerSendMessage(msg, message.MsgChannel);
        }

        private async ValueTask<CompletionResult> CalcCompletions(IConsoleShell shell, string[] args, string argStr)
        {
            return await CalcCompletions(shell, args, argStr, CancellationToken.None);
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
            public bool IsLocal => _owner.IsLocal;

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

            public void WriteLine(FormattedMessage message)
            {
                _owner.WriteLine(message);
                _sudoer.WriteLine(message);
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
