using System;
using System.Diagnostics;
using SS14.Client.Interfaces;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;

namespace SS14.Client
{
    public class BaseClient : IBaseClient
    {
        [Dependency]
        private readonly IClientNetManager _net;

        /// <summary>
        ///     Default port that the client tries to connect to if no other port is specified.
        /// </summary>
        public ushort DefaultPort { get; } = 1212;

        public ClientRunLevel RunLevel { get; private set; }

        public void Initialize()
        {
            _net.Connected += OnNetConnected;
            _net.ConnectFailed += OnConnectFailed;
            _net.Disconnect += OnNetDisconnect;

            Reset();
        }

        public void Update() { }

        public void Tick() { }

        public void Dispose() { }

        public void ConnectToServer(string ip, ushort port)
        {
            Debug.Assert(RunLevel < ClientRunLevel.Connect);
            Debug.Assert(!_net.IsConnected);

            OnRunLevelChanged(ClientRunLevel.Connect);
            _net.ClientConnect(ip, port);
        }

        public void DisconnectFromServer(string reason)
        {
            Debug.Assert(RunLevel > ClientRunLevel.Initialize);
            Debug.Assert(_net.IsConnected);

            // runlevel changed in OnNetDisconnect()
            _net.ClientDisconnect(reason);
        }

        public event EventHandler<RunLevelChangedEvent> RunLevelChanged;

        private void Reset()
        {
            OnRunLevelChanged(ClientRunLevel.Initialize);
        }

        private void OnNetConnected(object sender, NetChannelArgs args)
        {
            Debug.Assert(RunLevel < ClientRunLevel.Lobby);
            OnRunLevelChanged(ClientRunLevel.Lobby);
        }

        private void OnConnectFailed(object sender, NetConnectFailArgs args)
        {
            Debug.Assert(RunLevel == ClientRunLevel.Connect);
            Reset();
        }

        private void OnNetDisconnect(object sender, NetChannelArgs args)
        {
            Debug.Assert(RunLevel > ClientRunLevel.Initialize);
            Reset();
        }

        private void OnRunLevelChanged(ClientRunLevel newRunLevel)
        {
            Logger.Debug($"[ENG] Runlevel changed to: {newRunLevel}");
            var evnt = new RunLevelChangedEvent(RunLevel, newRunLevel);
            RunLevel = newRunLevel;
            RunLevelChanged?.Invoke(this, evnt);
        }
    }

    public enum ClientRunLevel
    {
        Error = 0,
        Initialize,
        Connect,
        Lobby,
        Ingame
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
