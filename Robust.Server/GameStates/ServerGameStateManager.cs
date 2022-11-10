using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharpZstd.Interop;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Players;

namespace Robust.Server.GameStates
{
    /// <inheritdoc cref="IServerGameStateManager"/>
    [UsedImplicitly]
    public sealed class ServerGameStateManager : IServerGameStateManager, IPostInjectInit
    {
        // Mapping of net UID of clients -> last known acked state.
        private readonly Dictionary<long, GameTick> _ackedStates = new();
        private GameTick _lastOldestAck = GameTick.Zero;

        private PVSSystem _pvs = default!;

        [Dependency] private readonly IServerEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly INetworkedMapManager _mapManager = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IServerEntityNetworkManager _entityNetworkManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IParallelManager _parallelMgr = default!;

        private ISawmill _logger = default!;

        private DefaultObjectPool<PvsThreadResources> _threadResourcesPool = default!;

        public ushort TransformNetId { get; set; }

        public Action<ICommonSession, GameTick, GameTick>? ClientAck { get; set; }
        public Action<ICommonSession, GameTick, GameTick, EntityUid?>? ClientRequestFull { get; set; }

        public void PostInject()
        {
            _logger = Logger.GetSawmill("PVS");
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>();
            _networkManager.RegisterNetMessage<MsgStateLeavePvs>();
            _networkManager.RegisterNetMessage<MsgStateAck>(HandleStateAck);
            _networkManager.RegisterNetMessage<MsgStateRequestFull>(HandleFullStateRequest);

            _networkManager.Connected += HandleClientConnected;
            _networkManager.Disconnect += HandleClientDisconnect;

            _pvs = EntitySystem.Get<PVSSystem>();

            _parallelMgr.AddAndInvokeParallelCountChanged(ResetParallelism);

            _cfg.OnValueChanged(CVars.NetPVSCompressLevel, _ => ResetParallelism(), true);
        }

        private void ResetParallelism()
        {
            var compressLevel = _cfg.GetCVar(CVars.NetPVSCompressLevel);
            // The * 2 is because trusting .NET won't take more is what got this code into this mess in the first place.
            _threadResourcesPool = new DefaultObjectPool<PvsThreadResources>(new PvsThreadResourcesObjectPolicy(compressLevel), _parallelMgr.ParallelProcessCount * 2);
        }

        private sealed class PvsThreadResourcesObjectPolicy : IPooledObjectPolicy<PvsThreadResources>
        {
            public int CompressionLevel;

            public PvsThreadResourcesObjectPolicy(int ce)
            {
                CompressionLevel = ce;
            }

            PvsThreadResources IPooledObjectPolicy<PvsThreadResources>.Create()
            {
                var res = new PvsThreadResources();
                res.CompressionContext.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, CompressionLevel);
                return res;
            }

            bool IPooledObjectPolicy<PvsThreadResources>.Return(PvsThreadResources _)
            {
                return true;
            }
        }

        private sealed class PvsThreadResources
        {
            public ZStdCompressionContext CompressionContext;

            public PvsThreadResources()
            {
                CompressionContext = new ZStdCompressionContext();
            }

            ~PvsThreadResources()
            {
                CompressionContext.Dispose();
            }
        }

        private void HandleClientConnected(object? sender, NetChannelArgs e)
        {
            _ackedStates[e.Channel.ConnectionId] = GameTick.Zero;
        }

        private void HandleClientDisconnect(object? sender, NetChannelArgs e)
        {
            _ackedStates.Remove(e.Channel.ConnectionId);
        }

        private void HandleFullStateRequest(MsgStateRequestFull msg)
        {
            if (!_playerManager.TryGetSessionById(msg.MsgChannel.UserId, out var session) ||
                !_ackedStates.TryGetValue(msg.MsgChannel.ConnectionId, out var lastAcked))
                return;

            EntityUid? ent = msg.MissingEntity.IsValid() ? msg.MissingEntity : null;
            ClientRequestFull?.Invoke(session, msg.Tick, lastAcked, ent);

            // Update acked tick so that OnClientAck doesn't get invoked by any late acks.
            _ackedStates[msg.MsgChannel.ConnectionId] = _gameTiming.CurTick;
        }

        private void HandleStateAck(MsgStateAck msg)
        {
            if (_playerManager.TryGetSessionById(msg.MsgChannel.UserId, out var session))
                Ack(msg.MsgChannel.ConnectionId, msg.Sequence, session);
        }

        private void Ack(long uniqueIdentifier, GameTick stateAcked, IPlayerSession playerSession)
        {
            if (!_ackedStates.TryGetValue(uniqueIdentifier, out var lastAck) || stateAcked <= lastAck)
                return;

            ClientAck?.Invoke(playerSession, stateAcked, lastAck);
            _ackedStates[uniqueIdentifier] = stateAcked;
        }

