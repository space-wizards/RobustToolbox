using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Prometheus;
using Robust.Server.Configuration;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Server.Replays;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly INetworkedMapManager _mapManager = default!;
    [Dependency] private readonly IServerEntityNetworkManager _netEntMan = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IParallelManager _parallelManager = default!;
    [Dependency] private readonly IServerGameStateManager _serverGameStateManager = default!;
    [Dependency] private readonly IServerNetConfigurationManager _netConfigManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InputSystem _input = default!;
    [Dependency] private readonly IServerNetManager _netMan = default!;
    [Dependency] private readonly IParallelManagerInternal _parallelMgr = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IServerReplayRecordingManager _replay = default!;

    // TODO make this a cvar. Make it in terms of seconds and tie it to tick rate?
    // Main issue is that I CBF figuring out the logic for handling it changing mid-game.
    public const int DirtyBufferSize = 20;
    // Note: If a client has ping higher than TickBuffer / TickRate, then the server will treat every entity as if it
    // had entered PVS for the first time. Note that due to the PVS budget, this buffer is easily overwhelmed.

    /// <summary>
    /// See <see cref="CVars.NetForceAckThreshold"/>.
    /// </summary>
    public int ForceAckThreshold { get; private set; }

    /// <summary>
    /// Is view culling enabled, or will we send the whole map?
    /// </summary>
    public bool CullingEnabled { get; private set; }

    /// <summary>
    /// Size of the side of the view bounds square. Related to <see cref="CVars.NetMaxUpdateRange"/>
    /// </summary>
    private float _viewSize;

    /// <summary>
    /// Size of the side of the priority view bounds square. Related to <see cref="CVars.NetPvsPriorityRange"/>
    /// </summary>
    private float _priorityViewSize;

    /// <summary>
    /// Per-tick ack data to avoid re-allocating.
    /// </summary>
    private readonly List<PvsSession> _toAck = new();
    internal readonly HashSet<ICommonSession> PendingAcks = new();

    private PvsAckJob _ackJob;
    private PvsChunkJob _chunkJob;
    private PvsLeaveJob _leaveJob;
    private PvsDeletionsJob _deletionJob;

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private uint _oldestAck;
    private GameTick _lastOldestAck = GameTick.Zero;

    /// <summary>
    /// List of recently deleted entities.
    /// </summary>
    private readonly List<NetEntity> _deletedEntities = new();

    /// <summary>
    /// The tick at which each entity was deleted.
    /// </summary>
    private readonly List<GameTick> _deletedTick = new();

    private readonly HashSet<EntityUid> _toDelete = new();

    /// <summary>
    /// The sessions that are currently being processed. Note that this is in general used by parallel & async tasks.
    /// Hence player disconnection processing is deferred and only run via <see cref="ProcessDisconnections"/>.
    /// </summary>
    private PvsSession[] _sessions = default!;

    private bool _async;

    private DefaultObjectPool<PvsThreadResources> _threadResourcesPool = default!;

    private static readonly Histogram Histogram = Metrics.CreateHistogram("robust_game_state_update_usage",
        "Amount of time spent processing different parts of the game state update", new HistogramConfiguration
        {
            LabelNames = new[] {"area"},
            Buckets = Histogram.ExponentialBuckets(0.000_001, 1.5, 25)
        });

    public override void Initialize()
    {
        base.Initialize();

        if (Marshal.SizeOf<PvsMetadata>() != Marshal.SizeOf<PvsData>())
            throw new Exception($"Pvs struct sizes must match");

        _deletionJob = new PvsDeletionsJob(this);
        _leaveJob = new PvsLeaveJob(this);
        _chunkJob = new PvsChunkJob(this);
        _ackJob = new PvsAckJob(this);

        _eyeQuery = GetEntityQuery<EyeComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<MapRemovedEvent>(OnMapChanged);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        SubscribeLocalEvent<TransformComponent, TransformStartupEvent>(OnTransformStartup);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _transform.OnBeforeMoveEvent += OnEntityMove;
        EntityManager.EntityAdded += OnEntityAdded;
        EntityManager.EntityDeleted += OnEntityDeleted;
        EntityManager.AfterEntityFlush += AfterEntityFlush;
        EntityManager.BeforeEntityTerminating += OnEntityTerminating;

        Subs.CVar(_configManager, CVars.NetPVS, SetPvs, true);
        Subs.CVar(_configManager, CVars.NetMaxUpdateRange, OnViewsizeChanged, true);
        Subs.CVar(_configManager, CVars.NetPvsPriorityRange, OnPriorityRangeChanged, true);
        Subs.CVar(_configManager, CVars.NetForceAckThreshold, OnForceAckChanged, true);
        Subs.CVar(_configManager, CVars.NetPvsAsync, OnAsyncChanged, true);
        Subs.CVar(_configManager, CVars.NetPvsCompressLevel, ResetParallelism, true);

        _serverGameStateManager.ClientAck += OnClientAck;
        _serverGameStateManager.ClientRequestFull += OnClientRequestFull;
        _parallelMgr.ParallelCountChanged += ResetParallelism;

        InitializeDirty();
        InitializePvsArray();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        _transform.OnBeforeMoveEvent -= OnEntityMove;
        EntityManager.EntityAdded -= OnEntityAdded;
        EntityManager.EntityDeleted -= OnEntityDeleted;
        EntityManager.AfterEntityFlush -= AfterEntityFlush;
        EntityManager.BeforeEntityTerminating -= OnEntityTerminating;

        _parallelMgr.ParallelCountChanged -= ResetParallelism;

        _serverGameStateManager.ClientAck -= OnClientAck;
        _serverGameStateManager.ClientRequestFull -= OnClientRequestFull;

        ClearPvsData();
        ShutdownDirty();
    }

    public override void Update(float frameTime)
    {
        ProcessDeletions();
    }

    /// <summary>
    /// Send this tick's game state data to players.
    /// </summary>
    internal void SendGameStates(ICommonSession[] players)
    {
        // Wait for pending jobs and process disconnected players
        ProcessDisconnections();

        // Ensure each session has a PvsSession entry before starting any parallel jobs.
        CacheSessionData(players);

        // Get visible chunks, and update any dirty chunks.
        BeforeSerializeStates();

        // Construct & serialize the game state for each player (and for the replay).
        SerializeStates();

        foreach (var uid in _toDelete)
        {
            EntityManager.QueueDeleteEntity(uid);
        }
        _toDelete.Clear();

        // Compress & send the states.
        SendStates();

        // Cull deletion history
        AfterSerializeStates();

        ProcessLeavePvs();
    }

    private void ResetParallelism(int _) => ResetParallelism();
    private void ResetParallelism()
    {
        var compressLevel = _configManager.GetCVar(CVars.NetPvsCompressLevel);
        // The * 2 is because trusting .NET won't take more is what got this code into this mess in the first place.
        _threadResourcesPool = new DefaultObjectPool<PvsThreadResources>(new PvsThreadResourcesObjectPolicy(compressLevel), _parallelMgr.ParallelProcessCount * 2);
    }

    private void OnAsyncChanged(bool value)
    {
        _async = value;
    }

    // TODO PVS rate limit this?
    private void OnClientRequestFull(ICommonSession session, GameTick tick, NetEntity? missingEntity)
    {
        if (!PlayerData.TryGetValue(session, out var pvsSession))
            return;

        var lastAcked = pvsSession.LastReceivedAck;

        var sb = new StringBuilder();
        sb.Append($"Client {session} requested full state on tick {tick}. Last Acked: {lastAcked}. Curtick: {_gameTiming.CurTick}.");

        if (missingEntity != null)
        {
            var (entity, meta) = GetEntityData(missingEntity.Value);
            sb.Append($" Apparently they received an entity without metadata: {ToPrettyString(entity)}.");
            //sb.Append($" Entity last seen: {meta.PvsData[sessionData.Index].EntityLastAcked}");
        }

        Log.Warning(sb.ToString());
        ForceFullState(pvsSession);
    }

    private void ForceFullState(PvsSession session)
    {
        _leaveTask?.WaitOne();
        _leaveTask = null;
        session.LastReceivedAck = _gameTiming.CurTick;
        session.RequestedFull = true;
        ClearSendHistory(session);
        ClearPlayerPvsData(session);
    }

    private void OnViewsizeChanged(float value)
    {
        _viewSize = Math.Max(ChunkSize, value);
        OnPriorityRangeChanged(_configManager.GetCVar(CVars.NetPvsPriorityRange));
    }

    private void OnPriorityRangeChanged(float value)
    {
        _priorityViewSize = Math.Max(_viewSize, value);
    }

    private void OnForceAckChanged(int value)
    {
        ForceAckThreshold = value;
    }

    private void SetPvs(bool value)
    {
        _seenAllEnts.Clear();
        CullingEnabled = value;
    }

    private void CullDeletionHistory(GameTick oldestAck)
    {
        using var _ = Histogram.WithLabels("Cull History").NewTimer();
        CullDeletionHistoryUntil(oldestAck);
        _mapManager.CullDeletionHistory(oldestAck);
    }

    private void GetEntityStates(PvsSession session)
    {
        // First, we send the client's own viewers. we want to ALWAYS send these, regardless of any pvs budget.
        AddForcedEntities(session);

        // After processing the entity's viewers, we set actual, budget limits.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (session.Channel != null)
        {
            session.Budget.NewLimit= _netConfigManager.GetClientCVar(session.Channel, CVars.NetPVSEntityBudget);
            session.Budget.EnterLimit = _netConfigManager.GetClientCVar(session.Channel, CVars.NetPVSEntityEnterBudget);
        }
        else
        {
            session.Budget.NewLimit= CVars.NetPVSEntityBudget.DefaultValue;
            session.Budget.EnterLimit = CVars.NetPVSEntityEnterBudget.DefaultValue;
        }

        // Process all PVS overrides.
        AddAllOverrides(session);

        // Process all entities in visible PVS chunks
        AddPvsChunks(session);

#if DEBUG
        VerifySessionData(session);
#endif

        var toSend = session.ToSend!;
        session.ToSend = null;

        // Add the constructed list of visible entities to this client's history.
        if (!session.PreviouslySent.Add(_gameTiming.CurTick, toSend, out var oldEntry))
            return;

        var fromTick = session.FromTick;
        if (oldEntry.Value.Key <= fromTick || session.Overflow != null)
        {
            _entDataListPool.Return(oldEntry.Value.Value);
            return;
        }

        // The clients last ack is too late, the overflow dictionary size has been exceeded, and we will no
        // longer have information about the sent entities. This means we would no longer be able to add
        // entities to _ackedEnts.
        //
        // If the client has enough latency, this result in a situation where we must constantly assume that every entity
        // that needs to get sent to the client is being received by them for the first time.
        //
        // In order to avoid this, while also keeping the overflow dictionary limited in size, we keep a single
        // overflow state, so we can at least periodically update the acked entities.

        // This is pretty shit and there is probably a better way of doing this.
        session.Overflow = oldEntry.Value;
    }

