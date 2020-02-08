using System;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Robust.Client.Console
{
    /// <summary>
    /// Tracks the console group of the client and which commands they can use.
    /// Receives up to date permissions from the server whenever they change.
    /// </summary>
    public class ClientConGroupController : IClientConGroupController
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _netManager;
#pragma warning restore 649

        /// <summary>
        /// The console group this client is in. Determines which commands the client can use and if they can use vv.
        /// </summary>
        private ConGroup _clientConGroup;

        public event Action ConGroupUpdated;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgConGroupUpdate>(MsgConGroupUpdate.Name, _onConGroupUpdate);
        }

        public bool CanCommand(string cmdName)
        {
            if (_clientConGroup == null)
                return false;
            return _clientConGroup.Commands.Contains(cmdName);
        }

        public bool CanViewVar()
        {
            if (_clientConGroup == null)
                return false;
            return _clientConGroup.CanViewVar;
        }

        public bool CanAdminPlace()
        {
            if (_clientConGroup == null)
                return false;
            return _clientConGroup.CanAdminPlace;
        }

        /// <summary>
        /// Update client console group data with message from the server.
        /// </summary>
        /// <param name="msg">Server message listing what commands this client can use.</param>
        private void _onConGroupUpdate(MsgConGroupUpdate msg)
        {
            _clientConGroup = msg.ClientConGroup;

            ConGroupUpdated?.Invoke();
        }
    }
}
