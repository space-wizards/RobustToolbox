using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <inheritdoc cref="IServerConsoleHost" />
    internal sealed class ServerConsoleHost : ConsoleHost, IServerConsoleHost, IConsoleHostInternal
    {
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;

        public ServerConsoleHost() : base(isServer: true) {}

        public override event ConAnyCommandCallback? AnyCommandExecuted;

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

            var msg = new MsgConCmd();
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

        public bool IsCmdServer(IConsoleCommand cmd) => true;

        /// <inheritdoc />
        public void Initialize()
        {
            RegisterCommand("sudo", "sudo make me a sandwich", "sudo <command>", (shell, argStr, _) =>
            {
                var localShell = shell.ConsoleHost.LocalShell;
                var sudoShell = new SudoShell(this, localShell, shell);
                ExecuteInShell(sudoShell, argStr["sudo ".Length..]);
            }, (shell, args) =>
            {
                var localShell = shell.ConsoleHost.LocalShell;
                var sudoShell = new SudoShell(this, localShell, shell);

#pragma warning disable CA2012
                return CalcCompletions(sudoShell, args);
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
                    args.RemoveAt(0);
                    var cmdArgs = args.ToArray();
                    if (!ShellCanExecute(shell, cmdName))
                    {
                        shell.WriteError($"Unknown command: '{cmdName}'");
                        return;
                    }

                    AnyCommandExecuted?.Invoke(shell, cmdName, command, cmdArgs);
                    conCmd.Execute(shell, command, cmdArgs);
                }
            }
            catch (Exception e)
            {
                LogManager.GetSawmill(SawmillName)
                    .Error($"{FormatPlayerString(shell.Player)}: ExecuteError - {command}:\n{e}");
                shell.WriteError($"There was an error while executing the command: {e}");
            }
        }

        private bool ShellCanExecute(IConsoleShell shell, string cmdName)
        {
            return shell.Player == null || _groupController.CanCommand((IPlayerSession)shell.Player, cmdName);
        }

        private void HandleRegistrationRequest(INetChannel senderConnection)
        {
            var message = new MsgConCmdReg();

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

            NetManager.ServerSendMessage(message, senderConnection);
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
                var replyMsg = new MsgConCmdAck();
                replyMsg.Error = error;
                replyMsg.Text = text;
                NetManager.ServerSendMessage(replyMsg, session.ConnectedClient);
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
            var shell = new ConsoleShell(this, session);

            var result = await CalcCompletions(shell, message.Args);

            var msg = new MsgConCompletionResp
            {
                Result = result,
                Seq = message.Seq
            };

            if (!message.MsgChannel.IsConnected)
                return;

            NetManager.ServerSendMessage(msg, message.MsgChannel);
        }

        private ValueTask<CompletionResult> CalcCompletions(IConsoleShell shell, string[] args)
        {
            // Logger.Debug(string.Join(", ", args));

            if (args.Length <= 1)
            {
                // Typing out command name, handle this ourselves.
                return ValueTask.FromResult(CompletionResult.FromOptions(
                    RegisteredCommands.Values.Where(c => ShellCanExecute(shell, c.Command)).Select(c => new CompletionOption(c.Command, c.Description))));
            }

            var cmdName = args[0];
            if (!AvailableCommands.TryGetValue(cmdName, out var cmd))
                return ValueTask.FromResult(CompletionResult.Empty);

            if (!ShellCanExecute(shell, cmdName))
                return ValueTask.FromResult(CompletionResult.Empty);

            return cmd.GetCompletionAsync(shell, args[1..], default);
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
