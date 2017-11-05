using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network;

namespace SS14.Client
{
    public class BaseClient : IBaseClient
    {
        [Dependency]
        private readonly IClientNetManager _net;

        private DateTime _connectTime;

        /// <summary>
        ///     Default port that the client tries to connect to if no other port is specified.
        /// </summary>
        public ushort DefaultPort { get; } = 1212;

        /// <summary>
        ///     How long to wait before the connection attempt times out
        /// </summary>
        public const float ConnectTimeOut = 5000.0f;

        public ClientRunLevel RunLevel
        {
            get;
            private set;
        }
        
        public void Initialize()
        {
            _net.Connected += OnNetConnected;
            _net.Disconnect += OnNetDisconnect;

            Reset();
        }

        private void Reset()
        {
            OnRunLevelChanged(ClientRunLevel.Initialize);
        }

        private void OnNetDisconnect(object sender, NetChannelArgs args)
        {
            Debug.Assert(RunLevel > ClientRunLevel.Initialize);
            Debug.Assert(_net.IsClient);

            Reset();
        }

        public void ConnectToServer(string ip, ushort port)
        {
            Debug.Assert(RunLevel < ClientRunLevel.Lobby);
            Debug.Assert(_net.IsClient);
            Debug.Assert(!_net.IsConnected);

            _connectTime = DateTime.Now;
            _net.ClientConnect(ip, port);
        }

        public void DisconnectFromServer(string reason)
        {
            Debug.Assert(RunLevel > ClientRunLevel.Initialize);
            Debug.Assert(_net.IsClient);
            Debug.Assert(_net.IsConnected);

            _net.ClientDisconnect(reason);
        }

        private void OnNetConnected(object sender, NetChannelArgs args)
        {
            Debug.Assert(RunLevel < ClientRunLevel.Lobby);
            OnRunLevelChanged(ClientRunLevel.Lobby);
        }

        private void OnRunLevelChanged(ClientRunLevel newRunLevel)
        {
            var evnt = new RunLevelChangedEvent(RunLevel, newRunLevel);
            RunLevel = newRunLevel;
            RunLevelChanged?.Invoke(this, evnt);
        }

        public event EventHandler<RunLevelChangedEvent> RunLevelChanged;



    }

    public enum ClientRunLevel
    {
        Error = 0,
        Initialize = 1,
        Lobby = 2,
        Ingame = 3,
    }

    public class RunLevelChangedEvent : EventArgs
    {
        public ClientRunLevel OldLevel { get; }
        public ClientRunLevel NewLevel { get; }

        public RunLevelChangedEvent(ClientRunLevel oldLevel, ClientRunLevel newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}
