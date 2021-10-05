using System;
using System.Collections.Generic;
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
using Robust.Shared.Map;
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

        private EntityViewCulling _entityView = null!;

        [Dependency] private readonly IServerEntityManager _entityManager = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerMapManager _mapManager = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IServerEntityNetworkManager _entityNetworkManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private ISawmill _logger = default!;

        public bool PvsEnabled
        {
            get => _configurationManager.GetCVar(CVars.NetPVS);
            set => _configurationManager.SetCVar(CVars.NetPVS, value);
        }

        public float PvsRange
        {
            get => _configurationManager.GetCVar(CVars.NetMaxUpdateRange);
            set => _configurationManager.SetCVar(CVars.NetMaxUpdateRange, value);
        }

        public void SetTransformNetId(ushort netId)
        {
            _entityView.SetTransformNetId(netId);
        }

        public void PostInject()
        {
            _logger = Logger.GetSawmill("PVS");
            _entityView = new EntityViewCulling(_entityManager, _mapManager, _lookup);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>();
            _networkManager.RegisterNetMessage<MsgStateAck>(HandleStateAck);

            _networkManager.Connected += HandleClientConnected;
            _networkManager.Disconnect += HandleClientDisconnect;

            _playerManager.PlayerStatusChanged += HandlePlayerStatusChanged;

            _entityManager.EntityDeleted += HandleEntityDeleted;

            _mapManager.OnGridRemoved += HandleGridRemove;

            // If you want to make this modifiable at runtime you need to subscribe to tickrate updates and streaming updates
            // plus invalidate any chunks currently being streamed as well.
            _entityView.StreamingTilesPerTick = (int) (_configurationManager.GetCVar(CVars.StreamedTilesPerSecond) / _gameTiming.TickRate);
            _configurationManager.OnValueChanged(CVars.StreamedTileRange, value => _entityView.StreamRange = value, true);
        }

        private void HandleGridRemove(MapId mapid, GridId gridid)
        {
            // Remove any sort of tracking for when a chunk was sent.
            foreach (var (_, chunks) in _entityView.PlayerChunks)
            {
                foreach (var (chunk, _) in chunks.ToArray())
                {
                    if (chunk is not MapChunk mapChunk ||
                        mapChunk.GridId == gridid)
                    {
                        chunks.Remove(chunk);
                    }
                }
            }
        }

        private void HandleEntityDeleted(object? sender, EntityUid e)
        {
            _entityView.EntityDeleted(e);
        }

        private void HandlePlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.InGame)
            {
                _entityView.AddPlayer(e.Session);
            }
            else if(e.OldStatus == SessionStatus.InGame)
            {
                _entityView.RemovePlayer(e.Session);
            }
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

            _entityView.ViewSize = PvsRange * 2;
            _entityView.CullingEnabled = PvsEnabled;

            if (!_networkManager.IsConnected)
            {
                // Prevent deletions piling up if we have no clients.
                _entityView.CullDeletionHistory(GameTick.MaxValue);
                _mapManager.CullDeletionHistory(GameTick.MaxValue);
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

                var (entStates, deletions) = _entityView.CalculateEntityStates(session, lastAck, _gameTiming.CurTick);
                var playerStates = _playerManager.GetPlayerStates(lastAck);
                var mapData = _mapManager.GetStateData(lastAck);

                // lastAck varies with each client based on lag and such, we can't just make 1 global state and send it to everyone
                var lastInputCommand = inputSystem.GetLastInputCommand(session);
                var lastSystemMessage = _entityNetworkManager.GetLastMessageSequence(session);
                var state = new GameState(lastAck, _gameTiming.CurTick, Math.Max(lastInputCommand, lastSystemMessage), entStates?.ToArray(), playerStates?.ToArray(), deletions?.ToArray(), mapData);

                InterlockedHelper.Min(ref oldestAckValue, lastAck.Value);

                DebugTools.Assert(state.MapData?.CreatedMaps is null || (state.MapData?.CreatedMaps is not null && state.EntityStates is not null), "Sending new maps, but no entity state.");

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

            var oldestAck = new GameTick(oldestAckValue);

            // keep the deletion history buffers clean
            if (oldestAck > _lastOldestAck)
            {
                _lastOldestAck = oldestAck;
                _entityView.CullDeletionHistory(oldestAck);
                _mapManager.CullDeletionHistory(oldestAck);
            }
        }

        /// <summary>
        /// Generates a network entity state for the given entity.
        /// </summary>
        /// <param name="entMan">EntityManager that contains the entity.</param>
        /// <param name="player">The player to generate this state for.</param>
        /// <param name="entityUid">Uid of the entity to generate the state from.</param>
        /// <param name="fromTick">Only provide delta changes from this tick.</param>
        /// <returns>New entity State for the given entity.</returns>
        internal static EntityState GetEntityState(IEntityManager entMan, ICommonSession player, EntityUid entityUid, GameTick fromTick)
        {
            var bus = entMan.EventBus;
            var changed = new List<ComponentChange>();

            foreach (var (netId, component) in entMan.GetNetComponents(entityUid))
            {
                DebugTools.Assert(component.Initialized);

                // NOTE: When LastModifiedTick or CreationTick are 0 it means that the relevant data is
                // "not different from entity creation".
                // i.e. when the client spawns the entity and loads the entity prototype,
                // the data it deserializes from the prototype SHOULD be equal
                // to what the component state / ComponentChange would send.
                // As such, we can avoid sending this data in this case since the client "already has it".

                DebugTools.Assert(component.LastModifiedTick >= component.CreationTick);

                if (component.CreationTick != GameTick.Zero && component.CreationTick >= fromTick && !component.Deleted)
                {
                    ComponentState? state = null;
                    if (component.NetSyncEnabled && component.LastModifiedTick != GameTick.Zero && component.LastModifiedTick >= fromTick)
                        state = entMan.GetComponentState(bus, component, player);

                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChange.Added(netId, state));
                }
                else if (component.NetSyncEnabled && component.LastModifiedTick != GameTick.Zero && component.LastModifiedTick >= fromTick)
                {
                    changed.Add(ComponentChange.Changed(netId, entMan.GetComponentState(bus, component, player)));
                }
                else if (component.Deleted && component.LastModifiedTick >= fromTick)
                {
                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChange.Removed(netId));
                }
            }

            return new EntityState(entityUid, changed.ToArray());
        }

        /// <summary>
        ///     Gets all entity states that have been modified after and including the provided tick.
        /// </summary>
        internal static List<EntityState>? GetAllEntityStates(IEntityManager entityMan, ICommonSession player, GameTick fromTick)
        {
            var stateEntities = new List<EntityState>();
            foreach (var entity in entityMan.GetEntities())
            {
                if (entity.Deleted)
                {
                    continue;
                }

                DebugTools.Assert(entity.Initialized);

                if (entity.LastModifiedTick >= fromTick)
                    stateEntities.Add(GetEntityState(entityMan, player, entity.Uid, fromTick));
            }

            // no point sending an empty collection
            return stateEntities.Count == 0 ? default : stateEntities;
        }
    }
}
