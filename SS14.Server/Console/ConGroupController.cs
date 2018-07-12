using SS14.Server.Interfaces.Player;
using SS14.Server.Player;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Resources;

namespace SS14.Server.Console
{
    /// <summary>
    /// Mediates the group system of a console shell.
    /// </summary>
    class ConGroupController
    {
        private ConGroupContainer _groups;
        private SessionGroupContainer _sessions;

        public ConGroupController(IResourceManager resMan, IConfigurationManager configMan, ISawmill logger)
        {
            // load the permission groups in the console
            _groups = new ConGroupContainer(resMan, logger);
            _groups.LoadGroups();

            // set up the session group container
            _sessions = new SessionGroupContainer(configMan, logger);
        }

        public void OnClientStatusChanged(object sender, SessionStatusEventArgs e)
        {
            _sessions.OnClientStatusChanged(sender, e);
        }

        public bool CanCommand(IPlayerSession session, string cmdName)
        {
            // get group of session
            var group = _sessions.GetSessionGroup(session);

            // check if group canCmd
            return  _groups.HasCommand(group, cmdName);
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
    }
}
