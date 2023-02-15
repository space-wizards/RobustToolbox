using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Robust.Client.Log;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Console
{
    public sealed class AddStringArgs : EventArgs
    {
        public string Text { get; }

        public bool Local { get; }

        public bool Error { get; }

        public AddStringArgs(string text, bool local, bool error)
        {
            Text = text;
            Local = local;
            Error = error;
        }
    }

    public sealed class AddFormattedMessageArgs : EventArgs
    {
        public readonly FormattedMessage Message;

        public AddFormattedMessageArgs(FormattedMessage message)
        {
            Message = message;
        }
    }

    /// <inheritdoc cref="IClientConsoleHost" />
    internal sealed partial class ClientConsoleHost : ConsoleHost, IClientConsoleHost, IConsoleHostInternal
    {
        [Dependency] private readonly IClientConGroupController _conGroup = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IBaseClient _client = default!;

        [ViewVariables] private readonly Dictionary<string, IConsoleCommand> _availableServerCommands = new();

        private bool _requestedCommands;

        public ClientConsoleHost() : base(isServer: false) {}

        /// <inheritdoc />
        public void Initialize()
        {
            NetManager.RegisterNetMessage<MsgConCmdReg>(HandleConCmdReg);
            NetManager.RegisterNetMessage<MsgConCmdAck>(HandleConCmdAck);
            NetManager.RegisterNetMessage<MsgConCmd>(ProcessCommand);
            NetManager.RegisterNetMessage<MsgConCompletion>();
            NetManager.RegisterNetMessage<MsgConCompletionResp>(ProcessCompletionResp);

            _requestedCommands = false;
            NetManager.Connected += OnNetworkConnected;
            NetManager.Disconnect += OnNetworkDisconnected;
            _client.RunLevelChanged += OnLevelChanged;

            LoadConsoleCommands();
            SendServerCommandRequest();
            LogManager.RootSawmill.AddHandler(new DebugConsoleLogHandler(this));
        }

        private readonly Dictionary<string, IConsoleCommand> _availableCommands = new();
        public override IReadOnlyDictionary<string, IConsoleCommand> AvailableCommands => _availableCommands;

        private void OnLevelChanged(object? sender, RunLevelChangedEventArgs e)
        {
            UpdateAvailableCommands();
        }

        private void OnNetworkDisconnected(object? sender, NetDisconnectedArgs e)
        {
            _availableServerCommands.Clear();
            _requestedCommands = false;
            UpdateAvailableCommands();
        }

        protected override void UpdateAvailableCommands()
        {
            _availableCommands.Clear();

            foreach (var (name, cmd) in RegisteredCommands)
            {
                if (!cmd.RequireServerOrSingleplayer || (_client.RunLevel is ClientRunLevel.Initialize or ClientRunLevel.SinglePlayerGame))
                    _availableCommands.Add(name, cmd);
            }

            foreach (var (name, cmd) in _availableServerCommands)
            {
                _availableCommands.TryAdd(name, cmd);
            }
        }

        private void ProcessCommand(MsgConCmd message)
        {
            string? text = message.Text;
            LogManager.GetSawmill(SawmillName).Info($"SERVER:{text}");

            ExecuteCommand(null, text);
        }

        /// <inheritdoc />
        public event EventHandler<AddStringArgs>? AddString;

        /// <inheritdoc />
        public event EventHandler<AddFormattedMessageArgs>? AddFormatted;

        /// <inheritdoc />
        public void AddFormattedLine(FormattedMessage message)
        {
            AddFormatted?.Invoke(this, new AddFormattedMessageArgs(message));
        }

        /// <inheritdoc />
        public override void WriteError(ICommonSession? session, string text)
        {
            OutputText(text, true, true);
        }

        public bool IsCmdServer(IConsoleCommand cmd)
        {
            return cmd is ServerDummyCommand;
        }

        public override event ConAnyCommandCallback? AnyCommandExecuted;

        /// <inheritdoc />
        public override void ExecuteCommand(ICommonSession? session, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            // echo the command locally
            WriteLine(null, "> " + command);

            //Commands are processed locally and then sent to the server to be processed there again.
            var args = new List<string>();

            CommandParsing.ParseArguments(command, args);

            var commandName = args[0];

            if (!AvailableCommands.TryGetValue(commandName, out var cmd))
            {
                WriteError(null, "Unknown command: " + commandName);
                return;
            }

            if (!CanExecute(commandName))
            {
                WriteError(null, $"Insufficient perms for command: {commandName}");
                return;
            }

            args.RemoveAt(0);
            var shell = new ConsoleShell(this, null);
            var cmdArgs = args.ToArray();

            AnyCommandExecuted?.Invoke(shell, commandName, command, cmdArgs);
            cmd.Execute(shell, command, cmdArgs);
        }

        private bool CanExecute(string cmdName)
        {
            // When not connected to a server, you can run all local commands.
            // When connected to a server, you can only run commands according to the con group controller.

            return _player.LocalPlayer == null
                   || _player.LocalPlayer.Session.Status <= SessionStatus.Connecting
                   || _conGroup.CanCommand(cmdName);
        }

        /// <inheritdoc />
        public override void RemoteExecuteCommand(ICommonSession? session, string command)
        {
            if (!NetManager.IsConnected) // we don't care about session on client
                return;

            var msg = new MsgConCmd();
            msg.Text = command;
            NetManager.ClientSendMessage(msg);
        }

        /// <inheritdoc />
        public override void WriteLine(ICommonSession? session, string text)
        {
            OutputText(text, true, false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // We don't have anything to dispose.
        }

        private void OutputText(string text, bool local, bool error)
        {
            AddString?.Invoke(this, new AddStringArgs(text, local, error));

            var level = error ? LogLevel.Warning : LogLevel.Info;
            Logger.LogS(level, "CON", text);
        }

        private void OnNetworkConnected(object? sender, NetChannelArgs netChannelArgs)
        {
            SendServerCommandRequest();
        }

        private void HandleConCmdAck(MsgConCmdAck msg)
        {
            OutputText("< " + msg.Text, false, msg.Error);
        }

        private void HandleConCmdReg(MsgConCmdReg msg)
        {
            foreach (var cmd in msg.Commands)
            {
                string? commandName = cmd.Name;

                // Do not do duplicate commands.
                if (_availableServerCommands.ContainsKey(commandName))
                {
                    Logger.Error("console", $"Server sent duplicate console command {commandName}");
                    continue;
                }

                var command = new ServerDummyCommand(commandName, cmd.Help, cmd.Description);
                _availableServerCommands[commandName] = command;
            }

            UpdateAvailableCommands();
        }

        /// <summary>
        /// Requests remote commands from server.
        /// </summary>
        private void SendServerCommandRequest()
        {
            if (_requestedCommands)
                return;

            if (!NetManager.IsConnected)
                return;

            var msg = new MsgConCmdReg();
            NetManager.ClientSendMessage(msg);

            _requestedCommands = true;
        }

        /// <summary>
        /// These dummies are made purely so list and help can list server-side commands.
        /// </summary>
        [Reflect(false)]
        private sealed class ServerDummyCommand : IConsoleCommand
        {
            internal ServerDummyCommand(string command, string help, string description)
            {
                Command = command;
                Help = help;
                Description = description;
            }

            public string Command { get; }

            public string Description { get; }

            public string Help { get; }

            // Always forward to server.
            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                shell.RemoteExecuteCommand(argStr);
            }

            public async ValueTask<CompletionResult> GetCompletionAsync(
                IConsoleShell shell,
                string[] args,
                CancellationToken cancel)
            {
                var host = (ClientConsoleHost)shell.ConsoleHost;
                var argsList = args.ToList();
                argsList.Insert(0, Command);

                return await host.DoServerCompletions(argsList, cancel);
            }
        }

        private sealed class RemoteExecCommand : LocalizedCommands
        {
            public override string Command => ">";
            public override string Description => LocalizationManager.GetString("cmd-remoteexec-desc");
            public override string Help => LocalizationManager.GetString("cmd-remoteexec-help");

            public override void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                shell.RemoteExecuteCommand(argStr["> ".Length..]);
            }

            public override async ValueTask<CompletionResult> GetCompletionAsync(
                IConsoleShell shell,
                string[] args,
                CancellationToken cancel)
            {
                var host = (ClientConsoleHost)shell.ConsoleHost;
                return await host.DoServerCompletions(args.ToList(), cancel);
            }
        }
    }
}
