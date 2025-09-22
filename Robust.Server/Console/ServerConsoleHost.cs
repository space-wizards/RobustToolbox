using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <inheritdoc cref="IServerConsoleHost" />
    [Virtual]
    internal class ServerConsoleHost : ConsoleHost, IServerConsoleHost, IConsoleHostInternal
    {
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;
        [Dependency] private readonly ToolshedManager _toolshed = default!;

        public ServerConsoleHost() : base(isServer: true) {}

        public override event ConAnyCommandCallback? AnyCommandExecuted;

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

        /// <inheritdoc />
        public void Initialize()
        {
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
                return CalcCompletionsOrEmpty(sudoShell, args, argStr);
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

                if (RegisteredCommands.TryGetValue(cmdName, out var conCmd)) // command registered
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
                else
                {
                    // toolshed time
                    _toolshed.InvokeCommand(shell, command, null, out var res, out var ctx);

                    bool anyErrors = false;
                    foreach (var err in ctx.GetErrors())
                    {
                        anyErrors = true;
                        ctx.WriteLine(err.Describe());
                    }

                    // why does ctx not have any write-error support?
                    if (anyErrors)
                        shell.WriteError($"Failed to execute toolshed command");

                    shell.WriteLine(FormattedMessage.FromMarkupPermissive(_toolshed.PrettyPrintType(res, out var more, moreUsed: true)));
                    ctx.WriteVar("more", more);
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
            return shell.Player == null || _groupController.CanCommand(shell.Player, cmdName);
        }

        private void HandleRegistrationRequest(INetChannel senderConnection)
        {
            var message = new MsgConCmdReg();

            var toolshedCommands = _toolshed.DefaultEnvironment.AllCommands();
            message.Commands = new List<MsgConCmdReg.Command>(AvailableCommands.Count + toolshedCommands.Count);
            var commands = new HashSet<string>();

            foreach (var command in AvailableCommands.Values)
            {
                if (!commands.Add(command.Command))
                {
                    Sawmill.Error($"Duplicate command: {command.Command}");
                    continue;
                }
                message.Commands.Add(new MsgConCmdReg.Command
                {
                    Name = command.Command,
                    Description = command.Description,
                    Help = command.Help
                });
            }

            foreach (var spec in toolshedCommands)
            {
                var name = spec.FullName();
                if (!commands.Add(name))
                {
                    Sawmill.Warning($"Duplicate toolshed command: {name}");
                    continue;
                }

                message.Commands.Add(new MsgConCmdReg.Command
                {
                    Name = name,
                    Description = spec.Cmd.Description(spec.SubCommand),
                    Help = spec.Cmd.GetHelp(spec.SubCommand)
                });
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

            if ((result == null) || message.Args.Length <= 1)
            {
                var shedRes = _toolshed.GetCompletions(shell, message.ArgString);
                if (shedRes == null)
                    goto done;

                IEnumerable<CompletionOption> options = result?.Options ?? Array.Empty<CompletionOption>();

                options = options.Concat(shedRes.Options);

                var hints = result?.Hint ?? shedRes.Hint;

                result = new CompletionResult(options.ToArray(), hints);
            }

            done:

            result ??= CompletionResult.Empty;

            var msg = new MsgConCompletionResp
            {
                Result = result,
                Seq = message.Seq
            };

            if (!message.MsgChannel.IsConnected)
                return;

            NetManager.ServerSendMessage(msg, message.MsgChannel);
        }

        private async ValueTask<CompletionResult> CalcCompletionsOrEmpty(IConsoleShell shell, string[] args, string argStr)
        {
            return await CalcCompletions(shell, args, argStr) ?? CompletionResult.Empty;
        }

        /// <summary>
        /// Get completions. Non-null results imply that the command was handled. If it is empty, it implies that
        /// there are no completions for this command.
        /// </summary>
        private async ValueTask<CompletionResult?> CalcCompletions(IConsoleShell shell, string[] args, string argStr)
        {
            // Logger.Debug(string.Join(", ", args));

            if (args.Length <= 1)
            {
                // Typing out command name, handle this ourselves.
                return CompletionResult.FromOptions(
                    AvailableCommands.Values.Where(c => ShellCanExecute(shell, c.Command)).Select(c => new CompletionOption(c.Command, c.Description)));
            }

            var cmdName = args[0];
            if (!RegisteredCommands.TryGetValue(cmdName, out var cmd))
                return null;

            if (!ShellCanExecute(shell, cmdName))
                return CompletionResult.Empty;

            return await cmd.GetCompletionAsync(shell, args[1..], argStr, CancellationToken.None);
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
