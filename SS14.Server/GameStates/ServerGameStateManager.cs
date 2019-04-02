using System.Collections.Generic;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameState;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using SS14.Shared.Timing;
using SS14.Shared.Utility;

namespace SS14.Server.GameStates
{
    public class ServerGameStateManager : IServerGameStateManager
    {
        // Mapping of net UID of clients -> last known acked state.
        private readonly Dictionary<long, GameTick> ackedStates = new Dictionary<long, GameTick>();

        private GameTick lastOldestAck = GameTick.Zero;

        [Dependency]
        private IServerEntityManager _entityManager;

        [Dependency]
        private IGameTiming _gameTiming;

        [Dependency]
        private IServerNetManager _networkManager;

        [Dependency]
        private IPlayerManager _playerManager;

        [Dependency] private IMapManager _mapManager;

        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>(MsgState.NAME);
            _networkManager.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME, HandleStateAck);
        }

        private void Ack(long uniqueIdentifier, GameTick stateAcked)
        {
            ackedStates[uniqueIdentifier] = stateAcked;
        }

        public void SendGameStateUpdate()
        {
            DebugTools.Assert(_networkManager.IsServer);

            if (!_networkManager.IsConnected)
            {
                // Prevent deletions piling up if we have no clients.
                _entityManager.CullDeletionHistory(GameTick.MaxValue);
                _mapManager.CullDeletionHistory(GameTick.MaxValue);
                return;
            }

            var oldestAck = GameTick.MaxValue;
            foreach (var connection in _networkManager.Channels)
            {
                if (!ackedStates.TryGetValue(connection.ConnectionId, out var ack))
                {
                    ackedStates.Add(connection.ConnectionId, GameTick.Zero);
                }
                else if (ack < oldestAck)
                {
                    oldestAck = ack;
                }
            }

            if (oldestAck > lastOldestAck)
            {
                lastOldestAck = oldestAck;
                _entityManager.CullDeletionHistory(oldestAck);
            }

            var entities = _entityManager.GetEntityStates(oldestAck);
            var players = _playerManager.GetPlayerStates(oldestAck);
            var deletions = _entityManager.GetDeletedEntities(oldestAck);
            var mapData = _mapManager.GetStateData(oldestAck);

            var state = new GameState(oldestAck, _gameTiming.CurTick, entities, players, deletions, mapData);

            foreach (var c in _networkManager.Channels)
            {
                var session = _playerManager.GetSessionByChannel(c);

                if (session == null || session.Status != SessionStatus.InGame)
                    continue;

                var stateUpdateMessage = _networkManager.CreateNetMessage<MsgState>();
                stateUpdateMessage.State = state;
                _networkManager.ServerSendMessage(stateUpdateMessage, c);
            }
        }

        private void HandleStateAck(MsgStateAck msg)
        {
            Ack(msg.MsgChannel.ConnectionId, msg.Sequence);
        }
    }
}
