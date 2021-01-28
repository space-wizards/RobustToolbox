using System;
using System.Collections.Generic;
using Robust.Client.Log;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

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

    /// <inheritdoc cref="IClientConsoleHost"/>
    internal sealed class ClientConsoleHost : ConsoleHost, IClientConsoleHost
    {
        private static readonly Color MsgColor = new Color(65, 105, 225);
        
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private bool _requestedCommands;
        
        /// <inheritdoc />
        public void Initialize()
        {
            NetManager.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME, HandleConCmdReg);
            NetManager.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME, HandleConCmdAck);
            NetManager.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME);

            Reset();
            LogManager.RootSawmill.AddHandler(new DebugConsoleLogHandler(this));
        }

        /// <inheritdoc />
        public void Reset()
        {
            AvailableCommands.Clear();
            _requestedCommands = false;
            NetManager.Connected += OnNetworkConnected;

            ReloadCommands();
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

        public override void WriteLine(ICommonSession? session, string text, Color color)
        {
            AddString?.Invoke(this, new AddStringArgs(text, color));
        }

        public override void ClearLocalConsole()
        {
            ClearText?.Invoke(this, EventArgs.Empty);
        }

        public override void ExecuteCommand(ICommonSession? session, string command)
        {
            ExecuteCommand(command);
        }

        public event EventHandler<AddStringArgs>? AddString;
        public event EventHandler? ClearText;
        public event EventHandler<AddFormattedMessageArgs>? AddFormatted;

        private void HandleConCmdAck(MsgConCmdAck msg)
        {
            WriteLine(null, "< " + msg.Text, MsgColor);
        }

        private void HandleConCmdReg(MsgConCmdReg msg)
        {
            foreach (var cmd in msg.Commands)
            {
                var commandName = cmd.Name;

                // Do not do duplicate commands.
                if (AvailableCommands.ContainsKey(commandName))
                {
                    Logger.DebugS("console", $"Server sent console command {commandName}, but we already have one with the same name. Ignoring.");
                    continue;
                }

                var command = new ServerDummyCommand(commandName, cmd.Help, cmd.Description);
                AvailableCommands[commandName] = command;
            }
        }
        
        /// <summary>
        ///     Processes commands (chat messages starting with /)
        /// </summary>
        /// <param name="text">input text</param>
        public override void ExecuteCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // echo the command locally
            WriteLine(null, "> " + text, Color.Lime);

            //Commands are processed locally and then sent to the server to be processed there again.
            var args = new List<string>();

            CommandParsing.ParseArguments(text, args);

            var commandname = args[0];

            if (AvailableCommands.ContainsKey(commandname))
            {
                var command = AvailableCommands[commandname];
                args.RemoveAt(0);
                command.Execute(new ConsoleShell(this, null), text, args.ToArray());
            }
            else if (!NetManager.IsConnected)
            {
                WriteLine(null, "Unknown command: " + commandname, Color.Red);
            }
        }

        /// <summary>
        ///     Requests remote commands from server.
        /// </summary>
        public void SendServerCommandRequest()
        {
            if (_requestedCommands)
                return;

            if (!NetManager.IsConnected)
                return;

            var msg = NetManager.CreateNetMessage<MsgConCmdReg>();
            NetManager.ClientSendMessage(msg);

            _requestedCommands = true;
        }

        /// <summary>
        ///     Sends a command directly to the server.
        /// </summary>
        public override void RemoteExecuteCommand(ICommonSession? session, string command)
        {
            if (NetManager == null || !NetManager.IsConnected)
                return;

            var msg = NetManager.CreateNetMessage<MsgConCmd>();
            msg.Text = command;
            NetManager.ClientSendMessage(msg);
        }

        public void AddFormattedLine(FormattedMessage message)
        {
            AddFormatted?.Invoke(this, new AddFormattedMessageArgs(message));
        }
        
        /// <inheritdoc />
        public override IConsoleShell GetSessionShell(ICommonSession session)
        {
            return LocalShell;
        }

        public override void WriteLine(ICommonSession? session, string text)
        {
            WriteLine(null, text, Color.White);
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
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            shell.RemoteExecuteCommand(argStr);
        }
    }
}
