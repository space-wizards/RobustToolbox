using System.Collections.Generic;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.GameState;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates
{
    /// <inheritdoc />
    public class ServerGameStateManager : IServerGameStateManager
    {
        // Mapping of net UID of clients -> last known acked state.
        private readonly Dictionary<long, GameTick> _ackedStates = new Dictionary<long, GameTick>();
        private GameTick _lastOldestAck = GameTick.Zero;

#pragma warning disable 649
        [Dependency] private readonly IServerEntityManager _entityManager;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IServerNetManager _networkManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IConfigurationManager _configurationManager;
#pragma warning restore 649

        public bool PvsEnabled => _configurationManager.GetCVar<bool>("net.pvs");

        /// <inheritdoc />
        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>(MsgState.NAME);
            _networkManager.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME, HandleStateAck);

            _networkManager.Connected += HandleClientConnected;
            _networkManager.Disconnect += HandleClientDisconnect;

            _configurationManager.RegisterCVar("net.pvs", true, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("net.maxupdaterange", 12.5f, CVar.ARCHIVE);
        }

        private void HandleClientConnected(object sender, NetChannelArgs e)
        {
            if (!_ackedStates.ContainsKey(e.Channel.ConnectionId))
                _ackedStates.Add(e.Channel.ConnectionId, GameTick.Zero);
            else
                _ackedStates[e.Channel.ConnectionId] = GameTick.Zero;
        }

        private void HandleClientDisconnect(object sender, NetChannelArgs e)
        {
            _entityManager.DropPlayerState(_playerManager.GetSessionById(e.Channel.SessionId));

            if (_ackedStates.ContainsKey(e.Channel.ConnectionId))
                _ackedStates.Remove(e.Channel.ConnectionId);
        }

        private void HandleStateAck(MsgStateAck msg)
        {
            Ack(msg.MsgChannel.ConnectionId, msg.Sequence);
        }

        private void Ack(long uniqueIdentifier, GameTick stateAcked)
        {
            DebugTools.Assert(_networkManager.IsServer);

            if (_ackedStates.TryGetValue(uniqueIdentifier, out var lastAck))
            {
                if (stateAcked > lastAck) // most of the time this is true
                {
                    _ackedStates[uniqueIdentifier] = stateAcked;
                }
                else if (stateAcked == GameTick.Zero) // client signaled they need a full state
                {
                    //Performance/Abuse: Should this be rate limited?
                    _ackedStates[uniqueIdentifier] = GameTick.Zero;
                }

                //else stateAcked was out of order or client is being silly, just ignore
            }
            else
                DebugTools.Assert("How did the client send us an ack without being connected?");
        }

        /// <inheritdoc />
        public void SendGameStateUpdate()
        {
            DebugTools.Assert(_networkManager.IsServer);

            _entityManager.Update();

            if (!_networkManager.IsConnected)
            {
                // Prevent deletions piling up if we have no clients.
                _entityManager.CullDeletionHistory(GameTick.MaxValue);
                _mapManager.CullDeletionHistory(GameTick.MaxValue);
                return;
            }

            var oldestAck = GameTick.MaxValue;


            foreach (var channel in _networkManager.Channels)
            {
                var session = _playerManager.GetSessionByChannel(channel);
                if (session == null || session.Status != SessionStatus.InGame)
                {
                    // client still joining, maybe iterate over sessions instead?
                    continue;
                }

                if (!_ackedStates.TryGetValue(channel.ConnectionId, out var lastAck))
                {
                    DebugTools.Assert("Why does this channel not have an entry?");
                }

                var entStates = lastAck == GameTick.Zero || !PvsEnabled
                    ? _entityManager.GetEntityStates(lastAck)
                    : _entityManager.UpdatePlayerSeenEntityStates(lastAck, session, _entityManager.MaxUpdateRange);
                var playerStates = _playerManager.GetPlayerStates(lastAck);
                var deletions = _entityManager.GetDeletedEntities(lastAck);
                var mapData = _mapManager.GetStateData(lastAck);


                // lastAck varies with each client based on lag and such, we can't just make 1 global state and send it to everyone
                var state = new GameState(lastAck, _gameTiming.CurTick, entStates, playerStates, deletions, mapData);
                if (lastAck < oldestAck)
                {
                    oldestAck = lastAck;
                }

                // actually send the state
                var stateUpdateMessage = _networkManager.CreateNetMessage<MsgState>();
                stateUpdateMessage.State = state;
                _networkManager.ServerSendMessage(stateUpdateMessage, channel);

                // If the state is too big we let Lidgren send it reliably.
                // This is to avoid a situation where a state is so large that it consistently gets dropped
                // (or, well, part of it).
                // When we send them reliably, we immediately update the ack so that the next state will not be huge.
                if (stateUpdateMessage.ShouldSendReliably())
                {
                    _ackedStates[channel.ConnectionId] = _gameTiming.CurTick;
                }

            }

            // keep the deletion history buffers clean
            if (oldestAck > _lastOldestAck)
            {
                _lastOldestAck = oldestAck;
                _entityManager.CullDeletionHistory(oldestAck);
                _mapManager.CullDeletionHistory(oldestAck);
            }
        }

    }
}
