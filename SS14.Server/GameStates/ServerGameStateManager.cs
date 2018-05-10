using System.Collections.Generic;
using System.Linq;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameState;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;

namespace SS14.Server.GameStates
{
    public class ServerGameStateManager : IServerGameStateManager
    {
        // Mapping of net UID of clients -> last known acked state.
        private readonly Dictionary<long, uint> ackedStates = new Dictionary<long, uint>();


        [Dependency]
        private IServerEntityManager _entityManager;

        [Dependency]
        private IGameTiming _gameTiming;

        [Dependency]
        private IServerNetManager _networkManager;

        [Dependency]
        private IPlayerManager _playerManager;

        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>(MsgState.NAME);
            _networkManager.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME, HandleStateAck);
        }

        private void Ack(long uniqueIdentifier, uint stateAcked)
        {
            ackedStates[uniqueIdentifier] = stateAcked;
        }

        public void SendGameStateUpdate()
        {
            var connections = _networkManager.Channels;
            if (!connections.Any())
            {
                return;
            }

            uint oldestAck = uint.MaxValue;
            foreach (var connection in connections)
            {
                if (!ackedStates.TryGetValue(connection.ConnectionId, out var ack))
                {
                    ackedStates.Add(connection.ConnectionId, 0);
                }
                else if (ack < oldestAck)
                {
                    oldestAck = ack;
                }
            }

            var entities = _entityManager.GetEntityStates(oldestAck);
            var players = _playerManager.GetPlayerStates();

            var state = new GameState(oldestAck, _gameTiming.CurTick, entities, players);

            foreach (var c in connections)
            {
                var session = _playerManager.GetSessionByChannel(c);

                if (session == null || session.Status != SessionStatus.InGame && session.Status != SessionStatus.InLobby)
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
