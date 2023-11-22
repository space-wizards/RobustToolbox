using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
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
using Prometheus;
using Robust.Server.Replays;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Robust.Server.GameStates
{
    /// <inheritdoc cref="IServerGameStateManager"/>
    [UsedImplicitly]
    public sealed class ServerGameStateManager : IServerGameStateManager, IPostInjectInit
    {
        // Mapping of net UID of clients -> last known acked state.
        private GameTick _lastOldestAck = GameTick.Zero;

        private PvsSystem _pvs = default!;

        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly INetworkedMapManager _mapManager = default!;
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IServerReplayRecordingManager _replay = default!;
        [Dependency] private readonly IServerEntityNetworkManager _entityNetworkManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IParallelManager _parallelMgr = default!;
        [Dependency] private readonly IConsoleHost _conHost = default!;

        private static readonly Histogram _usageHistogram = Metrics.CreateHistogram("robust_game_state_update_usage",
            "Amount of time spent processing different parts of the game state update", new HistogramConfiguration
            {
                LabelNames = new[] {"area"},
                Buckets = Histogram.ExponentialBuckets(0.000_001, 1.5, 25)
            });

        private ISawmill _logger = default!;

        private DefaultObjectPool<PvsThreadResources> _threadResourcesPool = default!;

        public ushort TransformNetId { get; set; }

        public Action<ICommonSession, GameTick>? ClientAck { get; set; }
        public Action<ICommonSession, GameTick, NetEntity?>? ClientRequestFull { get; set; }

        public void PostInject()
        {
            _logger = Logger.GetSawmill("gamestate");
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>();
            _networkManager.RegisterNetMessage<MsgStateLeavePvs>();
            _networkManager.RegisterNetMessage<MsgStateAck>(HandleStateAck);
            _networkManager.RegisterNetMessage<MsgStateRequestFull>(HandleFullStateRequest);

            _pvs = _entityManager.System<PvsSystem>();

            _parallelMgr.AddAndInvokeParallelCountChanged(ResetParallelism);

            _cfg.OnValueChanged(CVars.NetPVSCompressLevel, _ => ResetParallelism(), true);

            // temporary command for debugging PVS bugs.
            _conHost.RegisterCommand("print_pvs_ack", PrintPvsAckInfo);
        }

        private void PrintPvsAckInfo(IConsoleShell shell, string argstr, string[] args)
        {
            var ack = _pvs.PlayerData.Min(x => x.Value.LastReceivedAck);
            var players = _pvs.PlayerData
                .Where(x => x.Value.LastReceivedAck == ack)
                .Select(x => x.Key)
                .Select(x => $"{x.Name} ({_entityManager.ToPrettyString(x.AttachedEntity)})");

            shell.WriteLine($@"Current tick: {_gameTiming.CurTick}
Stored oldest acked tick: {_lastOldestAck}
Deletion history size: {_pvs.EntityPVSCollection.GetDeletedIndices(GameTick.First)?.Count ?? 0}
Actual oldest: {ack}
Oldest acked clients: {string.Join(", ", players)}
");
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

        internal sealed class PvsThreadResources
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

        private void HandleFullStateRequest(MsgStateRequestFull msg)
        {
            if (!_playerManager.TryGetSessionById(msg.MsgChannel.UserId, out var session))
                return;

            NetEntity? ent = msg.MissingEntity.IsValid() ? msg.MissingEntity : null;
            ClientRequestFull?.Invoke(session, msg.Tick, ent);
        }

        private void HandleStateAck(MsgStateAck msg)
        {
            if (_playerManager.TryGetSessionById(msg.MsgChannel.UserId, out var session))
                ClientAck?.Invoke(session, msg.Sequence);
        }

        /// <inheritdoc />
        public void SendGameStateUpdate()
        {
            var players = _playerManager.Sessions.Where(o => o.Status == SessionStatus.InGame).ToArray();

            // Update entity positions in PVS chunks/collections
            // TODO disable processing if culling is disabled? Need to check if toggling PVS breaks anything.
            // TODO parallelize?
            using (_usageHistogram.WithLabels("Update Collections").NewTimer())
            {
                _pvs.ProcessCollections();
            }

            // Figure out what chunks players can see and cache some chunk data.
            PvsData? pvsData = null;
            if (_pvs.CullingEnabled)
            {
                using var _ = _usageHistogram.WithLabels("Get Chunks").NewTimer();
                pvsData = GetPVSData(players);
            }

            // Update client acks, which is used to figure out what data needs to be sent to clients
            using (_usageHistogram.WithLabels("Process Acks").NewTimer())
            {
                _pvs.ProcessQueuedAcks();
            }

            // Construct & send the game state to each player.
            GameTick oldestAck;
            using (_usageHistogram.WithLabels("Send States").NewTimer())
            {
                oldestAck = SendStates(players, pvsData);
            }

            if (pvsData != null)
                _pvs.ReturnToPool(pvsData.Value.PlayerChunks);

            using (_usageHistogram.WithLabels("Clean Dirty").NewTimer())
            {
                _pvs.CleanupDirty(players);
            }

            if (oldestAck == GameTick.MaxValue)
            {
                // There were no connected players?
                // In that case we just clear all deletion history.
                _pvs.CullDeletionHistory(GameTick.MaxValue);
                _lastOldestAck = GameTick.Zero;
                return;
            }

            if (oldestAck == _lastOldestAck)
                return;

            _lastOldestAck = oldestAck;
            using var __ = _usageHistogram.WithLabels("Cull History").NewTimer();
            _pvs.CullDeletionHistory(oldestAck);
        }

        private GameTick SendStates(ICommonSession[] players, PvsData? pvsData)
        {
            var inputSystem = _systemManager.GetEntitySystem<InputSystem>();
            var opts = new ParallelOptions {MaxDegreeOfParallelism = _parallelMgr.ParallelProcessCount};
            var oldestAckValue = GameTick.MaxValue.Value;

            // Replays process game states in parallel with players
            Parallel.For(-1, players.Length, opts, _threadResourcesPool.Get, SendPlayer, _threadResourcesPool.Return);

            PvsThreadResources SendPlayer(int i, ParallelLoopState state, PvsThreadResources resource)
            {
                try
                {
                    var guid = i >= 0 ? players[i].UserId.UserId : default;

                    PvsEventSource.Log.WorkStart(_gameTiming.CurTick.Value, i, guid);

                    if (i >= 0)
                        SendStateUpdate(i, resource, inputSystem, players[i], pvsData, ref oldestAckValue);
                    else
                        _replay.Update();

                    PvsEventSource.Log.WorkStop(_gameTiming.CurTick.Value, i, guid);
                }
                catch (Exception e) // Catch EVERY exception
                {
                    _logger.Log(LogLevel.Error, e, "Caught exception while generating mail.");
                }
                return resource;
            }

            return new GameTick(oldestAckValue);
        }

        private struct PvsData
        {
            public HashSet<int>[] PlayerChunks;
            public EntityUid[][] ViewerEntities;
            public (Dictionary<NetEntity, MetaDataComponent> metadata, RobustTree<NetEntity> tree)?[] ChunkCache;
        }

        private PvsData? GetPVSData(ICommonSession[] players)
        {
            var (chunks, playerChunks, viewerEntities) = _pvs.GetChunks(players);
            const int ChunkBatchSize = 2;
            var chunksCount = chunks.Count;
            var chunkBatches = (int)MathF.Ceiling((float)chunksCount / ChunkBatchSize);
            var chunkCache =
                new (Dictionary<NetEntity, MetaDataComponent> metadata, RobustTree<NetEntity> tree)?[chunksCount];

            // Update the reused trees sequentially to avoid having to lock the dictionary per chunk.
            var reuse = ArrayPool<bool>.Shared.Rent(chunksCount);

            Parallel.For(0, chunkBatches,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelMgr.ParallelProcessCount },
                i =>
                {
                    var start = i * ChunkBatchSize;
                    var end = Math.Min(start + ChunkBatchSize, chunksCount);

                    for (var j = start; j < end; ++j)
                    {
                        var (visMask, chunkIndexLocation) = chunks[j];
                        reuse[j] = _pvs.TryCalculateChunk(chunkIndexLocation, visMask, out var chunk);
                        chunkCache[j] = chunk;

#if DEBUG
                        if (chunk == null)
                            continue;

                        // Each root nodes should simply be a map or a grid entity.
                        DebugTools.Assert(chunk.Value.tree.RootNodes.Count == 1,
                            $"Root node count is {chunk.Value.tree.RootNodes.Count} instead of 1.");
                        var nent = chunk.Value.tree.RootNodes.FirstOrDefault();
                        var ent = _entityManager.GetEntity(nent);
                        DebugTools.Assert(_entityManager.EntityExists(ent), $"Root node does not exist. Node {ent}.");
                        DebugTools.Assert(_entityManager.HasComponent<MapComponent>(ent)
                                          || _entityManager.HasComponent<MapGridComponent>(ent));
#endif
                    }
                });

            _pvs.RegisterNewPreviousChunkTrees(chunks, chunkCache, reuse);
            ArrayPool<bool>.Shared.Return(reuse);
            return new PvsData()
            {
                PlayerChunks = playerChunks,
                ViewerEntities = viewerEntities,
                ChunkCache = chunkCache,
            };
        }

        private void SendStateUpdate(int i,
            PvsThreadResources resources,
            InputSystem inputSystem,
            ICommonSession session,
            PvsData? pvsData,
            ref uint oldestAckValue)
        {
            var channel = session.Channel;
            var sessionData = _pvs.PlayerData[session];
            var lastAck = sessionData.LastReceivedAck;
            List<NetEntity>? leftPvs = null;
            List<EntityState>? entStates;
            List<NetEntity>? deletions;
            GameTick fromTick;

            DebugTools.Assert(_pvs.CullingEnabled == (pvsData != null));
            if (pvsData != null)
            {
                (entStates, deletions, leftPvs, fromTick) = _pvs.CalculateEntityStates(
                    session,
                    lastAck,
                    _gameTiming.CurTick,
                    pvsData.Value.ChunkCache,
                    pvsData.Value.PlayerChunks[i],
                    pvsData.Value.ViewerEntities[i]);
            }
            else
            {
                (entStates, deletions, fromTick) = _pvs.GetAllEntityStates(session, lastAck, _gameTiming.CurTick);
            }

            var playerStates = _playerManager.GetPlayerStates(fromTick);

            // lastAck varies with each client based on lag and such, we can't just make 1 global state and send it to everyone
            var lastInputCommand = inputSystem.GetLastInputCommand(session);
            var lastSystemMessage = _entityNetworkManager.GetLastMessageSequence(session);

            var state = new GameState(
                fromTick,
                _gameTiming.CurTick,
                Math.Max(lastInputCommand, lastSystemMessage),
                entStates,
                playerStates,
                deletions);

            InterlockedHelper.Min(ref oldestAckValue, lastAck.Value);

            // actually send the state
            var stateUpdateMessage = new MsgState();
            stateUpdateMessage.State = state;
            stateUpdateMessage.CompressionContext = resources.CompressionContext;

            // If the state is too big we let Lidgren send it reliably. This is to avoid a situation where a state is so
            // large that it (or part of it) consistently gets dropped. When we send reliably, we immediately update the
            // ack so that the next state will not also be huge.
            //
            // We also do this if the client's last ack is too old. This helps prevent things like the entity deletion
            // history from becoming too bloated if a bad client fails to send acks for whatever reason.

            if (_gameTiming.CurTick.Value > lastAck.Value + _pvs.ForceAckThreshold)
            {
                stateUpdateMessage.ForceSendReliably = true;
#if FULL_RELEASE
                var connectedTime = (DateTime.UtcNow - session.ConnectedTime).TotalMinutes;
                if (lastAck > GameTick.Zero && connectedTime > 1)
                    _logger.Warning($"Client {session} exceeded ack-tick threshold. Last ack: {lastAck}. Cur tick: {_gameTiming.CurTick}. Connect time: {connectedTime} minutes");
#endif
            }

            _networkManager.ServerSendMessage(stateUpdateMessage, channel);

            if (stateUpdateMessage.ShouldSendReliably())
            {
                sessionData.LastReceivedAck = _gameTiming.CurTick;
                lock (_pvs.PendingAcks)
                {
                    _pvs.PendingAcks.Add(session);
                }
            }

            // Send PVS detach / left-view messages separately and reliably. This is not resistant to packet loss, but
            // unlike game state it doesn't really matter. This also significantly reduces the size of game state
            // messages as PVS chunks get moved out of view.
            if (leftPvs != null && leftPvs.Count > 0)
            {
                var pvsMessage = new MsgStateLeavePvs {Entities = leftPvs, Tick = _gameTiming.CurTick};
                _networkManager.ServerSendMessage(pvsMessage, channel);
            }
        }

        [EventSource(Name = "Robust.Pvs")]
        public sealed class PvsEventSource : System.Diagnostics.Tracing.EventSource
        {
            public static PvsEventSource Log { get; } = new();

            [Event(1)]
            public void WorkStart(uint tick, int playerIdx, Guid playerGuid) => WriteEvent(1, tick, playerIdx, playerGuid);

            [Event(2)]
            public void WorkStop(uint tick, int playerIdx, Guid playerGuid) => WriteEvent(2, tick, playerIdx, playerGuid);

            [NonEvent]
            private unsafe void WriteEvent(int eventId, uint arg1, int arg2, Guid arg3)
            {
                if (IsEnabled())
                {
                    var descrs = stackalloc EventData[3];

                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 4;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = 16;

                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }
    }
}
