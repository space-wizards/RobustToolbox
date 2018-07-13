using System;
using System.Collections.Generic;
using SS14.Server.Interfaces.ClientConsoleHost;
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
using SS14.Shared.Network.Messages;
using SS14.Shared.Utility;

namespace SS14.Server.Console
{
    /// <inheritdoc />
    internal class ConsoleShell : IConsoleShell
    {
        private const string SawmillName = "con";

        [Dependency]
        private readonly IReflectionManager _reflectionManager;
        [Dependency]
        private readonly IPlayerManager _players;
        [Dependency]
        private readonly IServerNetManager _net;
        [Dependency]
        private readonly ISystemConsoleManager _systemConsole;
        [Dependency]
        private readonly ILogManager _logMan;
        [Dependency]
        private readonly IResourceManager _resMan;
        [Dependency]
        private readonly IConfigurationManager _configMan;
        
        private ConGroupController _groupController;

        private readonly Dictionary<string, IClientCommand> _availableCommands = new Dictionary<string, IClientCommand>();

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
            _groupController = new ConGroupController(_resMan, _configMan, _logMan.GetSawmill("con.groups"));
            _players.PlayerStatusChanged += _groupController.OnClientStatusChanged;

            // register console admin global password. DO NOT ADD THE REPLICATED FLAG
            if(!_configMan.IsCVarRegistered("console.password"))
                _configMan.RegisterCVar("console.password", string.Empty, CVar.ARCHIVE | CVar.SERVER | CVar.NOT_CONNECTED);
            
            ReloadCommands();

            // setup networking with clients
            _net.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, ProcessCommand);
            _net.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME);
            _net.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME, message => HandleRegistrationRequest(message.MsgChannel));
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
                    throw new InvalidImplementationException(instance.GetType(), typeof(IClientCommand), $"Command name already registered: {instance.Command}, previous: {duplicate.GetType()}");

                _availableCommands[instance.Command] = instance;
            }
        }

        /// <inheritdoc />
        public void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);

            _logMan.GetSawmill(SawmillName).Info($"{FormatPlayerString(session)}:{text}");

            ExecuteCommand(session, text);
        }

        /// <inheritdoc />
        public void ExecuteHostCommand(string command)
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
                    if(session != null) // remote client
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
                SendConsoleText(session.ConnectedClient, text);
            else
                _systemConsole.Print(text + "\n");
        }

        /// <inheritdoc />
        public void SendConsoleText(INetChannel target, string text)
        {
            var replyMsg = _net.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            _net.ServerSendMessage(replyMsg, target);
        }

        private static string FormatPlayerString(IPlayerSession session)
        {
            return session != null ? $"[{session.Index}]{session.Name}" : "[HOST]";
        }

#if _Future
        private class LoginCommand : IClientCommand
        {
            public string Command => "login";
            public string Description => "Elevates client to admin permission group.";
            public string Help => "login";
            public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
            {
                // system console can't log in to itself, and is pointless anyways
                if(player == null)
                    return;

                if(args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
                    return;

                //TODO: Make me work.
                //AttemptLogin(player, args[0]);
            }
        }
#endif
    }
}
