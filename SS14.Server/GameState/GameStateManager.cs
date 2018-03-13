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
    public class GameStateManager : Dictionary<uint, GameState>, IGameStateManager
    {
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
            _networkManager.RegisterNetMessage<MsgStateUpdate>(MsgStateUpdate.NAME);
            _networkManager.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME, message => HandleStateAck((MsgStateAck) message));
            _networkManager.RegisterNetMessage<MsgFullState>(MsgFullState.NAME);
        }

        public void Cull()
        {
            foreach (var v in Keys.Where(v => v < OldestStateAcked).ToList())
                Remove(v);
        }

        public uint OldestStateAcked
        {
            get { return ackedStates.Values.FirstOrDefault(val => val == ackedStates.Values.Min()); }
        }

        public void Ack(long uniqueIdentifier, uint stateAcked)
        {
            if (!ackedStates.ContainsKey(uniqueIdentifier))
                ackedStates.Add(uniqueIdentifier, stateAcked);
            else
                ackedStates[uniqueIdentifier] = stateAcked;
        }

        public GameStateDelta GetDelta(INetChannel client, uint state)
        {
            var toState = GetFullState(state);
            if (!ackedStates.ContainsKey(client.ConnectionId))
                return toState - new GameState(0); //The client has no state!

            var ackack = ackedStates[client.ConnectionId];
            var fromState = this[ackack];
            return toState - fromState;
        }

        public GameState GetFullState(uint state)
        {
            if (ContainsKey(state))
                return this[state];
            return null; //TODO SHIT
        }

        public uint GetLastStateAcked(INetChannel client)
        {
            if (!ackedStates.ContainsKey(client.ConnectionId))
                ackedStates[client.ConnectionId] = 0;

            return ackedStates[client.ConnectionId];
        }

        public void CullAll()
        {
            ackedStates.Clear();
            Clear();
        }

        public void SendGameStateUpdate()
        {
            //Create a new GameState object
            var state1 = new GameState(_gameTiming.CurTick)
            {
                EntityStates = _entityManager.GetEntityStates(),
                PlayerStates = _playerManager.GetPlayerStates()
            };
            var state = state1;
            Add(state.Sequence, state);

            var connections = _networkManager.Channels;
            if (!connections.Any())
            {
                CullAll();
                return;
            }

            var playerMan = _playerManager;

            foreach (var c in connections)
            {
                var session = playerMan.GetSessionByChannel(c);

                if (session == null || session.Status != SessionStatus.InGame && session.Status != SessionStatus.InLobby)
                    continue;

                SendConnectionGameStateUpdate(c, state, _gameTiming.CurTick);
            }
            Cull();
        }

        private void SendConnectionGameStateUpdate(INetChannel c, GameState state, uint curTick)
        {
            if (((IGameStateManager) this).GetLastStateAcked(c) == 0)
            {
                var fullStateMessage = _networkManager.CreateNetMessage<MsgFullState>();
                fullStateMessage.State = state;
                _networkManager.ServerSendMessage(fullStateMessage, c);
            }
            else
            {
                var stateUpdateMessage = _networkManager.CreateNetMessage<MsgStateUpdate>();
                stateUpdateMessage.StateDelta = ((IGameStateManager) this).GetDelta(c, curTick);
                _networkManager.ServerSendMessage(stateUpdateMessage, c);
            }
        }

        private void HandleStateAck(MsgStateAck msg)
        {
            Ack(msg.MsgChannel.ConnectionId, msg.Sequence);
        }
    }
}
