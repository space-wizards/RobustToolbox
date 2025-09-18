using System;
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
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Client.Console
{
    public sealed class AddStringArgs : EventArgs
    {
        public FormattedMessage Text { get; }

        public bool Local { get; }

        public bool Error { get; }

        public AddStringArgs(FormattedMessage text, bool local, bool error)
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
    [Virtual]
    internal partial class ClientConsoleHost : ConsoleHost, IClientConsoleHost, IConsoleHostInternal
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IBaseClient _client = default!;

        private bool _requestedCommands;

        /// <summary>
        /// Sawmill associated with the console. Text that is manually written to the console should be
        /// logged via this logger.
        /// </summary>
        /// <remarks>
        /// The console also automatically echoes most logged messages, but will exempt any coming from this
        /// logger to avoid duplicates.
        /// </remarks>
        private ISawmill _conLogger = default!;
        public const string ConsoleSawmill = "CON";

        public ClientConsoleHost() : base(isServer: false) {}

        public override void Initialize()
        {
            base.Initialize();
            _conLogger = LogManager.GetSawmill(ConsoleSawmill);

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

        private void OnLevelChanged(object? sender, RunLevelChangedEventArgs e)
        {
            UpdateAvailableCommands();
        }

        private void OnNetworkDisconnected(object? sender, NetDisconnectedArgs e)
        {
            RemoteCommands.Clear();
            _requestedCommands = false;
            UpdateAvailableCommands();
        }

        protected override bool IsAvailable(IConsoleCommand cmd)
        {
            if (!base.IsAvailable(cmd))
                return false;

            return !cmd.RequireServerOrSingleplayer
                   || (_client.RunLevel is ClientRunLevel.Initialize or ClientRunLevel.SinglePlayerGame);
        }

        private void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            Sawmill.Info($"SERVER:{text}");
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

        public override void WriteLine(ICommonSession? session, FormattedMessage msg)
        {
            AddFormattedLine(msg);
        }

        /// <inheritdoc />
        public override void WriteError(ICommonSession? session, string text)
        {
            var msg = new FormattedMessage();
            msg.AddText(text);
            OutputText(msg, true, true);
        }

        public bool IsCmdServer(IConsoleCommand cmd)
        {
            return cmd is ServerDummyCommand;
        }

        /// <inheritdoc />
        public override void ExecuteCommand(ICommonSession? session, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            WriteLine(null, "");
            var msg = new FormattedMessage();
            msg.PushColor(Color.Gold);
            msg.AddText("> " + command);
            msg.Pop();
            // echo the command locally
            OutputText(msg, true, false);

            var shell = new ConsoleShell(this, session ?? _player.LocalSession, session == null);
            ExecuteInShell(shell, command);
        }

        protected override bool ShellCanExecute(IConsoleShell shell, IConsoleCommand cmd)
        {
            return shell.Player is not {Status: > SessionStatus.Connecting}
                   || base.ShellCanExecute(shell, cmd);
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
            var msg = new FormattedMessage();
            msg.AddText(text);
            OutputText(msg, true, false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // We don't have anything to dispose.
        }

        private void OutputText(FormattedMessage text, bool local, bool error)
        {
            AddString?.Invoke(this, new AddStringArgs(text, local, error));

            var level = error ? LogLevel.Warning : LogLevel.Info;
            _conLogger.Log(level, text.ToString());
        }

        private void OnNetworkConnected(object? sender, NetChannelArgs netChannelArgs)
        {
            SendServerCommandRequest();
        }

        private void HandleConCmdAck(MsgConCmdAck msg)
        {
            OutputText(msg.Text, false, msg.Error);
        }

        private void HandleConCmdReg(MsgConCmdReg msg)
        {
            foreach (var cmd in msg.Commands)
            {
                var commandName = cmd.Name;

                // Do not do duplicate commands.
                if (RemoteCommands.ContainsKey(commandName))
                {
                    Sawmill.Error($"Server sent duplicate console command {commandName}");
                    continue;
                }

                var command = new ServerDummyCommand(commandName, cmd.Help, cmd.Description);
                RemoteCommands[commandName] = command;
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
                string argStr,
                CancellationToken cancel)
            {
                var host = (ClientConsoleHost)shell.ConsoleHost;
                var argsList = args.ToList();
                argsList.Insert(0, Command);

                return await host.DoServerCompletions(argsList, argStr, cancel);
            }
        }

        private sealed class RemoteExecCommand : LocalizedCommands
        {
            public override string Command => ">";
            public override string Description => LocalizationManager.GetString("cmd-remoteexec-desc");
            public override string Help => LocalizationManager.GetString("cmd-remoteexec-help");

            public override void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                shell.RemoteExecuteCommand(argStr[">".Length..]);
            }

            public override async ValueTask<CompletionResult> GetCompletionAsync(
                IConsoleShell shell,
                string[] args,
                string argStr,
                CancellationToken cancel)
            {
                var host = (ClientConsoleHost)shell.ConsoleHost;
                return await host.DoServerCompletions(args.ToList(), argStr[">".Length..], cancel);
            }
        }
    }
}
