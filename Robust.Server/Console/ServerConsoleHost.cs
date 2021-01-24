using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <inheritdoc />
    internal class ServerConsoleHost : IServerConsoleHost
    {
        private const string SawmillName = "con";

        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IServerNetManager _net = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;
        [Dependency] private readonly ILogManager _logMan = default!;
        [Dependency] private readonly IConGroupController _groupController = default!;

        private readonly Dictionary<string, IConsoleCommand> _availableCommands =
            new Dictionary<string, IConsoleCommand>();

        public ServerConsoleHost()
        {
            LocalShell = new ConsoleShellAdapter(this, null);
        }

        public IConsoleShell LocalShell { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IConsoleCommand> RegisteredCommands => _availableCommands;

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

        /// <inheritdoc />
        public void Initialize()
        {
            ReloadCommands();

            // setup networking with clients
            _net.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, ProcessCommand);
            _net.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME);
            _net.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME,
                message => HandleRegistrationRequest(message.MsgChannel));
        }

        /// <inheritdoc />
        public void ReloadCommands()
        {
            // search for all client commands in all assemblies, and register them
            _availableCommands.Clear();
            foreach (var type in _reflectionManager.GetAllChildren<IConsoleCommand>())
            {
                var instance = (IConsoleCommand) Activator.CreateInstance(type, null)!;
                if (RegisteredCommands.TryGetValue(instance.Command, out var duplicate))
                    throw new InvalidImplementationException(instance.GetType(), typeof(IConsoleCommand),
                        $"Command name already registered: {instance.Command}, previous: {duplicate.GetType()}");

                _availableCommands[instance.Command] = instance;
            }
        }

        private void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);

            _logMan.GetSawmill(SawmillName).Info($"{FormatPlayerString(session)}:{text}");

            ExecuteCommand(session, text);
        }

        /// <inheritdoc />
        public void ExecuteCommand(string command)
        {
            ExecuteCommand(null, command);
        }

        /// <inheritdoc />
        public void ExecuteCommand(ICommonSession? session, string command)
        {
            var svSession = session as IPlayerSession;
            try
            {
                var args = new List<string>();
                CommandParsing.ParseArguments(command, args);

                // missing cmdName
                if (args.Count == 0)
                    return;

                var cmdName = args[0];

                if (_availableCommands.TryGetValue(cmdName, out var conCmd)) // command registered
                {
                    if (svSession != null) // remote client
                    {
                        if (_groupController.CanCommand(svSession, cmdName)) // client has permission
                        {
                            args.RemoveAt(0);
                            conCmd.Execute(new ConsoleShellAdapter(this, session), command, args.ToArray());
                        }
                        else
                            SendText(svSession, $"Unknown command: '{cmdName}'");
                    }
                    else // system console
                    {
                        args.RemoveAt(0);
                        conCmd.Execute(new ConsoleShellAdapter(this, null), command, args.ToArray());
                    }
                }
                else
                    SendText(svSession, $"Unknown command: '{cmdName}'");
            }
            catch (Exception e)
            {
                _logMan.GetSawmill(SawmillName).Warning($"{FormatPlayerString(svSession)}: ExecuteError - {command}:\n{e}");
                SendText(svSession, $"There was an error while executing the command: {e}");
            }
        }

        /// <summary>
        /// Sends a text string to the remote player.
        /// </summary>
        /// <param name="session">
        /// Remote player to send the text message to. If this is null, the text is sent to the local
        /// console.
        /// </param>
        /// <param name="text">Text message to send.</param>
        public void SendText(IPlayerSession? session, string text)
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
        public void SendText(INetChannel target, string text)
        {
            var replyMsg = _net.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            _net.ServerSendMessage(replyMsg, target);
        }
        
        private static string FormatPlayerString(IPlayerSession? session)
        {
            return session != null ? $"{session.Name}" : "[HOST]";
        }

        private class SudoCommand : IConsoleCommand
        {
            public string Command => "sudo";
            public string Description => "sudo make me a sandwich";
            public string Help => "sudo";

            public void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                var command = args[0];
                var cArgs = args[1..].Select(CommandParsing.Escape);

                var localShell = shell.ConsoleHost.LocalShell;
                localShell.ExecuteCommand($"{command} {string.Join(' ', cArgs)}");
            }
        }

        IConsoleShell IConsoleHost.LocalShell => LocalShell;

        public void RegisterCommand(string command, string description, string help, ConCommandCallback callback)
        {
            if (_availableCommands.ContainsKey(command))
                throw new InvalidOperationException($"Command already registered: {command}");

            var newCmd = new RegisteredCommand(command, description, help, callback);
            _availableCommands.Add(command, newCmd);
        }

        public IConsoleShell GetSessionShell(ICommonSession session)
        {
            if (session.Status >= SessionStatus.Disconnected)
                throw new InvalidOperationException("Tried to get the session shell of a disconnected peer.");

            return new ConsoleShellAdapter(this, session);
        }

        public void WriteLine(ICommonSession? session, string text)
        {
            if (session is IPlayerSession playerSession)
            {
                SendText(playerSession, text);
            }
            else
            {
                SendText(null as IPlayerSession, text);
            }
        }
    }

    public class ConsoleShellAdapter : IConsoleShell
    {
        private IServerConsoleHost _host;
        private ICommonSession? _session;

        public ConsoleShellAdapter(IServerConsoleHost host, ICommonSession? session)
        {
            _host = host;
            _session = session;
        }

        public IConsoleHost ConsoleHost => _host;
        public bool IsServer => true;
        public ICommonSession? Player => _session;

        public void ExecuteCommand(string command)
        {
            _host.ExecuteCommand(_session, command);
        }

        public void RemoteExecuteCommand(string command)
        {
            // Does nothing
        }

        public void WriteLine(string text)
        {
            _host.WriteLine(_session, text);
        }

        public void WriteLine(string text, Color color)
        {
            //TODO: Make the color work!
            _host.WriteLine(_session, text);
        }

        public void Clear()
        {
            // Does nothing
        }
    }
}
