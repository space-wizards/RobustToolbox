using System;
using System.Collections.Generic;
using Robust.Client.Interfaces.Console;
using Robust.Client.Log;
using Robust.Client.Utility;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using IConsoleCommand = Robust.Client.Interfaces.Console.IConsoleCommand;

namespace Robust.Client.Console
{
    public class AddStringArgs : EventArgs
    {
        public string Text { get; }
        public Color Color { get; }

        public AddStringArgs(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }

    public class AddFormattedMessageArgs : EventArgs
    {
        public readonly FormattedMessage Message;

        public AddFormattedMessageArgs(FormattedMessage message)
        {
            Message = message;
        }
    }

    internal sealed class ClientConsole : IClientConsole, IDebugConsole
    {
        private static readonly Color MsgColor = new Color(65, 105, 225);

        [Dependency] private readonly IClientNetManager _network = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly ILogManager logManager = default!;

        private readonly Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();
        private bool _requestedCommands;

        /// <inheritdoc />
        public void Initialize()
        {
            _network.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME, HandleConCmdReg);
            _network.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME, HandleConCmdAck);
            _network.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME);

            Reset();
            logManager.RootSawmill.AddHandler(new DebugConsoleLogHandler(this));
        }

        /// <inheritdoc />
        public void Reset()
        {
            _commands.Clear();
            _requestedCommands = false;
            _network.Connected += OnNetworkConnected;

            InitializeCommands();
            SendServerCommandRequest();
        }

        private void OnNetworkConnected(object? sender, NetChannelArgs netChannelArgs)
        {
            SendServerCommandRequest();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // We don't have anything to dispose.
        }

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => _commands;

        public void AddLine(string text, Color color)
        {
            AddString?.Invoke(this, new AddStringArgs(text, color));
        }

        public void AddLine(string text)
        {
            AddLine(text, Color.White);
        }

        public void Clear()
        {
            ClearText?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<AddStringArgs>? AddString;
        public event EventHandler? ClearText;
        public event EventHandler<AddFormattedMessageArgs>? AddFormatted;

        private void HandleConCmdAck(MsgConCmdAck msg)
        {
            AddLine("< " + msg.Text, MsgColor);
        }

        private void HandleConCmdReg(MsgConCmdReg msg)
        {
            foreach (var cmd in msg.Commands)
            {
                var commandName = cmd.Name;

                // Do not do duplicate commands.
                if (_commands.ContainsKey(commandName))
                {
                    Logger.DebugS("console", $"Server sent console command {commandName}, but we already have one with the same name. Ignoring.");
                    continue;
                }

                var command = new ServerDummyCommand(commandName, cmd.Help, cmd.Description);
                _commands[commandName] = command;
            }
        }

        /// <summary>
        ///     Processes commands (chat messages starting with /)
        /// </summary>
        /// <param name="text">input text</param>
        public void ProcessCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // echo the command locally
            AddLine("> " + text, Color.Lime);

            //Commands are processed locally and then sent to the server to be processed there again.
            var args = new List<string>();

            CommandParsing.ParseArguments(text, args);

            var commandname = args[0];

            var forward = true;
            if (_commands.ContainsKey(commandname))
            {
                var command = _commands[commandname];
                args.RemoveAt(0);
                forward = command.Execute(this, args.ToArray());
            }
            else if (!_network.IsConnected)
            {
                AddLine("Unknown command: " + commandname, Color.Red);
                return;
            }

            if (forward)
                SendServerConsoleCommand(text);
        }

        /// <summary>
        ///     Locates and registeres all local commands.
        /// </summary>
        private void InitializeCommands()
        {
            foreach (var t in _reflectionManager.GetAllChildren<IConsoleCommand>())
            {
                var instance = (IConsoleCommand)Activator.CreateInstance(t)!;
                if (_commands.ContainsKey(instance.Command))
                    throw new InvalidOperationException($"Command already registered: {instance.Command}");

                _commands[instance.Command] = instance;
            }
        }

        /// <summary>
        ///     Requests remote commands from server.
        /// </summary>
        public void SendServerCommandRequest()
        {
            if (_requestedCommands)
                return;

            if (!_network.IsConnected)
                return;

            var msg = _network.CreateNetMessage<MsgConCmdReg>();
            _network.ClientSendMessage(msg);

            _requestedCommands = true;
        }

        /// <summary>
        ///     Sends a command directly to the server.
        /// </summary>
        private void SendServerConsoleCommand(string text)
        {
            if (_network == null || !_network.IsConnected)
                return;

            var msg = _network.CreateNetMessage<MsgConCmd>();
            msg.Text = text;
            _network.ClientSendMessage(msg);
        }

        public void AddFormattedLine(FormattedMessage message)
        {
            // Why the hell does this class implement IDebugConsole.
            AddFormatted?.Invoke(this, new AddFormattedMessageArgs(message));
        }
    }

    /// <summary>
    ///     These dummies are made purely so list and help can list server-side commands.
    /// </summary>
    [Reflect(false)]
    internal class ServerDummyCommand : IConsoleCommand
    {
        internal ServerDummyCommand(string command, string help, string description)
        {
            Command = command;
            Help = help;
            Description = description;
        }

        public string Command { get; }

        public string Help { get; }

        public string Description { get; }

        // Always forward to server.
        public bool Execute(IDebugConsole console, params string[] args)
        {
            return true;
        }
    }
}
