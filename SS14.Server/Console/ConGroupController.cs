using System;
using SS14.Server.Interfaces.Player;
using SS14.Server.Player;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;

namespace SS14.Server.Console
{
    /// <summary>
    /// Mediates the group system of a console shell.
    /// </summary>
    internal class ConGroupController : IConGroupController
    {
        [Dependency] private readonly IResourceManager _resourceManager;
        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly ILogManager _logManager;
        [Dependency] private readonly IPlayerManager _playerManager;

        private ConGroupContainer _groups;
        private SessionGroupContainer _sessions;

        public void Initialize()
        {
            var logger = _logManager.GetSawmill("con.groups");

            _playerManager.PlayerStatusChanged += _onClientStatusChanged;

            // load the permission groups in the console
            _groups = new ConGroupContainer(_resourceManager, logger);
            _groups.LoadGroups();

            // set up the session group container
            _sessions = new SessionGroupContainer(_configurationManager, logger);
        }

        private void _onClientStatusChanged(object sender, SessionStatusEventArgs e)
        {
            _sessions.OnClientStatusChanged(sender, e);
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
        }

        public void SetGroup(IPlayerSession session, ConGroupIndex newGroup)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (!_groups.GroupExists(newGroup))
                return;

            _sessions.SetSessionGroup(session, newGroup);
        }
    }
}
