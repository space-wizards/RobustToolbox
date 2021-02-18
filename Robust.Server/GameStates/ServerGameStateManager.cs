using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
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
        private readonly Dictionary<long, GameTick> _ackedStates = new();
        private GameTick _lastOldestAck = GameTick.Zero;

        [Dependency] private readonly IServerEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IServerEntityNetworkManager _entityNetworkManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public bool PvsEnabled => _configurationManager.GetCVar(CVars.NetPVS);

        /// <inheritdoc />
        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>(MsgState.NAME);
            _networkManager.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME, HandleStateAck);

            _networkManager.Connected += HandleClientConnected;
            _networkManager.Disconnect += HandleClientDisconnect;
        }

        private void HandleClientConnected(object? sender, NetChannelArgs e)
        {
            if (!_ackedStates.ContainsKey(e.Channel.ConnectionId))
                _ackedStates.Add(e.Channel.ConnectionId, GameTick.Zero);
            else
                _ackedStates[e.Channel.ConnectionId] = GameTick.Zero;
        }

        private void HandleClientDisconnect(object? sender, NetChannelArgs e)
        {
            _ackedStates.Remove(e.Channel.ConnectionId);

            if (!_playerManager.TryGetSessionByChannel(e.Channel, out var session))
            {
                return;
            }

            _entityManager.DropPlayerState(session);
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

            var inputSystem = _systemManager.GetEntitySystem<InputSystem>();

            var oldestAck = GameTick.MaxValue;

            var oldDeps = IoCManager.Resolve<IDependencyCollection>();

            var deps = new DependencyCollection();
            deps.RegisterInstance<ILogManager>(new ProxyLogManager(IoCManager.Resolve<ILogManager>()));
            deps.BuildGraph();

            (MsgState, INetChannel) GenerateMail(IPlayerSession session)
            {
                IoCManager.InitThread(deps, true);

                if (session.Status != SessionStatus.InGame)
                {
                    return default;
                }

                var channel = session.ConnectedClient;

                if (!_ackedStates.TryGetValue(channel.ConnectionId, out var lastAck))
                {
                    DebugTools.Assert("Why does this channel not have an entry?");
                }

                var entStates = lastAck == GameTick.Zero || !PvsEnabled
                    ? _entityManager.GetEntityStates(lastAck, session)
                    : _entityManager.UpdatePlayerSeenEntityStates(lastAck, session, _entityManager.MaxUpdateRange);
                var playerStates = _playerManager.GetPlayerStates(lastAck);
                var deletions = _entityManager.GetDeletedEntities(lastAck);
                var mapData = _mapManager.GetStateData(lastAck);

                // lastAck varies with each client based on lag and such, we can't just make 1 global state and send it to everyone
                var lastInputCommand = inputSystem.GetLastInputCommand(session);
                var lastSystemMessage = _entityNetworkManager.GetLastMessageSequence(session);
                var state = new GameState(lastAck, _gameTiming.CurTick, Math.Max(lastInputCommand, lastSystemMessage), entStates?.ToArray(), playerStates?.ToArray(), deletions?.ToArray(), mapData);
                if (lastAck < oldestAck)
                {
                    oldestAck = lastAck;
                }

                // actually send the state
                var stateUpdateMessage = _networkManager.CreateNetMessage<MsgState>();
                stateUpdateMessage.State = state;

                // If the state is too big we let Lidgren send it reliably.
                // This is to avoid a situation where a state is so large that it consistently gets dropped
                // (or, well, part of it).
                // When we send them reliably, we immediately update the ack so that the next state will not be huge.
                if (stateUpdateMessage.ShouldSendReliably())
                {
                    _ackedStates[channel.ConnectionId] = _gameTiming.CurTick;
                }

                return (stateUpdateMessage, channel);
            }

            var mailBag = _playerManager.GetAllPlayers()
                .AsParallel().Select(GenerateMail).ToList();

            // TODO: oh god oh fuck kill it with fire.
            // PLINQ *seems* to be scheduling to the main thread partially (I guess that makes sense?)
            // Which causes that IoC "hack" up there to override IoC in the main thread, nuking the game.
            // At least, that's our running theory. I reproduced it once locally and can't reproduce it again.
            // Throwing shit at the wall to hope it fixes it.
            IoCManager.InitThread(oldDeps, true);

            foreach (var (msg, chan) in mailBag)
            {
                // see session.Status != SessionStatus.InGame above
                if (chan == null) continue;
                _networkManager.ServerSendMessage(msg, chan);
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
