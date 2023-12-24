using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
        [Dependency] private readonly IEntitySystemManager _systemManager = default!;
        [Dependency] private readonly IServerReplayRecordingManager _replay = default!;
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
            public ZStdCompressionContext CompressionContext = new();

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

            // Update client acks, which is used to figure out what data needs to be sent to clients
            // This only needs SessionData which isn't touched during GetPVSData or ProcessCollections.
            var ackJob = _pvs.ProcessQueuedAcks();

            // Figure out what chunks players can see and cache some chunk data.
            if (_pvs.CullingEnabled)
            {
                using var _ = _usageHistogram.WithLabels("Get Chunks").NewTimer();
                _pvs.ProcessChunks(players);
            }

            ackJob.WaitOne();

            // Construct & send the game state to each player.
            GameTick oldestAck;
            using (_usageHistogram.WithLabels("Send States").NewTimer())
            {
                oldestAck = SendStates(players);
            }

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

        private GameTick SendStates(ICommonSession[] players)
        {
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
                        SendStateUpdate(resource, players[i], ref oldestAckValue);
                    else
                        _replay.Update();

                    PvsEventSource.Log.WorkStop(_gameTiming.CurTick.Value, i, guid);
                }
                catch (Exception e) // Catch EVERY exception
                {
                    var source = i >= 0 ? players[i].ToString() : "replays";
                    _logger.Log(LogLevel.Error, e, $"Caught exception while generating mail for {source}.");
                }
                return resource;
            }

            return new GameTick(oldestAckValue);
        }

        private void SendStateUpdate(
            PvsThreadResources resources,
            ICommonSession session,
            ref uint oldestAckValue)
        {
            var data = _pvs.GetSessionData(session);
            InterlockedHelper.Min(ref oldestAckValue, data.FromTick.Value);

            // actually send the state
            var stateUpdateMessage = new MsgState
            {
                State = data.State,
                CompressionContext = resources.CompressionContext
            };

            _networkManager.ServerSendMessage(stateUpdateMessage, session.Channel);
            data.ClearState();

            if (stateUpdateMessage.ShouldSendReliably())
            {
                data.LastReceivedAck = _gameTiming.CurTick;
                lock (_pvs.PendingAcks)
                {
                    _pvs.PendingAcks.Add(session);
                }
            }

            // TODO parallelize this with system processing.
            // Before we do that we need to:
            // - Defer player connection changes untill the start of the nxt PVS tick and  this job has finished
            // - Defer OnEntityDeleted in pvs system. Or refactor per-session entity data to be stored as arrays on metadaat component
            _pvs.ProcessLeavePvs(data);
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