        /// <inheritdoc />
        public void SendGameStateUpdate()
        {
            if (!_networkManager.IsConnected)
            {
                // Prevent deletions piling up if we have no clients.
                _pvs.CullDeletionHistory(GameTick.MaxValue);
                _mapManager.CullDeletionHistory(GameTick.MaxValue);
                _pvs.Cleanup(_playerManager.ServerSessions);
                return;
            }

            var inputSystem = _systemManager.GetEntitySystem<InputSystem>();

            var oldestAckValue = GameTick.MaxValue.Value;

            var mainThread = Thread.CurrentThread;
            var parentDeps = IoCManager.Instance!;

            _pvs.ProcessCollections();

            // people not in the game don't get states
            var players = _playerManager.ServerSessions.Where(o => o.Status == SessionStatus.InGame).ToArray();

            //todo paul oh my god make this less shit
            EntityQuery<MetaDataComponent> metadataQuery = default!;
            EntityQuery<TransformComponent> transformQuery = default!;
            HashSet<int>[] playerChunks = default!;
            EntityUid[][] viewerEntities = default!;
            (Dictionary<EntityUid, MetaDataComponent> metadata, RobustTree<EntityUid> tree)?[] chunkCache = default!;

            if (_pvs.CullingEnabled)
            {
                List<(uint, IChunkIndexLocation)> chunks;
                (chunks, playerChunks, viewerEntities) = _pvs.GetChunks(players);
                const int ChunkBatchSize = 2;
                var chunksCount = chunks.Count;
                var chunkBatches = (int)MathF.Ceiling((float)chunksCount / ChunkBatchSize);
                chunkCache =
                    new (Dictionary<EntityUid, MetaDataComponent> metadata, RobustTree<EntityUid> tree)?[chunksCount];

                // Update the reused trees sequentially to avoid having to lock the dictionary per chunk.
                var reuse = ArrayPool<bool>.Shared.Rent(chunksCount);

                transformQuery = _entityManager.GetEntityQuery<TransformComponent>();
                metadataQuery = _entityManager.GetEntityQuery<MetaDataComponent>();
                Parallel.For(0, chunkBatches, i =>
                {
                    var start = i * ChunkBatchSize;
                    var end = Math.Min(start + ChunkBatchSize, chunksCount);

                    for (var j = start; j < end; ++j)
                    {
                        var (visMask, chunkIndexLocation) = chunks[j];
                        reuse[j] = _pvs.TryCalculateChunk(chunkIndexLocation, visMask, transformQuery, metadataQuery,
                            out var chunk);
                        chunkCache[j] = chunk;
                    }
                });

                _pvs.RegisterNewPreviousChunkTrees(chunks, chunkCache, reuse);
                ArrayPool<bool>.Shared.Return(reuse);
            }

            Parallel.For(
                0, players.Length,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelMgr.ParallelProcessCount },
                () => _threadResourcesPool.Get(),
                (i, _, resource) =>
                {
                    try
                    {
                        SendStateUpdate(i, resource);
                    }
                    catch (Exception e) // Catch EVERY exception
                    {
                        _logger.Log(LogLevel.Error, e, "Caught exception while generating mail.");
                    }
                    return resource;
                },
                resource => _threadResourcesPool.Return(resource)
            );

            void SendStateUpdate(int sessionIndex, PvsThreadResources resources)
            {
                var session = players[sessionIndex];

                // KILL IT WITH FIRE
                if (mainThread != Thread.CurrentThread)
                    IoCManager.InitThread(new DependencyCollection(parentDeps), true);

                var channel = session.ConnectedClient;

                if (!_ackedStates.TryGetValue(channel.ConnectionId, out var lastAck))
                {
                    DebugTools.Assert("Why does this channel not have an entry?");
                }

                var (entStates, deletions, leftPvs, fromTick) = _pvs.CullingEnabled
                    ? _pvs.CalculateEntityStates(session, lastAck, _gameTiming.CurTick, chunkCache,
                        playerChunks[sessionIndex], metadataQuery, transformQuery, viewerEntities[sessionIndex])
                    : _pvs.GetAllEntityStates(session, lastAck, _gameTiming.CurTick);
                var playerStates = _playerManager.GetPlayerStates(lastAck);
                var mapData = _mapManager.GetStateData(lastAck);

                // lastAck varies with each client based on lag and such, we can't just make 1 global state and send it to everyone
                var lastInputCommand = inputSystem.GetLastInputCommand(session);
                var lastSystemMessage = _entityNetworkManager.GetLastMessageSequence(session);

                var state = new GameState(
                    fromTick,
                    _gameTiming.CurTick,
                    Math.Max(lastInputCommand, lastSystemMessage),
                    entStates,
                    playerStates,
                    deletions,
                    mapData);

                InterlockedHelper.Min(ref oldestAckValue, lastAck.Value);

                // actually send the state
                var stateUpdateMessage = new MsgState();
                stateUpdateMessage.State = state;
                stateUpdateMessage.CompressionContext = resources.CompressionContext;

                _networkManager.ServerSendMessage(stateUpdateMessage, channel);

                // If the state is too big we let Lidgren send it reliably.
                // This is to avoid a situation where a state is so large that it consistently gets dropped
                // (or, well, part of it).
                // When we send them reliably, we immediately update the ack so that the next state will not be huge.
                if (stateUpdateMessage.ShouldSendReliably())
                {
                    // TODO: remove this lock by having a single state object per session that contains all per-session state needed.
                    lock (_ackedStates)
                    {
                        Ack(channel.ConnectionId, _gameTiming.CurTick, session);
                    }
                }

                // separately, we send PVS detach / left-view messages reliably. This is not resistant to packet loss,
                // but unlike game state it doesn't really matter. This also significantly reduces the size of game
                // state messages PVS chunks move out of view.
                if (leftPvs != null && leftPvs.Count > 0)
                    _networkManager.ServerSendMessage(new MsgStateLeavePvs() { Entities = leftPvs, Tick = _gameTiming.CurTick }, channel);
            }

            if (_pvs.CullingEnabled)
                _pvs.ReturnToPool(playerChunks);
            _pvs.Cleanup(_playerManager.ServerSessions);
            var oldestAck = new GameTick(oldestAckValue);

            // keep the deletion history buffers clean
            if (oldestAck > _lastOldestAck)
            {
                _lastOldestAck = oldestAck;
                _pvs.CullDeletionHistory(oldestAck);
                _mapManager.CullDeletionHistory(oldestAck);
            }
        }
    }
}
