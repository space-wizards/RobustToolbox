using System;
using System.Net;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;

namespace Robust.Server.Console
{
    /// <summary>
    /// Mediates the group system of a console shell.
    /// </summary>
    internal class ConGroupController : IConGroupController
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceManager _resourceManager;
        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly ILogManager _logManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly INetManager _netManager;
#pragma warning restore 649

        private ConGroupContainer _groups;
        private SessionGroupContainer _sessions;

        public void Initialize()
        {
            var logger = _logManager.GetSawmill("con.groups");

            _configurationManager.RegisterCVar("console.loginlocal", true, CVar.ARCHIVE);

            _netManager.RegisterNetMessage<MsgConGroupUpdate>(MsgConGroupUpdate.Name);

            _playerManager.PlayerStatusChanged += _onClientStatusChanged;

            // load the permission groups in the console
            _groups = new ConGroupContainer(_resourceManager, logger);
            _groups.LoadGroups();

            // set up the session group container
            _sessions = new SessionGroupContainer(_configurationManager, logger);

            UpdateAllClientData();
        }

        private void _onClientStatusChanged(object sender, SessionStatusEventArgs e)
        {
            _sessions.OnClientStatusChanged(sender, e);

            if (e.NewStatus == SessionStatus.Connected && _configurationManager.GetCVar<bool>("console.loginlocal"))
            {
                var session = e.Session;
                var address = session.ConnectedClient.RemoteEndPoint.Address;
                if (Equals(address, IPAddress.Loopback) || Equals(address, IPAddress.IPv6Loopback))
                {
                    SetGroup(session, new ConGroupIndex(_configurationManager.GetCVar<int>("console.adminGroup")));
                    UpdateClientData(session);
                }
            }
        }

        public bool CanCommand(IPlayerSession session, string cmdName)
        {
            // get group of session
            var group = _sessions.GetSessionGroup(session);

            // check if group canCmd
            return _groups.HasCommand(group, cmdName);
        }

        public bool CanViewVar(IPlayerSession session)
        {
            var group = _sessions.GetSessionGroup(session);

            return _groups.CanViewVar(group);
        }

        public bool CanAdminPlace(IPlayerSession session)
        {
            var group = _sessions.GetSessionGroup(session);

            return _groups.CanAdminPlace(group);
        }

        /// <summary>
        /// Clears all session data.
        /// </summary>
        public void ClearSessions()
        {
            _sessions.Clear();
        }

        /// <summary>
        /// Clears the existing groups, and reloads from disk.
        /// </summary>
        public void ReloadGroups()
        {
            _groups.Clear();
            _groups.LoadGroups();
            UpdateAllClientData();
        }

        public void SetGroup(IPlayerSession session, ConGroupIndex newGroup)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (!_groups.GroupExists(newGroup))
                return;

            _sessions.SetSessionGroup(session, newGroup);
            UpdateClientData(session);
        }

        /// <summary>
        /// Update a single clients group data.
        /// </summary>
        /// <param name="session">The client session to update.</param>
        private void UpdateClientData(IPlayerSession session)
        {
            var group = _sessions.GetSessionGroup(session);
            var groupData = _groups.Groups[group];

            var msg = _netManager.CreateNetMessage<MsgConGroupUpdate>();
            msg.ClientConGroup = groupData;
            _netManager.ServerSendMessage(msg, session.ConnectedClient);
        }

        /// <summary>
        /// Update group data for all clients.
        /// </summary>
        private void UpdateAllClientData()
        {
            foreach (var session in _playerManager.GetAllPlayers())
            {
                UpdateClientData(session);
            }
        }
    }
}
