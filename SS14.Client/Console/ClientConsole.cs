using System;
using System.Collections.Generic;
using SS14.Client.Interfaces.Console;
using SS14.Shared.Console;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Reflection;
using SS14.Shared.Utility;

namespace SS14.Client.Console
{
    public class AddStringArgs : EventArgs
    {
        public string Text { get; }
        public Color Color { get; }
        public ChatChannel Channel { get; }

        public AddStringArgs(string text, Color color, ChatChannel channel)
        {
            Text = text;
            Color = color;
            Channel = channel;
        }
    }

    public class ClientConsole : IClientConsole, IDebugConsole
    {
        private static readonly Color MsgColor = new Color(65, 105, 225);

        [Dependency]
        protected readonly IClientNetManager _network;
        [Dependency]
        private readonly IReflectionManager _reflectionManager;

        private readonly Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();
        private bool _requestedCommands;

        /// <inheritdoc />
        public virtual void Initialize()
        {
            _network.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME, HandleConCmdReg);
            _network.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME, HandleConCmdAck);
            _network.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME);

            Reset();
        }

        /// <inheritdoc />
        public virtual void Reset()
        {
            _commands.Clear();
            _requestedCommands = false;
            _network.Connected += OnNetworkConnected;

            InitializeCommands();
            SendServerCommandRequest();
        }

        private void OnNetworkConnected(object sender, NetChannelArgs netChannelArgs)
        {
            SendServerCommandRequest();
        }

        /// <inheritdoc />
        public void Dispose() { }

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => _commands;

        public void AddLine(string text, ChatChannel channel, Color color)
        {
            AddString?.Invoke(this, new AddStringArgs(text, color, channel));
        }

        public void AddLine(string text, Color color)
        {
            AddLine(text, ChatChannel.Default, color);
        }

        public void AddLine(string text)
        {
            AddLine(text, ChatChannel.Default, Color.White);
        }

        public void Clear()
        {
            ClearText?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<AddStringArgs> AddString;
        public event EventHandler ClearText;

        private void HandleConCmdAck(NetMessage message)
        {
            var msg = (MsgConCmdAck)message;

            AddLine("< " + msg.Text, ChatChannel.Default, MsgColor);
        }

        private void HandleConCmdReg(NetMessage message)
        {
            var msg = (MsgConCmdReg)message;

            foreach (var cmd in msg.Commands)
            {
                var commandName = cmd.Name;

                // Do not do duplicate commands.
                if (_commands.ContainsKey(commandName))
                {
                    Logger.Warning($"Server sent console command {commandName}, but we already have one with the same name. Ignoring.");
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
            AddLine("> " + text, ChatChannel.Default, new Color(255, 250, 240));

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
                AddLine("Unknown command: " + commandname, ChatChannel.Default, Color.Red);
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
                var instance = (IConsoleCommand)Activator.CreateInstance(t, null);
                if (_commands.ContainsKey(instance.Command))
                    throw new Exception($"Command already registered: {instance.Command}");

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
