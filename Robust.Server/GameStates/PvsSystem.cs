using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Prometheus;
using Robust.Server.Configuration;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem : EntitySystem
{
    [Shared.IoC.Dependency] private readonly IConfigurationManager _configManager = default!;
    [Shared.IoC.Dependency] private readonly INetworkedMapManager _mapManager = default!;
    [Shared.IoC.Dependency] private readonly IServerEntityNetworkManager _netEntMan = default!;
    [Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
    [Shared.IoC.Dependency] private readonly IParallelManager _parallelManager = default!;
    [Shared.IoC.Dependency] private readonly IServerGameStateManager _serverGameStateManager = default!;
    [Shared.IoC.Dependency] private readonly IServerNetConfigurationManager _netConfigManager = default!;
    [Shared.IoC.Dependency] private readonly SharedTransformSystem _transform = default!;
    [Shared.IoC.Dependency] private readonly InputSystem _input = default!;
    [Shared.IoC.Dependency] private readonly IServerNetManager _netMan = default!;
    [Shared.IoC.Dependency] private readonly IParallelManagerInternal _parallelMgr = default!;
    [Shared.IoC.Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

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
    /// Size of the side of the view bounds square.
    /// </summary>
    private float _viewSize;

    // see CVars.NetLowLodDistance
    private float _lowLodDistance;

    /// <summary>
    /// Per-tick ack data to avoid re-allocating.
    /// </summary>
    private readonly List<ICommonSession> _toAck = new();
    internal readonly HashSet<ICommonSession> PendingAcks = new();
    private PvsAckJob _ackJob;
    private PvsChunkJob _chunkJob;

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    /// List of recently deleted entities.
    /// </summary>
    private readonly List<NetEntity> _deletedEntities = new();

    /// <summary>
    /// The tick at which each entity was deleted.
    /// </summary>
    private readonly List<GameTick> _deletedTick = new();

    private bool _async;

    public override void Initialize()
    {
        base.Initialize();

        _chunkJob = new PvsChunkJob { Pvs = this };
        _ackJob = new PvsAckJob { System = this, Sessions = _toAck,};

        _eyeQuery = GetEntityQuery<EyeComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<MapChangedEvent>(OnMapChanged);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<TransformComponent, TransformStartupEvent>(OnTransformStartup);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _transform.OnGlobalMoveEvent += OnEntityMove;

        _configManager.OnValueChanged(CVars.NetPVS, SetPvs, true);
        _configManager.OnValueChanged(CVars.NetMaxUpdateRange, OnViewsizeChanged, true);
        _configManager.OnValueChanged(CVars.NetLowLodRange, OnLodChanged, true);
        _configManager.OnValueChanged(CVars.NetForceAckThreshold, OnForceAckChanged, true);
        _configManager.OnValueChanged(CVars.NetPvsAsync, OnAsyncChanged, true);

        _serverGameStateManager.ClientAck += OnClientAck;
        _serverGameStateManager.ClientRequestFull += OnClientRequestFull;

        InitializeDirty();
    }

    private void OnAsyncChanged(bool value)
    {
        _async = value;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        _transform.OnGlobalMoveEvent -= OnEntityMove;

        _configManager.UnsubValueChanged(CVars.NetPVS, SetPvs);
        _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnViewsizeChanged);
        _configManager.UnsubValueChanged(CVars.NetForceAckThreshold, OnForceAckChanged);

        _serverGameStateManager.ClientAck -= OnClientAck;
        _serverGameStateManager.ClientRequestFull -= OnClientRequestFull;

        ShutdownDirty();
    }

    // TODO PVS rate limit this?
    private void OnClientRequestFull(ICommonSession session, GameTick tick, NetEntity? missingEntity)
    {
        if (!PlayerData.TryGetValue(session, out var sessionData))
            return;

        // Update acked tick so that OnClientAck doesn't get invoked by any late acks.
        var lastAcked = sessionData.LastReceivedAck;
        sessionData.LastReceivedAck = _gameTiming.CurTick;

        var sb = new StringBuilder();
        sb.Append($"Client {session} requested full state on tick {tick}. Last Acked: {lastAcked}. Curtick: {_gameTiming.CurTick}.");

        if (missingEntity != null)
        {
            var entity = GetEntity(missingEntity)!;
            sb.Append($" Apparently they received an entity without metadata: {ToPrettyString(entity.Value)}.");

            if (sessionData.Entities.TryGetValue(missingEntity.Value, out var data))
                sb.Append($" Entity last seen: {data.EntityLastAcked}");
        }

        Log.Warning(sb.ToString());

        if (sessionData.Overflow != null)
            _entDataListPool.Return(sessionData.Overflow.Value.SentEnts);
        sessionData.Overflow = null;

        foreach (var visSet in sessionData.PreviouslySent.Values)
        {
            _entDataListPool.Return(visSet);
        }
        sessionData.PreviouslySent.Clear();

        sessionData.RequestedFull = true;
        sessionData.Entities.Clear();
    }

    private void OnViewsizeChanged(float value)
    {
        _viewSize = value;
    }

    private void OnLodChanged(float value)
    {
        _lowLodDistance = Math.Clamp(value, ChunkSize, 100f);
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

    public void CullDeletionHistory(GameTick oldestAck, Histogram? histogram)
    {
        using var _ = histogram?.WithLabels("Cull History").NewTimer();
        CullDeletionHistoryUntil(oldestAck);
        _mapManager.CullDeletionHistory(oldestAck);
    }

    #region PVSCollection Event Updates

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            if (!PlayerData.TryAdd(e.Session, new(e.Session)))
                Log.Error($"Attempted to add player to _playerVisibleSets, but they were already present? Session:{e.Session}");

            return;
        }

        if (e.NewStatus != SessionStatus.Disconnected)
            return;

        if (!PlayerData.Remove(e.Session, out var data))
            return;

        if (data.Overflow != null)
            _entDataListPool.Return(data.Overflow.Value.SentEnts);
        data.Overflow = null;

        foreach (var visSet in data.PreviouslySent.Values)
        {
            _entDataListPool.Return(visSet);
        }
    }

    #endregion

    internal void GetEntityStates(PvsSession session)
    {
        // First, we send the client's own viewers. we want to ALWAYS send these, regardless of any pvs budget.
        AddForcedEntities(session);

        // After processing the entity's viewers, we set actual, budget limits.
        session.Budget.NewLimit= _netConfigManager.GetClientCVar(session.Channel, CVars.NetPVSEntityBudget);
        session.Budget.EnterLimit = _netConfigManager.GetClientCVar(session.Channel, CVars.NetPVSEntityEnterBudget);

        // Process all entities in visible PVS chunks
        AddPvsChunks(session);

        // Process all PVS overrides.
        AddAllOverrides(session);

        VerifySessionData(session);

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

    [Conditional("DEBUG")]
    private void VerifySessionData(PvsSession pvsSession)
    {
        var toSend = pvsSession.ToSend;
        var toSendSet = new HashSet<EntityUid>(toSend!.Count);
        foreach (var data in toSend)
        {
            toSendSet.Add(data.Entity.Owner);
        }
        DebugTools.AssertEqual(toSend.Count, toSendSet.Count);

        foreach (var data in CollectionsMarshal.AsSpan(toSend))
        {
            DebugTools.Assert(data.Visibility > PvsEntityVisibility.Unsent);
            DebugTools.AssertEqual(data.LastSeen, _gameTiming.CurTick);
            DebugTools.Assert(ReferenceEquals(data, pvsSession.Entities[data.NetEntity]));

            // if an entity is visible, its parents should always be visible.
            if (_xformQuery.GetComponent(data.Entity).ParentUid is not {Valid: true} pUid)
                continue;

            DebugTools.Assert(toSendSet.Contains(pUid),
                $"Attempted to send an entity without sending it's parents. Entity: {ToPrettyString(pUid)}.");
        }

        pvsSession.PreviouslySent.TryGetValue(_gameTiming.CurTick - 1, out var lastSent);
        foreach (var data in CollectionsMarshal.AsSpan(lastSent))
        {
            DebugTools.Assert(data.Visibility > PvsEntityVisibility.Unsent);
            DebugTools.Assert(!pvsSession.Entities.TryGetValue(data.NetEntity, out var old) || ReferenceEquals(data, old));
            DebugTools.Assert(data.LastSeen != GameTick.Zero);
            DebugTools.AssertEqual(toSendSet.Contains(data.Entity), data.LastSeen == _gameTiming.CurTick);
            DebugTools.Assert(data.LastSeen == _gameTiming.CurTick
                              || data.LastSeen == _gameTiming.CurTick - 1);
        }
    }

    /// <summary>
    /// Figure out what entities are no longer visible to the client. These entities are sent reliably to the client
    /// in a separate net message. This has to be called after EntityData.LastSent is updated.
    /// </summary>
    internal void ProcessLeavePvs(PvsSession session)
    {
        if (!CullingEnabled || session.DisableCulling)
            return;

        if (session.LastSent == null)
            return;

        var (toTick, lastSent) = session.LastSent.Value;
        foreach (var data in CollectionsMarshal.AsSpan(lastSent))
        {
            if (data.LastSeen == toTick)
                continue;

            session.LeftView.Add(data.NetEntity);
            data.LastLeftView = toTick;

            // TODO PVS make this not required. I.e., hide maps/grids from clients.
            DebugTools.Assert(!HasComp<MapGridComponent>(data.Entity));
            DebugTools.Assert(!HasComp<MapComponent>(data.Entity));
        }

        if (session.LeftView.Count == 0)
            return;

        var pvsMessage = new MsgStateLeavePvs {Entities = session.LeftView, Tick = toTick};
        _netMan.ServerSendMessage(pvsMessage, session.Channel);
        session.LeftView.Clear();
    }

    private (Vector2 worldPos, float range, EntityUid? map) CalcViewBounds(Entity<TransformComponent, EyeComponent?> eye)
    {
        var size = Math.Max(eye.Comp2?.PvsSize ?? _viewSize, 1);
        return (_transform.GetWorldPosition(eye.Comp1), size / 2f, eye.Comp1.MapUid);
    }

    public void CullDeletionHistoryUntil(GameTick tick)
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

    public void BeforeSendState(ICommonSession[] players, Histogram histogram)
    {
        var ackJob = ProcessQueuedAcks(histogram);

        // Figure out what chunks players can see and cache some chunk data.
        if (CullingEnabled)
        {
            GetVisibleChunks(players, histogram);
            ProcessVisibleChunks(histogram);
        }

        ackJob?.WaitOne();
    }

    public void AfterSendState(ICommonSession[] players, Histogram histogram, GameTick oldestAck,
        ref GameTick lastOldestAck)
    {
        CleanupDirty(players, histogram);

        if (oldestAck == GameTick.MaxValue)
        {
            // There were no connected players?
            // In that case we just clear all deletion history.
            CullDeletionHistory(GameTick.MaxValue, histogram);
            lastOldestAck = GameTick.Zero;
            return;
        }

        if (oldestAck == lastOldestAck)
            return;

        lastOldestAck = oldestAck;
        CullDeletionHistory(oldestAck, histogram);
    }
}

[ByRefEvent]
public struct ExpandPvsEvent(ICommonSession session)
{
    public readonly ICommonSession Session = session;

    /// <summary>
    /// List of entities that will get added to this session's PVS set.
    /// </summary>
    public List<EntityUid>? Entities;

    /// <summary>
    /// List of entities that will get added to this session's PVS set. Unlike <see cref="Entities"/> this will also
    /// recursively add all children of the given entity.
    /// </summary>
    public List<EntityUid>? RecursiveEntities;
}
