using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Map;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates
{
    /// <inheritdoc cref="IServerGameStateManager"/>
    [UsedImplicitly]
    public class ServerGameStateManager : IServerGameStateManager, IPostInjectInit
    {
        // Mapping of net UID of clients -> last known acked state.
        private readonly Dictionary<long, GameTick> _ackedStates = new();
        private GameTick _lastOldestAck = GameTick.Zero;

        private PVSSystem _pvs = default!;

        [Dependency] private readonly IServerEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerMapManager _mapManager = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IServerEntityNetworkManager _entityNetworkManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private ISawmill _logger = default!;

        public float PvsRange
        {
            get => _configurationManager.GetCVar(CVars.NetMaxUpdateRange);
            set => _configurationManager.SetCVar(CVars.NetMaxUpdateRange, value);
        }

        public void SetTransformNetId(ushort netId)
        {
            _pvs.SetTransformNetId(netId);
        }

        public void PostInject()
        {
            _logger = Logger.GetSawmill("PVS");
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>();
            _networkManager.RegisterNetMessage<MsgStateAck>(HandleStateAck);

            _networkManager.Connected += HandleClientConnected;
            _networkManager.Disconnect += HandleClientDisconnect;

            _pvs = EntitySystem.Get<PVSSystem>();
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

            _pvs.ViewSize = PvsRange * 2;

            if (!_networkManager.IsConnected)
            {
                // Prevent deletions piling up if we have no clients.
                _entityManager.CullDeletionHistory(GameTick.MaxValue);
                _pvs.CullDeletionHistory(GameTick.MaxValue);
                _mapManager.CullDeletionHistory(GameTick.MaxValue);
                _pvs.Cleanup(_playerManager.GetAllPlayers());
                return;
            }

            var inputSystem = _systemManager.GetEntitySystem<InputSystem>();

            var oldestAckValue = GameTick.MaxValue.Value;

            var mainThread = Thread.CurrentThread;
            var parentDeps = IoCManager.Instance!;

            void SendStateUpdate(IPlayerSession session)
            {
                // KILL IT WITH FIRE
                if(mainThread != Thread.CurrentThread)
                    IoCManager.InitThread(new DependencyCollection(parentDeps), true);

                // people not in the game don't get states
                if (session.Status != SessionStatus.InGame)
                    return;

                var channel = session.ConnectedClient;

                if (!_ackedStates.TryGetValue(channel.ConnectionId, out var lastAck))
                {
                    DebugTools.Assert("Why does this channel not have an entry?");
                }

                var (entStates, deletions) = _pvs.CalculateEntityStates(session, lastAck, _gameTiming.CurTick);
                var playerStates = _playerManager.GetPlayerStates(lastAck);
                var mapData = _mapManager.GetStateData(lastAck);

                // lastAck varies with each client based on lag and such, we can't just make 1 global state and send it to everyone
                var lastInputCommand = inputSystem.GetLastInputCommand(session);
                var lastSystemMessage = _entityNetworkManager.GetLastMessageSequence(session);
                var state = new GameState(lastAck, _gameTiming.CurTick, Math.Max(lastInputCommand, lastSystemMessage), entStates, playerStates, deletions, mapData);

                InterlockedHelper.Min(ref oldestAckValue, lastAck.Value);

                DebugTools.Assert(state.MapData?.CreatedMaps is null || (state.MapData?.CreatedMaps is not null && state.EntityStates.HasContents), "Sending new maps, but no entity state.");

                // actually send the state
                var stateUpdateMessage = _networkManager.CreateNetMessage<MsgState>();
                stateUpdateMessage.State = state;

                // If the state is too big we let Lidgren send it reliably.
                // This is to avoid a situation where a state is so large that it consistently gets dropped
                // (or, well, part of it).
                // When we send them reliably, we immediately update the ack so that the next state will not be huge.
                if (stateUpdateMessage.ShouldSendReliably())
                {
                    // TODO: remove this lock by having a single state object per session that contains all per-session state needed.
                    lock (_ackedStates)
                    {
                        _ackedStates[channel.ConnectionId] = _gameTiming.CurTick;
                    }
                }

                _networkManager.ServerSendMessage(stateUpdateMessage, channel);
            }

            Parallel.ForEach(_playerManager.GetAllPlayers(), session =>
            {
                try
                {
                    SendStateUpdate(session);
                }
                catch (Exception e) // Catch EVERY exception
                {
                    _logger.Log(LogLevel.Error, e, "Caught exception while generating mail.");
                }
            });

            _pvs.Cleanup(_playerManager.GetAllPlayers());
            var oldestAck = new GameTick(oldestAckValue);

            // keep the deletion history buffers clean
            if (oldestAck > _lastOldestAck)
            {
                _lastOldestAck = oldestAck;
                _entityManager.CullDeletionHistory(oldestAck);
                _pvs.CullDeletionHistory(oldestAck);
                _mapManager.CullDeletionHistory(oldestAck);
            }
        }
    }
}
