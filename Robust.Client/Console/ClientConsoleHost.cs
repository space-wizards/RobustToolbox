using System;
using System.Collections.Generic;
using Robust.Client.Log;
using Robust.Shared.Console;
using Robust.Shared.Log;
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

        public bool Local { get; }

        public bool Error { get; }

        public AddStringArgs(string text, bool local, bool error)
        {
            Text = text;
            Local = local;
            Error = error;
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

    /// <inheritdoc cref="IClientConsoleHost" />
    internal class ClientConsoleHost : ConsoleHost, IClientConsoleHost
    {
        private bool _requestedCommands;

        /// <inheritdoc />
        public void Initialize()
        {
            NetManager.RegisterNetMessage<MsgConCmdReg>(HandleConCmdReg);
            NetManager.RegisterNetMessage<MsgConCmdAck>(HandleConCmdAck);
            NetManager.RegisterNetMessage<MsgConCmd>(ProcessCommand);

            _requestedCommands = false;
            NetManager.Connected += OnNetworkConnected;

            LoadConsoleCommands();
            SendServerCommandRequest();
            LogManager.RootSawmill.AddHandler(new DebugConsoleLogHandler(this));
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

        /// <inheritdoc />
        public override void ExecuteCommand(ICommonSession? session, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            // echo the command locally
            WriteError(null, "> " + command);

            //Commands are processed locally and then sent to the server to be processed there again.
            var args = new List<string>();

            CommandParsing.ParseArguments(command, args);

            var commandName = args[0];

            if (AvailableCommands.ContainsKey(commandName))
            {
                var command1 = AvailableCommands[commandName];
                args.RemoveAt(0);
                command1.Execute(new ConsoleShell(this, null), command, args.ToArray());
            }
            else
                WriteError(null, "Unknown command: " + commandName);
        }

        /// <inheritdoc />
        public override void RemoteExecuteCommand(ICommonSession? session, string command)
        {
            if (!NetManager.IsConnected) // we don't care about session on client
                return;

            var msg = NetManager.CreateNetMessage<MsgConCmd>();
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
        /// Requests remote commands from server.
        /// </summary>
        private void SendServerCommandRequest()
        {
            if (_requestedCommands)
                return;

            if (!NetManager.IsConnected)
                return;

            var msg = NetManager.CreateNetMessage<MsgConCmdReg>();
            NetManager.ClientSendMessage(msg);

            _requestedCommands = true;
        }
    }

    /// <summary>
    /// These dummies are made purely so list and help can list server-side commands.
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

        public string Description { get; }

        public string Help { get; }

        // Always forward to server.
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            shell.RemoteExecuteCommand(argStr);
        }
    }
}