#if DEBUG
    private void VerifySessionData(PvsSession pvsSession)
    {
        var toSend = pvsSession.ToSend!;
        var toSendSet = pvsSession.ToSendSet;
        toSendSet.Clear();

        foreach (var intPtr in toSend)
        {
            toSendSet.Add(IndexToNetEntity(intPtr));
        }
        DebugTools.AssertEqual(toSend.Count, toSendSet.Count);

        foreach (var intPtr in CollectionsMarshal.AsSpan(toSend))
        {
            ref var data = ref pvsSession.DataMemory.GetRef(intPtr.Index);
            DebugTools.AssertEqual(data.LastSeen, _gameTiming.CurTick);
        }

        pvsSession.PreviouslySent.TryGetValue(_gameTiming.CurTick - 1, out var lastSent);
        foreach (var intPtr in CollectionsMarshal.AsSpan(lastSent))
        {
            ref var data = ref pvsSession.DataMemory.GetRef(intPtr.Index);
            DebugTools.Assert(data.LastSeen != GameTick.Zero);
            DebugTools.AssertEqual(toSendSet.Contains(IndexToNetEntity(intPtr)), data.LastSeen == _gameTiming.CurTick);
            DebugTools.Assert(data.LastSeen == _gameTiming.CurTick
                              || data.LastSeen == _gameTiming.CurTick - 1);
        }
    }
