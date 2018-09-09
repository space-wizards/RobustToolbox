using System;
using System.Collections.Generic;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using SS14.Shared.Utility;

namespace SS14.Server.Console
{
    /// <inheritdoc />
    internal class ConsoleShell : IConsoleShell
    {
        private const string SawmillName = "con";

        [Dependency] private readonly IReflectionManager _reflectionManager;
        [Dependency] private readonly IPlayerManager _players;
        [Dependency] private readonly IServerNetManager _net;
        [Dependency] private readonly ISystemConsoleManager _systemConsole;
        [Dependency] private readonly ILogManager _logMan;
        [Dependency] private readonly IConfigurationManager _configMan;
        [Dependency] private readonly IConGroupController _groupController;

        private readonly Dictionary<string, IClientCommand> _availableCommands =
            new Dictionary<string, IClientCommand>();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IClientCommand> AvailableCommands => _availableCommands;

        private void HandleRegistrationRequest(INetChannel senderConnection)
        {
            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var message = netMgr.CreateNetMessage<MsgConCmdReg>();

            var counter = 0;
            message.Commands = new MsgConCmdReg.Command[AvailableCommands.Count];
            foreach (var command in AvailableCommands.Values)
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
            // register console admin global password. DO NOT ADD THE REPLICATED FLAG
            if (!_configMan.IsCVarRegistered("console.password"))
                _configMan.RegisterCVar("console.password", string.Empty,
                    CVar.ARCHIVE | CVar.SERVER | CVar.NOT_CONNECTED);

            if (!_configMan.IsCVarRegistered("console.adminGroup"))
                _configMan.RegisterCVar("console.adminGroup", 100, CVar.ARCHIVE | CVar.SERVER);

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
            foreach (var type in _reflectionManager.GetAllChildren<IClientCommand>())
            {
                var instance = (IClientCommand) Activator.CreateInstance(type, null);
                if (AvailableCommands.TryGetValue(instance.Command, out var duplicate))
                    throw new InvalidImplementationException(instance.GetType(), typeof(IClientCommand),
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
        public void ExecuteCommand(IPlayerSession session, string command)
        {
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
                    if (session != null) // remote client
                    {
                        if (_groupController.CanCommand(session, cmdName)) // client has permission
                        {
                            args.RemoveAt(0);
                            conCmd.Execute(this, session, args.ToArray());
                        }
                        else
                            SendText(session, $"Unknown command: '{cmdName}'");
                    }
                    else // system console
                    {
                        args.RemoveAt(0);
                        conCmd.Execute(this, null, args.ToArray());
                    }
                }
                else
                    SendText(session, $"Unknown command: '{cmdName}'");
            }
            catch (Exception e)
            {
                _logMan.GetSawmill(SawmillName).Warning($"{FormatPlayerString(session)}: ExecuteError - {command}");
                SendText(session, $"There was an error while executing the command: {e.Message}");
            }
        }

        /// <inheritdoc />
        public void SendText(IPlayerSession session, string text)
        {
            if (session != null)
                SendText(session.ConnectedClient, text);
            else
                _systemConsole.Print(text + "\n");
        }

        /// <inheritdoc />
        public void SendText(INetChannel target, string text)
        {
            var replyMsg = _net.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            _net.ServerSendMessage(replyMsg, target);
        }

        private static string FormatPlayerString(IPlayerSession session)
        {
            return session != null ? $"{session.Name}" : "[HOST]";
        }

        public bool ElevateShell(IPlayerSession session, string password)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var realPass = _configMan.GetCVar<string>("console.password");

            // password disabled
            if (string.IsNullOrWhiteSpace(realPass))
                return false;

            // wrong password
            if (password != realPass)
                return false;

            // success!
            _groupController.SetGroup(session, new ConGroupIndex(_configMan.GetCVar<int>("console.adminGroup")));

            return true;
        }

        private class LoginCommand : IClientCommand
        {
            public string Command => "login";
            public string Description => "Elevates client to admin permission group.";
            public string Help => "login";

            public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
            {
                // system console can't log in to itself, and is pointless anyways
                if (player == null)
                    return;

                // If the password is null/empty/whitespace in the config, this effectively disables the command
                if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
                    return;

                // WE ARE AT THE BRIDGE OF DEATH
                if (shell.ElevateShell(player, args[0]))
                    return;

                // CAST INTO THE GORGE OF ETERNAL PERIL
                Logger.WarningS(
                    "con.auth",
                    $"Failed console login authentication.\n  NAME:{player}\n  IP:  {player.ConnectedClient.RemoteEndPoint}");

                var net = IoCManager.Resolve<IServerNetManager>();
                net.DisconnectChannel(player.ConnectedClient, "Failed login authentication.");
            }
        }

        private class GroupCommand : IClientCommand
        {
            public string Command => "group";
            public string Description => "Prints your current permission group.";
            public string Help => "group";

            public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
            {
                // only the local server console bypasses permissions
                if (player == null)
                    shell.SendText(player, "LOCAL_CONSOLE");

                //TODO: Turn console commands into delegates so that this can actually work.
            }
        }
    }
}
