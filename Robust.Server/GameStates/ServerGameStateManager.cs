using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates
{
    /// <inheritdoc cref="IServerGameStateManager"/>
    public class ServerGameStateManager : IServerGameStateManager, IPostInjectInit
    {
        // Mapping of net UID of clients -> last known acked state.
        private readonly Dictionary<long, GameTick> _ackedStates = new();
        private GameTick _lastOldestAck = GameTick.Zero;

        private EntityViewCulling _entityView = null!;

        [Dependency] private readonly IServerEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
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

        public void PostInject()
        {
            _logger = Logger.GetSawmill("PVS");
            _entityView = new EntityViewCulling(_entityManager, _mapManager);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>(MsgState.NAME);
            _networkManager.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME, HandleStateAck);

            _networkManager.Connected += HandleClientConnected;
            _networkManager.Disconnect += HandleClientDisconnect;

            _playerManager.PlayerStatusChanged += HandlePlayerStatusChanged;

            _entityManager.EntityDeleted += HandleEntityDeleted;
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

            var oldestAck = GameTick.MaxValue;

            var mainThread = Thread.CurrentThread;
            (MsgState, INetChannel) GenerateMail(IPlayerSession session)
            {
                // KILL IT WITH FIRE
                if(mainThread != Thread.CurrentThread)
                    IoCManager.InitThread(new DependencyCollection(), true);

                // people not in the game don't get states
                if (session.Status != SessionStatus.InGame)
                {
                    return default;
                }

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

            (MsgState, INetChannel?) SafeGenerateMail(IPlayerSession session)
            {
                try
                {
                    return GenerateMail(session);
                }
                catch (Exception e) // Catch EVERY exception
                {
                    _logger.Log(LogLevel.Error, e, string.Empty);
                }

                return (new MsgState(session.ConnectedClient), null);
            }

            var mailBag = _playerManager.GetAllPlayers()
                .Where(s => s.Status == SessionStatus.InGame)
                .AsParallel()
                .Select(SafeGenerateMail);

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
                _entityView.CullDeletionHistory(oldestAck);
                _mapManager.CullDeletionHistory(oldestAck);
            }
        }

        /// <summary>
        /// Generates a network entity state for the given entity.
        /// </summary>
        /// <param name="compMan">ComponentManager that contains the components for the entity.</param>
        /// <param name="player">The player to generate this state for.</param>
        /// <param name="entityUid">Uid of the entity to generate the state from.</param>
        /// <param name="fromTick">Only provide delta changes from this tick.</param>
        /// <returns>New entity State for the given entity.</returns>
        internal static EntityState GetEntityState(IComponentManager compMan, ICommonSession player, EntityUid entityUid, GameTick fromTick)
        {
            var compStates = new List<ComponentState>();
            var changed = new List<ComponentChanged>();

            foreach (var comp in compMan.GetNetComponents(entityUid))
            {
                DebugTools.Assert(comp.Initialized);

                // NOTE: When LastModifiedTick or CreationTick are 0 it means that the relevant data is
                // "not different from entity creation".
                // i.e. when the client spawns the entity and loads the entity prototype,
                // the data it deserializes from the prototype SHOULD be equal
                // to what the component state / ComponentChanged would send.
                // As such, we can avoid sending this data in this case since the client "already has it".

                if (comp.NetSyncEnabled && comp.LastModifiedTick != GameTick.Zero && comp.LastModifiedTick >= fromTick)
                    compStates.Add(comp.GetComponentState(player));

                if (comp.CreationTick != GameTick.Zero && comp.CreationTick >= fromTick && !comp.Deleted)
                {
                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChanged.Added(comp.NetID!.Value, comp.Name));
                }
                else if (comp.Deleted && comp.LastModifiedTick >= fromTick)
                {
                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChanged.Removed(comp.NetID!.Value));
                }
            }

            return new EntityState(entityUid, changed.ToArray(), compStates.ToArray());
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

                if (entity.LastModifiedTick <= fromTick)
                    continue;

                stateEntities.Add(GetEntityState(entityMan.ComponentManager, player, entity.Uid, fromTick));
            }

            // no point sending an empty collection
            return stateEntities.Count == 0 ? default : stateEntities;
        }
    }
}