#endif

    private (Vector2 worldPos, float range, EntityUid? map) CalcViewBounds(Entity<TransformComponent, EyeComponent?> eye)
    {
        var size = _priorityViewSize;
        var worldPos = _transform.GetWorldPosition(eye.Comp1);

        if (eye.Comp2 is not null)
        {
            // not using EyeComponent.Eye.Position, because it's updated only on the client's side
            worldPos += eye.Comp2.Offset;
            size *= eye.Comp2.PvsScale;
        }

        size = Math.Max(size, 1);

        return (worldPos, size / 2f, eye.Comp1.MapUid);
    }

    private void CullDeletionHistoryUntil(GameTick tick)
    {
        if (tick == GameTick.MaxValue)
        {
            _deletedEntities.Clear();
            _deletedTick.Clear();
            return;
        }

        for (var i = _deletedEntities.Count - 1; i >= 0; i--)
        {
            var delTick = _deletedTick[i];
            if (delTick > tick)
                continue;

            _deletedEntities.RemoveSwap(i);
            _deletedTick.RemoveSwap(i);
        }
    }

    private void BeforeSerializeStates()
    {
        DebugTools.Assert(_chunks.Values.All(x => Exists(x.Map) && Exists(x.Root)));
        DebugTools.Assert(_chunkSets.Keys.All(Exists));

        var ackJob = ProcessQueuedAcks();

        // Figure out what chunks players can see and cache some chunk data.
        if (CullingEnabled)
        {
            GetVisibleChunks();
            ProcessVisibleChunks();
        }

        ackJob?.WaitOne();
    }

    internal void ProcessDisconnections()
    {
        _leaveTask?.WaitOne();
        _leaveTask = null;

        foreach (var session in _disconnected)
        {
            if (PlayerData.Remove(session, out var pvsSession))
            {
                ClearSendHistory(pvsSession);
                FreeSessionDataMemory(pvsSession);
            }
        }
    }

    internal void CacheSessionData(ICommonSession[] players)
    {
        Array.Resize(ref _sessions, players.Length);
        for (var i = 0; i < players.Length; i++)
        {
            _sessions[i] = GetOrNewPvsSession(players[i]);
        }
    }

    private void AfterSerializeStates()
    {
        CleanupDirty();

        if (_oldestAck == GameTick.MaxValue.Value)
        {
            // There were no connected players?
            // In that case we just clear all deletion history.
            CullDeletionHistory(GameTick.MaxValue);
            _lastOldestAck = GameTick.Zero;
            return;
        }

        if (_oldestAck == _lastOldestAck.Value)
            return;

        _lastOldestAck = new(_oldestAck);
        CullDeletionHistory(_lastOldestAck);
    }
}

[ByRefEvent]
public struct ExpandPvsEvent(ICommonSession session, int mask)
{
    public readonly ICommonSession Session = session;

    /// <summary>
    /// List of entities that will get added to this session's PVS set. This will still respect visibility masks.
    /// </summary>
    public List<EntityUid>? Entities;

    /// <summary>
    /// List of entities that will get added to this session's PVS set. Unlike <see cref="Entities"/> this will also
    /// recursively add all children of the given entity. This will still respect visibility masks.
    /// </summary>
    public List<EntityUid>? RecursiveEntities;

    /// <summary>
    /// Visibility mask to use when adding entities. Defaults to the usual visibility mask for that client.
    /// </summary>
    /// <remarks>
    /// Note that this mask will affect all global & session overrides from <see cref="PvsOverrideSystem"/> for this
    /// client, not just the entities in <see cref="Entities"/> and <see cref="RecursiveEntities"/>.
    /// </remarks>
    public int VisMask = mask;
}
