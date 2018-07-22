using System;
using System.Collections.Generic;
using SS14.Server.Interfaces.Player;
using SS14.Server.Player;
using SS14.Shared.Configuration;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Log;

namespace SS14.Server.Console
{
    /// <summary>
    /// Contains a mapping of Session -> Group for the console shell.
    /// </summary>
    internal class SessionGroupContainer
    {
        private readonly Dictionary<IPlayerSession, ConGroupIndex> _sessionGroups = new Dictionary<IPlayerSession, ConGroupIndex>();
        private readonly IConfigurationManager _configMan;
        private readonly ISawmill _logger;

        /// <summary>
        /// Default group that users are put in when they join the server.
        /// </summary>
        public ConGroupIndex DefaultGroup
        {
            get => new ConGroupIndex(_configMan.GetCVar<int>("console.defaultGroup"));
            set => _configMan.SetCVar("console.defaultGroup", value.Index);
        }

        /// <summary>
        /// Constructs an instance of <see cref="SessionGroupContainer"/>.
        /// </summary>
        /// <param name="configMan">Configuration Dependency</param>
        /// <param name="logger"></param>
        public SessionGroupContainer(IConfigurationManager configMan, ISawmill logger)
        {
            _configMan = configMan;
            _logger = logger;

            if(!_configMan.IsCVarRegistered("console.defaultGroup"))
                _configMan.RegisterCVar("console.defaultGroup", 1, CVar.ARCHIVE);
        }

        /// <summary>
        /// Event handler for when the status of a player session changes.
        /// </summary>
        public void OnClientStatusChanged(object sender, SessionStatusEventArgs e)
        {
            switch (e.NewStatus)
            {
                case SessionStatus.Disconnected:
                    if(_sessionGroups.ContainsKey(e.Session))
                        _sessionGroups.Remove(e.Session);
                    break;
            }
        }

        /// <summary>
        /// Queries the container for the group of a given session.
        /// </summary>
        public ConGroupIndex GetSessionGroup(IPlayerSession session)
        {
            if(session == null)
                throw new ArgumentNullException(nameof(session));

            return _sessionGroups.TryGetValue(session, out var groupIndex) ? groupIndex : DefaultGroup;
        }

        /// <summary>
        /// Updates the session with the given group.
        /// </summary>
        public void SetSessionGroup(IPlayerSession session, ConGroupIndex group)
        {
            if(session == null)
                throw new ArgumentNullException(nameof(session));

            _logger.Info($"Set group: {session}, {group.Index}");

            _sessionGroups[session] = group;
        }

        /// <summary>
        /// Clears all sessions from the container.
        /// </summary>
        public void Clear()
        {
            _sessionGroups.Clear();
        }
    }
}
