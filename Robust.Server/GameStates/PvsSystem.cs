using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Configuration;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem : EntitySystem
{
    [Shared.IoC.Dependency] private readonly IConfigurationManager _configManager = default!;
    [Shared.IoC.Dependency] private readonly INetworkedMapManager _mapManager = default!;
    [Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
    [Shared.IoC.Dependency] private readonly IParallelManager _parallelManager = default!;
    [Shared.IoC.Dependency] private readonly IServerGameStateManager _serverGameStateManager = default!;
    [Shared.IoC.Dependency] private readonly IServerNetConfigurationManager _netConfigManager = default!;
    [Shared.IoC.Dependency] private readonly SharedTransformSystem _transform = default!;

    public const float ChunkSize = 8;

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
    /// Maximum number of pooled objects
    /// </summary>
    private const int MaxVisPoolSize = 1024;

    /// <summary>
    /// Is view culling enabled, or will we send the whole map?
    /// </summary>
    public bool CullingEnabled { get; private set; }

    /// <summary>
    /// Size of the side of the view bounds square.
    /// </summary>
    private float _viewSize;

    /// <summary>
    /// If PVS disabled then we'll track if we've dumped all entities on the player.
    /// This way any future ticks can be orders of magnitude faster as we only send what changes.
    /// </summary>
    private HashSet<ICommonSession> _seenAllEnts = new();

    internal readonly Dictionary<ICommonSession, SessionPVSData> PlayerData = new();

    private PVSCollection<NetEntity> _entityPvsCollection = default!;
    public PVSCollection<NetEntity> EntityPVSCollection => _entityPvsCollection;

    private readonly List<IPVSCollection> _pvsCollections = new();

    private readonly ObjectPool<Dictionary<NetEntity, PvsEntityVisibility>> _visSetPool
        = new DefaultObjectPool<Dictionary<NetEntity, PvsEntityVisibility>>(
            new DictPolicy<NetEntity, PvsEntityVisibility>(), MaxVisPoolSize);

    private readonly ObjectPool<HashSet<EntityUid>> _uidSetPool
        = new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>(), MaxVisPoolSize);

    private readonly ObjectPool<Stack<NetEntity>> _stackPool
        = new DefaultObjectPool<Stack<NetEntity>>(
            new StackPolicy<NetEntity>(), MaxVisPoolSize);

    private readonly ObjectPool<Dictionary<NetEntity, MetaDataComponent>> _chunkCachePool =
        new DefaultObjectPool<Dictionary<NetEntity, MetaDataComponent>>(
            new DictPolicy<NetEntity, MetaDataComponent>(), MaxVisPoolSize);

    private readonly ObjectPool<HashSet<int>> _playerChunkPool =
        new DefaultObjectPool<HashSet<int>>(new SetPolicy<int>(), MaxVisPoolSize);

    private readonly ObjectPool<RobustTree<NetEntity>> _treePool =
        new DefaultObjectPool<RobustTree<NetEntity>>(new TreePolicy<NetEntity>(), MaxVisPoolSize);

    private readonly ObjectPool<Dictionary<MapChunkLocation, int>> _mapChunkPool =
        new DefaultObjectPool<Dictionary<MapChunkLocation, int>>(
            new ChunkPoolPolicy<MapChunkLocation>(), MaxVisPoolSize);

    private readonly ObjectPool<Dictionary<GridChunkLocation, int>> _gridChunkPool =
        new DefaultObjectPool<Dictionary<GridChunkLocation, int>>(
            new ChunkPoolPolicy<GridChunkLocation>(), MaxVisPoolSize);

    private readonly Dictionary<int, Dictionary<MapChunkLocation, int>> _mapIndices = new(4);
    private readonly Dictionary<int, Dictionary<GridChunkLocation, int>> _gridIndices = new(4);
    private readonly List<(int, IChunkIndexLocation)> _chunkList = new(64);
    internal readonly HashSet<ICommonSession> PendingAcks = new();

    private readonly Dictionary<(int visMask, IChunkIndexLocation location), (Dictionary<NetEntity, MetaDataComponent> metadata,
        RobustTree<NetEntity> tree)?> _previousTrees = new();

    private readonly HashSet<(int visMask, IChunkIndexLocation location)> _reusedTrees = new();

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _eyeQuery = GetEntityQuery<EyeComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();

        _entityPvsCollection = RegisterPVSCollection<NetEntity>();

        SubscribeLocalEvent<MapChangedEvent>(ev =>
        {
            if (ev.Created)
                OnMapCreated(ev);
            else
                OnMapDestroyed(ev);
        });

        SubscribeLocalEvent<GridInitializeEvent>(OnGridCreated);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<MoveEvent>(OnEntityMove);
        SubscribeLocalEvent<TransformComponent, TransformStartupEvent>(OnTransformStartup);
        EntityManager.EntityDeleted += OnEntityDeleted;

        _configManager.OnValueChanged(CVars.NetPVS, SetPvs, true);
        _configManager.OnValueChanged(CVars.NetMaxUpdateRange, OnViewsizeChanged, true);
        _configManager.OnValueChanged(CVars.NetForceAckThreshold, OnForceAckChanged, true);

        _serverGameStateManager.ClientAck += OnClientAck;
        _serverGameStateManager.ClientRequestFull += OnClientRequestFull;

        InitializeDirty();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        UnregisterPVSCollection(_entityPvsCollection);
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        EntityManager.EntityDeleted -= OnEntityDeleted;

        _configManager.UnsubValueChanged(CVars.NetPVS, SetPvs);
        _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnViewsizeChanged);
        _configManager.UnsubValueChanged(CVars.NetForceAckThreshold, OnForceAckChanged);

        _serverGameStateManager.ClientAck -= OnClientAck;
        _serverGameStateManager.ClientRequestFull -= OnClientRequestFull;

        ShutdownDirty();
    }

    // TODO rate limit this?
    private void OnClientRequestFull(ICommonSession session, GameTick tick, NetEntity? missingEntity)
    {
        if (!PlayerData.TryGetValue(session, out var sessionData))
            return;

        // Update acked tick so that OnClientAck doesn't get invoked by any late acks.
        var lastAcked = sessionData.LastReceivedAck;
        sessionData.LastReceivedAck = _gameTiming.CurTick;

        var sb = new StringBuilder();
        sb.Append($"Client {session} requested full state on tick {tick}. Last Acked: {lastAcked}.");

        if (missingEntity != null)
        {
            var entity = GetEntity(missingEntity)!;
            sb.Append($" Apparently they received an entity without metadata: {ToPrettyString(entity.Value)}.");

            if (sessionData.LastSeenAt.TryGetValue(missingEntity.Value, out var lastSeenTick))
                sb.Append($" Entity last sent: {lastSeenTick.Value}");
        }

        Log.Warning(sb.ToString());

        sessionData.LastSeenAt.Clear();
        sessionData.LastLeftView.Clear();

        if (sessionData.Overflow != null)
        {
            _visSetPool.Return(sessionData.Overflow.Value.SentEnts);
            sessionData.Overflow = null;
        }

        // return last acked to pool, but only if it is not still in the OverflowDictionary.
        if (sessionData.LastAcked != null && !sessionData.SentEntities.ContainsKey(sessionData.LastAcked.Value.Tick))
        {
            DebugTools.Assert(!sessionData.SentEntities.Values.Contains(sessionData.LastAcked.Value.Data));
            _visSetPool.Return(sessionData.LastAcked.Value.Data);
        }

        sessionData.LastAcked = null;
        sessionData.RequestedFull = true;
    }

    private void OnViewsizeChanged(float obj)
    {
        _viewSize = obj * 2;
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

    public void ProcessCollections()
    {
        foreach (var collection in _pvsCollections)
        {
            collection.Process();
        }
    }

    public void CullDeletionHistory(GameTick oldestAck)
    {
        _entityPvsCollection.CullDeletionHistoryUntil(oldestAck);
        _mapManager.CullDeletionHistory(oldestAck);
    }

    #region PVSCollection methods to maybe make public someday:tm:

    private PVSCollection<TIndex> RegisterPVSCollection<TIndex>() where TIndex : IComparable<TIndex>, IEquatable<TIndex>
    {
        var collection = new PVSCollection<TIndex>(Log, EntityManager, _transform);
        _pvsCollections.Add(collection);
        return collection;
    }

    private bool UnregisterPVSCollection<TIndex>(PVSCollection<TIndex> pvsCollection) where TIndex : IComparable<TIndex>, IEquatable<TIndex> =>
        _pvsCollections.Remove(pvsCollection);

    #endregion

    #region PVSCollection Event Updates

    private void OnEntityDeleted(EntityUid e, MetaDataComponent metadata)
    {
        _entityPvsCollection.RemoveIndex(EntityManager.CurrentTick, metadata.NetEntity);

        var previousTick = _gameTiming.CurTick - 1;

        foreach (var sessionData in PlayerData.Values)
        {
            sessionData.LastSeenAt.Remove(metadata.NetEntity);
            sessionData.LastLeftView.Remove(metadata.NetEntity);
            if (sessionData.SentEntities.TryGetValue(previousTick, out var ents))
                ents.Remove(metadata.NetEntity);
        }
    }

    private void OnEntityMove(ref MoveEvent ev)
    {
        // GriddUid is only set after init.
        if (!ev.Component._gridInitialized)
            _transform.InitializeGridUid(ev.Sender, ev.Component);

        // since elements are cached grid-/map-relative, we dont need to update a given grids/maps children
        if (ev.Component.GridUid == ev.Sender)
            return;
        DebugTools.Assert(!_mapManager.IsGrid(ev.Sender));

        if (!ev.Component.ParentUid.IsValid())
        {
            // This entity is either a map, terminating, or a rare null-space entity.
            if (Terminating(ev.Sender))
                return;

            if (ev.Component.MapUid == ev.Sender)
                return;
        }

        DebugTools.Assert(!_mapManager.IsMap(ev.Sender));

        var coordinates = _transform.GetMoverCoordinates(ev.Sender, ev.Component);
        UpdateEntityRecursive(ev.Sender, _metaQuery.GetComponent(ev.Sender), ev.Component, coordinates, false, ev.ParentChanged);
    }

    private void OnTransformStartup(EntityUid uid, TransformComponent component, ref TransformStartupEvent args)
    {
        // use Startup because GridId is not set during the eventbus init yet!

        // since elements are cached grid-/map-relative, we dont need to update a given grids/maps children
        if (component.GridUid == uid)
            return;
        DebugTools.Assert(!_mapManager.IsGrid(uid));

        if (component.MapUid == uid)
            return;
        DebugTools.Assert(!_mapManager.IsMap(uid));

        var coordinates = _transform.GetMoverCoordinates(uid, component);
        UpdateEntityRecursive(uid, _metaQuery.GetComponent(uid), component, coordinates, false, false);
    }

    private void UpdateEntityRecursive(EntityUid uid, MetaDataComponent metadata, TransformComponent xform, EntityCoordinates coordinates, bool mover, bool forceDirty)
    {
        if (mover && !xform.LocalPosition.Equals(Vector2.Zero))
        {
            coordinates = _transform.GetMoverCoordinates(uid, xform);
        }

        // since elements are cached grid-/map-relative, we don't need to update a given grids/maps children
        DebugTools.Assert(!_mapManager.IsGrid(uid) && !_mapManager.IsMap(uid));

        var indices = PVSCollection<NetEntity>.GetChunkIndices(coordinates.Position);
        if (xform.GridUid != null)
            _entityPvsCollection.UpdateIndex(metadata.NetEntity, xform.GridUid.Value, indices, forceDirty: forceDirty);
        else
            _entityPvsCollection.UpdateIndex(metadata.NetEntity, xform.MapID, indices, forceDirty: forceDirty);

        var children = xform.ChildEnumerator;

        // TODO PERFORMANCE
        // Given uid is the parent of its children, we already know that the child xforms will have to be relative to
        // coordinates.EntityId. So instead of calling GetMoverCoordinates() for each child we should just calculate it
        // directly.
        while (children.MoveNext(out var child))
        {
            UpdateEntityRecursive(child.Value, _metaQuery.GetComponent(child.Value), _xformQuery.GetComponent(child.Value), coordinates, true, forceDirty);
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            if (!PlayerData.TryAdd(e.Session, new()))
                Log.Error($"Attempted to add player to _playerVisibleSets, but they were already present? Session:{e.Session}");

            foreach (var pvsCollection in _pvsCollections)
            {
                if (!pvsCollection.AddPlayer(e.Session))
                    Log.Error($"Attempted to add player to pvsCollection, but they were already present? Session:{e.Session}");
            }
            return;
        }

        if (e.NewStatus != SessionStatus.Disconnected)
            return;

        if (!PlayerData.Remove(e.Session, out var data))
            return;

        foreach (var pvsCollection in _pvsCollections)
        {
            if (!pvsCollection.RemovePlayer(e.Session))
                Log.Error($"Attempted to remove player from pvsCollection, but they were already removed? Session:{e.Session}");
        }

        if (data.Overflow != null)
            _visSetPool.Return(data.Overflow.Value.SentEnts);
        data.Overflow = null;

        var acked = data.LastAcked?.Data;
        if (acked != null)
            _visSetPool.Return(acked);

        foreach (var visSet in data.SentEntities.Values)
        {
            if (visSet != acked)
                _visSetPool.Return(visSet);
        }

        data.LastAcked = null;
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.RemoveGrid(ev.EntityUid);
        }
    }

    private void OnGridCreated(GridInitializeEvent ev)
    {
        var gridId = ev.EntityUid;
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.AddGrid(gridId);
        }

        _entityPvsCollection.AddGlobalOverride(_metaQuery.GetComponent(gridId).NetEntity, true, false);
    }

    private void OnMapDestroyed(MapChangedEvent e)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.RemoveMap(e.Map);
        }
    }

    private void OnMapCreated(MapChangedEvent e)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.AddMap(e.Map);
        }

        if(e.Map == MapId.Nullspace) return;
        var uid = _mapManager.GetMapEntityId(e.Map);
        _entityPvsCollection.AddGlobalOverride(_metaQuery.GetComponent(uid).NetEntity, true, false);
    }

    #endregion

    public (List<(int, IChunkIndexLocation)> , HashSet<int>[], EntityUid[][] viewers) GetChunks(ICommonSession[] sessions)
    {
        var playerChunks = new HashSet<int>[sessions.Length];
        var viewerEntities = new EntityUid[sessions.Length][];

        _chunkList.Clear();
        // Keep track of the index of each chunk we use for a faster index lookup.
        // Pool it because this will allocate a lot across ticks as we scale in players.
        foreach (var chunks in _mapIndices.Values)
        {
            _mapChunkPool.Return(chunks);
        }

        foreach (var chunks in _gridIndices.Values)
        {
            _gridChunkPool.Return(chunks);
        }

        _mapIndices.Clear();
        _gridIndices.Clear();

        for (int i = 0; i < sessions.Length; i++)
        {
            var session = sessions[i];
            playerChunks[i] = _playerChunkPool.Get();

            var viewers = GetSessionViewers(session);
            viewerEntities[i] = viewers;

            for (var j = 0; j < viewers.Length; j++)
            {
                var eyeEuid = viewers[j];
                var (viewPos, range, mapId) = CalcViewBounds(in eyeEuid);

                if (mapId == MapId.Nullspace) continue;

                int visMask = EyeComponent.DefaultVisibilityMask;
                if (_eyeQuery.TryGetComponent(eyeEuid, out var eyeComp))
                    visMask = eyeComp.VisibilityMask;

                // Get the nyoom dictionary for index lookups.
                if (!_mapIndices.TryGetValue(visMask, out var mapDict))
                {
                    mapDict = _mapChunkPool.Get();
                    _mapIndices[visMask] = mapDict;
                }

                var mapChunkEnumerator = new ChunkIndicesEnumerator(viewPos, range, ChunkSize);

                while (mapChunkEnumerator.MoveNext(out var chunkIndices))
                {
                    var chunkLocation = new MapChunkLocation(mapId, chunkIndices.Value);
                    var entry = (visMask, chunkLocation);

                    if (mapDict.TryGetValue(chunkLocation, out var indexOf))
                    {
                        playerChunks[i].Add(indexOf);
                    }
                    else
                    {
                        playerChunks[i].Add(_chunkList.Count);
                        mapDict.Add(chunkLocation, _chunkList.Count);
                        _chunkList.Add(entry);
                    }
                }

                // Get the nyoom dictionary for index lookups.
                if (!_gridIndices.TryGetValue(visMask, out var gridDict))
                {
                    gridDict = _gridChunkPool.Get();
                    _gridIndices[visMask] = gridDict;
                }

                var state = (i, _xformQuery, viewPos, range, visMask, gridDict, playerChunks, _chunkList, _transform);
                var rangeVec = new Vector2(range, range);

                _mapManager.FindGridsIntersecting(mapId, new Box2(viewPos - rangeVec, viewPos + rangeVec),
                    ref state, static (
                        EntityUid gridUid,
                        MapGridComponent _,
                        ref (int i,
                            EntityQuery<TransformComponent> transformQuery,
                            Vector2 viewPos,
                            float range,
                            int visMask,
                            Dictionary<GridChunkLocation, int> gridDict,
                            HashSet<int>[] playerChunks,
                            List<(int, IChunkIndexLocation)> _chunkList,
                            SharedTransformSystem xformSystem) tuple) =>
                    {
                        {
                            var localPos = tuple.xformSystem.GetInvWorldMatrix(gridUid, tuple.transformQuery).Transform(tuple.viewPos);

                            var gridChunkEnumerator =
                                new ChunkIndicesEnumerator(localPos, tuple.range, ChunkSize);

                            while (gridChunkEnumerator.MoveNext(out var gridChunkIndices))
                            {
                                var chunkLocation = new GridChunkLocation(gridUid, gridChunkIndices.Value);
                                var entry = (tuple.visMask, chunkLocation);

                                if (tuple.gridDict.TryGetValue(chunkLocation, out var indexOf))
                                {
                                    tuple.playerChunks[tuple.i].Add(indexOf);
                                }
                                else
                                {
                                    tuple.playerChunks[tuple.i].Add(tuple._chunkList.Count);
                                    tuple.gridDict.Add(chunkLocation, tuple._chunkList.Count);
                                    tuple._chunkList.Add(entry);
                                }
                            }

                            return true;
                        }
                    });
            }
        }

        return (_chunkList, playerChunks, viewerEntities);
    }

    public void RegisterNewPreviousChunkTrees(
        List<(int, IChunkIndexLocation)> chunks,
        (Dictionary<NetEntity, MetaDataComponent> metadata, RobustTree<NetEntity> tree)?[] trees,
        bool[] reuse)
    {
        // For any chunks able to re-used we'll chuck them in a dictionary for faster lookup.
        for (var i = 0; i < chunks.Count; i++)
        {
            var canReuse = reuse[i];
            if (!canReuse) continue;

            _reusedTrees.Add(chunks[i]);
        }

        var previousIndices = _previousTrees.Keys.ToArray();
        for (var i = 0; i < previousIndices.Length; i++)
        {
            var index = previousIndices[i];
            // ReSharper disable once InconsistentlySynchronizedField
            if (_reusedTrees.Contains(index)) continue;
            var chunk = _previousTrees[index];
            if (chunk.HasValue)
            {
                _chunkCachePool.Return(chunk.Value.metadata);
                _treePool.Return(chunk.Value.tree);
            }

            if (!chunks.Contains(index))
            {
                _previousTrees.Remove(index);
            }
        }

        _previousTrees.EnsureCapacity(chunks.Count);
        for (int i = 0; i < chunks.Count; i++)
        {
            //this is a redundant assign if the tree has been reused. the assumption is that this is cheaper than a .Contains call
            _previousTrees[chunks[i]] = trees[i];
        }
        // ReSharper disable once InconsistentlySynchronizedField
        _reusedTrees.Clear();
    }

    public bool TryCalculateChunk(
        IChunkIndexLocation chunkLocation,
        int visMask,
        out (Dictionary<NetEntity, MetaDataComponent> mData, RobustTree<NetEntity> tree)? result)
    {
        if (!_entityPvsCollection.IsDirty(chunkLocation) && _previousTrees.TryGetValue((visMask, chunkLocation), out var previousTree))
        {
            result = previousTree;
            return true;
        }

        var chunk = chunkLocation switch
        {
            GridChunkLocation gridChunkLocation => _entityPvsCollection.TryGetChunk(gridChunkLocation.GridId,
                gridChunkLocation.ChunkIndices, out var gridChunk)
                ? gridChunk
                : null,
            MapChunkLocation mapChunkLocation => _entityPvsCollection.TryGetChunk(mapChunkLocation.MapId,
                mapChunkLocation.ChunkIndices, out var mapChunk)
                ? mapChunk
                : null,
            _ => null
        };
        if (chunk == null)
        {
            result = null;
            return false;
        }
        var chunkSet = _chunkCachePool.Get();
        var tree = _treePool.Get();
        foreach (var netEntity in chunk)
        {
            var (uid, meta) = GetEntityData(netEntity);
            AddToChunkSetRecursively(in uid, in netEntity, meta, visMask, tree, chunkSet);
#if DEBUG
            var xform = _xformQuery.GetComponent(uid);
            if (chunkLocation is MapChunkLocation)
                DebugTools.Assert(xform.GridUid == null || xform.GridUid == uid);
            else if (chunkLocation is GridChunkLocation)
                DebugTools.Assert(xform.ParentUid != xform.MapUid || xform.GridUid == xform.MapUid);
#endif
        }

        if (tree.RootNodes.Count == 0)
        {
            // This can happen if the only entity in a chunk is invisible
            // (e.g., when a ghost moves from from a grid into empty space).
            DebugTools.Assert(chunkSet.Count == 0);
            _treePool.Return(tree);
            _chunkCachePool.Return(chunkSet);
            result = null;
            return true;
        }
        DebugTools.Assert(chunkSet.Count > 0);

        result = (chunkSet, tree);
        return false;
    }

    public void ReturnToPool(HashSet<int>[] playerChunks)
    {
        for (var i = 0; i < playerChunks.Length; i++)
        {
            _playerChunkPool.Return(playerChunks[i]);
        }
    }

    private void AddToChunkSetRecursively(in EntityUid uid, in NetEntity netEntity, MetaDataComponent mComp,
        int visMask, RobustTree<NetEntity> tree, Dictionary<NetEntity, MetaDataComponent> set)
    {
        // If the eye is missing ANY layer that this entity is on, or any layer that any of its parents belongs to, then
        // it is considered invisible.
        if ((visMask & mComp.VisibilityMask) != mComp.VisibilityMask)
            return;

        if (!set.TryAdd(netEntity, mComp))
            return; // already sending

        var xform = _xformQuery.GetComponent(uid);

        // is this a map or grid?
        var isRoot = !xform.ParentUid.IsValid() || uid == xform.GridUid;
        if (isRoot)
        {
            DebugTools.Assert(_mapManager.IsGrid(uid) || _mapManager.IsMap(uid));
            tree.Set(netEntity);
            return;
        }

        DebugTools.Assert(!_mapManager.IsGrid(uid) && !_mapManager.IsMap(uid));

        var parent = xform.ParentUid;
        var parentMeta = _metaQuery.GetComponent(parent);
        var parentNetEntity = parentMeta.NetEntity;

        // Child should have all o the same flags as the parent.
        DebugTools.Assert((parentMeta.VisibilityMask & mComp.VisibilityMask) == parentMeta.VisibilityMask);

        // Add our parent.
        AddToChunkSetRecursively(in parent, in parentNetEntity, parentMeta, visMask, tree, set);
        tree.Set(netEntity, parentNetEntity);
    }

    internal (List<EntityState>? updates, List<NetEntity>? deletions, List<NetEntity>? leftPvs, GameTick fromTick)
        CalculateEntityStates(ICommonSession session,
            GameTick fromTick,
            GameTick toTick,
            (Dictionary<NetEntity, MetaDataComponent> metadata, RobustTree<NetEntity> tree)?[] chunks,
            HashSet<int> visibleChunks,
            EntityUid[] viewers)
    {
        DebugTools.Assert(session.Status == SessionStatus.InGame);
        var newEntityBudget = _netConfigManager.GetClientCVar(session.Channel, CVars.NetPVSEntityBudget);
        var enteredEntityBudget = _netConfigManager.GetClientCVar(session.Channel, CVars.NetPVSEntityEnterBudget);
        var newEntityCount = 0;
        var enteredEntityCount = 0;
        var sessionData = PlayerData[session];
        sessionData.SentEntities.TryGetValue(toTick - 1, out var lastSent);
        var lastAcked = sessionData.LastAcked?.Data;
        var lastSeen = sessionData.LastSeenAt;
        var lastLeft = sessionData.LastLeftView;
        var visibleEnts = _visSetPool.Get();

        if (visibleEnts.Count != 0)
            throw new Exception("Encountered non-empty object inside of _visSetPool. Was the same object returned to the pool more than once?");

        var deletions = _entityPvsCollection.GetDeletedIndices(fromTick);
        var entStateCount = 0;

        var stack = _stackPool.Get();
        // TODO reorder chunks to prioritize those that are closest to the viewer? Helps make pop-in less visible.
        foreach (var i in visibleChunks)
        {
            var cache = chunks[i];
            if(!cache.HasValue) continue;

#if DEBUG
            // Each root nodes should simply be a map or a grid entity.
            DebugTools.Assert(cache.Value.tree.RootNodes.Count == 1,
                $"Root node count is {cache.Value.tree.RootNodes.Count} instead of 1. Session: {session}");
            var nent = cache.Value.tree.RootNodes.FirstOrDefault();
            var ent = GetEntity(nent);
            DebugTools.Assert(Exists(ent), $"Root node does not exist. Node {ent}. Session: {session}");
            DebugTools.Assert(HasComp<MapComponent>(ent) || HasComp<MapGridComponent>(ent));
#endif

            foreach (var rootNode in cache.Value.tree.RootNodes)
            {
                RecursivelyAddTreeNode(in rootNode, cache.Value.tree, lastAcked, lastSent, visibleEnts, lastSeen, lastLeft, cache.Value.metadata, stack, in fromTick,
                        ref newEntityCount, ref enteredEntityCount, ref entStateCount,  in newEntityBudget, in enteredEntityBudget);
            }
        }
        _stackPool.Return(stack);

        var globalEnumerator = _entityPvsCollection.GlobalOverridesEnumerator;
        while (globalEnumerator.MoveNext())
        {
            var netEntity = globalEnumerator.Current;
            var uid = GetEntity(netEntity);
            RecursivelyAddOverride(in uid, lastAcked, lastSent, visibleEnts, lastSeen, lastLeft, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
        }
        globalEnumerator.Dispose();

        var globalRecursiveEnumerator = _entityPvsCollection.GlobalRecursiveOverridesEnumerator;
        while (globalRecursiveEnumerator.MoveNext())
        {
            var netEntity = globalRecursiveEnumerator.Current;
            var uid = GetEntity(netEntity);
            RecursivelyAddOverride(in uid, lastAcked, lastSent, visibleEnts, lastSeen, lastLeft, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget, true);
        }
        globalRecursiveEnumerator.Dispose();

        var sessionOverrides = _entityPvsCollection.GetSessionOverrides(session);
        while (sessionOverrides.MoveNext())
        {
            var netEntity = sessionOverrides.Current;
            var uid = GetEntity(netEntity);
            RecursivelyAddOverride(in uid, lastAcked, lastSent, visibleEnts, lastSeen, lastLeft, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget, true);
        }
        sessionOverrides.Dispose();

        foreach (var viewerEntity in viewers)
        {
            RecursivelyAddOverride(in viewerEntity, lastAcked, lastSent, visibleEnts, lastSeen, lastLeft, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
        }

        var expandEvent = new ExpandPvsEvent(session);
        RaiseLocalEvent(ref expandEvent);
        if (expandEvent.Entities != null)
        {
            foreach (var entityUid in expandEvent.Entities)
            {
                RecursivelyAddOverride(in entityUid, lastAcked, lastSent, visibleEnts, lastSeen, lastLeft, in fromTick,
                    ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
            }
        }

        if (expandEvent.RecursiveEntities != null)
        {
            foreach (var entityUid in expandEvent.RecursiveEntities)
            {
                RecursivelyAddOverride(in entityUid, lastAcked, lastSent, visibleEnts, lastSeen, lastLeft, in fromTick,
                    ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget, true);
            }
        }

        var entityStates = new List<EntityState>(entStateCount);

        foreach (var (netEntity, visiblity) in visibleEnts)
        {
            EntityUid uid;
            MetaDataComponent meta;
#if DEBUG
            uid = GetEntity(netEntity);
            // if an entity is visible, its parents should always be visible.
            DebugTools.Assert((_xformQuery.GetComponent(uid).ParentUid is not { Valid: true } parent) ||
                              visibleEnts.ContainsKey(_metaQuery.GetComponent(parent).NetEntity),
                $"Attempted to send an entity without sending it's parents. Entity: {ToPrettyString(uid)}.");
#endif

            if (sessionData.RequestedFull)
            {
                (uid, meta) = GetEntityData(netEntity);
                entityStates.Add(GetFullEntityState(session, uid, meta));
                continue;
            }

            if (visiblity == PvsEntityVisibility.StayedUnchanged)
                continue;

            (uid, meta) = GetEntityData(netEntity);
            var entered = visiblity == PvsEntityVisibility.Entered;
            var entFromTick = entered ? lastSeen.GetValueOrDefault(netEntity) : fromTick;
            var state = GetEntityState(session, uid, entFromTick, meta);

            if (entered || !state.Empty)
                entityStates.Add(state);
        }

        // tell a client to detach entities that have left their view
        var leftView = ProcessLeavePvs(visibleEnts, lastSent, lastLeft);

        if (sessionData.SentEntities.Add(toTick, visibleEnts, out var oldEntry))
        {
            if (oldEntry.Value.Key > fromTick && sessionData.Overflow == null)
            {
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
                sessionData.Overflow = oldEntry.Value;

#if DEBUG
                // This happens relatively frequently for the current TickBuffer value, and doesn't really provide any
                // useful info when not debugging/testing locally. Hence only enable on DEBUG.
                Log.Debug($"Client {session} exceeded tick buffer.");
#endif
            }
            else if (oldEntry.Value.Value != lastAcked)
                _visSetPool.Return(oldEntry.Value.Value);
        }

        if (entityStates.Count == 0) entityStates = default;
        return (entityStates, deletions, leftView, sessionData.RequestedFull ? GameTick.Zero : fromTick);
    }

    /// <summary>
    ///     Figure out what entities are no longer visible to the client. These entities are sent reliably to the client
    ///     in a separate net message.
    /// </summary>
    private List<NetEntity>? ProcessLeavePvs(
        Dictionary<NetEntity, PvsEntityVisibility> visibleEnts,
        Dictionary<NetEntity, PvsEntityVisibility>? lastSent,
        Dictionary<NetEntity, GameTick> lastLeft)
    {
        if (lastSent == null)
            return null;

        var tick = _gameTiming.CurTick;
        var minSize = Math.Max(0, lastSent.Count - visibleEnts.Count);
        var leftView = new List<NetEntity>(minSize);

        foreach (var netEntity in lastSent.Keys)
        {
            if (!visibleEnts.ContainsKey(netEntity))
            {
                leftView.Add(netEntity);
                lastLeft[netEntity] = tick;
            }

        }

        return leftView.Count > 0 ? leftView : null;
    }

    private void RecursivelyAddTreeNode(in NetEntity nodeIndex,
        RobustTree<NetEntity> tree,
        Dictionary<NetEntity, PvsEntityVisibility>? lastAcked,
        Dictionary<NetEntity, PvsEntityVisibility>? lastSent,
        Dictionary<NetEntity, PvsEntityVisibility> toSend,
        Dictionary<NetEntity, GameTick> lastSeen,
        Dictionary<NetEntity, GameTick> lastLeft,
        Dictionary<NetEntity, MetaDataComponent> metaDataCache,
        Stack<NetEntity> stack,
        in GameTick fromTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int entStateCount,
        in int newEntityBudget,
        in int enteredEntityBudget)
    {
        stack.Push(nodeIndex);

        while (stack.TryPop(out var currentNodeIndex))
        {
            DebugTools.Assert(currentNodeIndex.IsValid());

            // As every map is parented to uid 0 in the tree we still need to get their children, plus because we go top-down
            // we may find duplicate parents with children we haven't encountered before
            // on different chunks (this is especially common with direct grid children)

            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(toSend, currentNodeIndex, out var exists);
            if (!exists)
            {
                var (entered, shouldAdd) = ProcessEntry(in currentNodeIndex, lastAcked, lastSent, lastSeen, lastLeft, fromTick,
                    ref newEntityCount, ref enteredEntityCount, newEntityBudget, enteredEntityBudget);

                if (!shouldAdd)
                {
                    // In the majority of instances entities do get added.
                    // So its better to add and maybe remove, rather than checking ContainsKey() and then maybe adding it.
                    toSend.Remove(currentNodeIndex);
                    continue;
                }

                AddToSendSet(in currentNodeIndex, metaDataCache[currentNodeIndex], ref value, toSend, fromTick, in entered, ref entStateCount);
            }

            var node = tree[currentNodeIndex];
            if (node.Children == null)
                continue;

            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }
    }

    public bool RecursivelyAddOverride(in EntityUid uid,
        Dictionary<NetEntity, PvsEntityVisibility>? lastAcked,
        Dictionary<NetEntity, PvsEntityVisibility>? lastSent,
        Dictionary<NetEntity, PvsEntityVisibility> toSend,
        Dictionary<NetEntity, GameTick> lastSeen,
        Dictionary<NetEntity, GameTick> lastLeft,
        in GameTick fromTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int entStateCount,
        in int newEntityBudget,
        in int enteredEntityBudget,
        bool addChildren = false)
    {
        //are we valid?
        //sometimes uids gets added without being valid YET (looking at you mapmanager) (mapcreate & gridcreated fire before the uids becomes valid)
        if (!uid.IsValid())
            return false;

        var xform = _xformQuery.GetComponent(uid);
        var parent = xform.ParentUid;
        if (parent.IsValid() && !RecursivelyAddOverride(in parent, lastAcked, lastSent, toSend, lastSeen, lastLeft, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget,
                in enteredEntityBudget))
        {
            return false;
        }

        var metadata = _metaQuery.GetComponent(uid);
        var netEntity = metadata.NetEntity;

        // Note that we check this AFTER adding parents. This is because while this entity may already have been added
        // to the toSend set, it doesn't guarantee that its parents have been. E.g., if a player ghost just teleported
        // to follow a far away entity, the player's own entity is still being sent, but we need to ensure that we also
        // send the new parents, which may otherwise be delayed because of the PVS budget..
        ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(toSend, netEntity, out var exists);
        if (!exists)
        {
            var (entered, _) = ProcessEntry(in netEntity, lastAcked, lastSent, lastSeen, lastLeft, fromTick, ref newEntityCount, ref enteredEntityCount, newEntityBudget, enteredEntityBudget);
            AddToSendSet(in netEntity, metadata, ref value, toSend, fromTick, in entered, ref entStateCount);
        }

        if (addChildren)
        {
            RecursivelyAddChildren(xform, lastAcked, lastSent, toSend, lastSeen, lastLeft, fromTick, ref newEntityCount,
                ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
        }

        return true;
    }

    private void RecursivelyAddChildren(TransformComponent xform,
        Dictionary<NetEntity, PvsEntityVisibility>? lastAcked,
        Dictionary<NetEntity, PvsEntityVisibility>? lastSent,
        Dictionary<NetEntity, PvsEntityVisibility> toSend,
        Dictionary<NetEntity, GameTick> lastSeen,
        Dictionary<NetEntity, GameTick> lastLeft,
        in GameTick fromTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int entStateCount,
        in int newEntityBudget,
        in int enteredEntityBudget)
    {
        foreach (var child in xform.ChildEntities)
        {
            if (!_xformQuery.TryGetComponent(child, out var childXform))
                continue;

            var metadata = _metaQuery.GetComponent(child);
            var childNetEntity = metadata.NetEntity;

            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(toSend, childNetEntity, out var exists);
            if (!exists)
            {
                var (entered, _) = ProcessEntry(in childNetEntity, lastAcked, lastSent, lastSeen, lastLeft, fromTick, ref newEntityCount,
                    ref enteredEntityCount, newEntityBudget, enteredEntityBudget);

                AddToSendSet(in childNetEntity, metadata, ref value, toSend, fromTick, in entered, ref entStateCount);
            }

            RecursivelyAddChildren(childXform, lastAcked, lastSent, toSend, lastSeen, lastLeft, fromTick, ref newEntityCount,
                ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
        }
    }

    private (bool Entered, bool ShouldAdd) ProcessEntry(in NetEntity netEntity,
        Dictionary<NetEntity, PvsEntityVisibility>? lastAcked,
        Dictionary<NetEntity, PvsEntityVisibility>? lastSent,
        Dictionary<NetEntity, GameTick> lastSeen,
        Dictionary<NetEntity, GameTick> lastLeft,
        GameTick fromTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        in int newEntityBudget,
        in int enteredEntityBudget)
    {
        var enteredSinceLastSent = lastSent == null || !lastSent.ContainsKey(netEntity);

        var entered = enteredSinceLastSent
                      || lastAcked == null
                      || !lastAcked.ContainsKey(netEntity) // entered since last acked
                      || lastLeft.GetValueOrDefault(netEntity) >= fromTick; // Just in case a packet was lost. I love dictionary lookups

        // If the entity is entering, but we already sent this entering entity in the last message, we won't add it to
        // the budget. Chances are the packet will arrive in a nice and orderly fashion, and the client will stick to
        // their requested budget. However this can cause issues if a packet gets dropped, because a player may create
        // 2x or more times the normal entity creation budget.
        if (enteredSinceLastSent)
        {
            if (newEntityCount >= newEntityBudget || enteredEntityCount >= enteredEntityBudget)
                return (entered, false);

            enteredEntityCount++;
            if (!lastSeen.ContainsKey(netEntity))
                newEntityCount++;
        }

        return (entered, true);
    }

    private void AddToSendSet(in NetEntity netEntity, MetaDataComponent metaDataComponent,
        ref PvsEntityVisibility vis, Dictionary<NetEntity, PvsEntityVisibility> toSend,
        GameTick fromTick, in bool entered, ref int entStateCount)
    {
        if (metaDataComponent.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            toSend.Remove(netEntity);
            var rep = new EntityStringRepresentation(GetEntity(netEntity), metaDataComponent.EntityDeleted, metaDataComponent.EntityName, metaDataComponent.EntityPrototype?.ID);
            Log.Error($"Attempted to add a deleted entity to PVS send set: '{rep}'. Trace:\n{Environment.StackTrace}");
            return;
        }

        if (entered)
        {
            vis = PvsEntityVisibility.Entered;
            entStateCount++;
            return;
        }

        if (metaDataComponent.EntityLastModifiedTick <= fromTick)
        {
            //entity has been sent before and hasnt been updated since
            vis = PvsEntityVisibility.StayedUnchanged;
            return;
        }

        //add us
        vis = PvsEntityVisibility.StayedChanged;
        entStateCount++;
    }

    /// <summary>
    ///     Gets all entity states that have been modified after and including the provided tick.
    /// </summary>
    public (List<EntityState>?, List<NetEntity>?, GameTick fromTick) GetAllEntityStates(ICommonSession? player, GameTick fromTick, GameTick toTick)
    {
        List<EntityState>? stateEntities;
        var toSend = _uidSetPool.Get();
        DebugTools.Assert(toSend.Count == 0);
        bool enumerateAll = false;

        if (player == null)
        {
            enumerateAll = fromTick == GameTick.Zero;
        }
        else if (!_seenAllEnts.Contains(player))
        {
            enumerateAll = true;
            fromTick = GameTick.Zero;
        }

        if (_gameTiming.CurTick.Value - fromTick.Value > DirtyBufferSize)
        {
            // Fall back to enumerating over all entities.
            enumerateAll = true;
        }

        if (enumerateAll)
        {
            stateEntities = new List<EntityState>(EntityManager.EntityCount);
            var query = EntityManager.AllEntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out var uid, out var md))
            {
                DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized, $"Entity {ToPrettyString(uid)} has not been initialized");
                DebugTools.Assert(md.EntityLifeStage < EntityLifeStage.Terminating, $"Entity {ToPrettyString(uid)} is/has been terminated");
                if (md.EntityLastModifiedTick <= fromTick)
                    continue;

                var state = GetEntityState(player, uid, fromTick, md);

                if (state.Empty)
                {
                    Log.Error($@"{nameof(GetEntityState)} returned an empty state while enumerating entities.
Tick: {fromTick}--{_gameTiming.CurTick}
Entity: {ToPrettyString(uid)}
Last modified: {md.EntityLastModifiedTick}
Metadata last modified: {md.LastModifiedTick}
Transform last modified: {Transform(uid).LastModifiedTick}");
                }

                stateEntities.Add(state);
            }
        }
        else
        {
            stateEntities = new();
            for (var i = fromTick.Value + 1; i <= toTick.Value; i++)
            {
                if (!TryGetDirtyEntities(new GameTick(i), out var add, out var dirty))
                {
                    // This should be unreachable if `enumerateAll` is false.
                    throw new Exception($"Failed to get tick dirty data. tick: {i}, from: {fromTick}, to {toTick}, buffer: {DirtyBufferSize}");
                }

                foreach (var uid in add)
                {
                    if (!toSend.Add(uid) || !_metaQuery.TryGetComponent(uid, out var md))
                        continue;

                    DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized, $"Entity {ToPrettyString(uid)} has not been initialized");
                    DebugTools.Assert(md.EntityLifeStage < EntityLifeStage.Terminating, $"Entity {ToPrettyString(uid)} is/has been terminated");
                    DebugTools.Assert(md.EntityLastModifiedTick >= md.CreationTick, $"Entity {ToPrettyString(uid)} last modified tick is less than creation tick");
                    DebugTools.Assert(md.EntityLastModifiedTick > fromTick, $"Entity {ToPrettyString(uid)} last modified tick is less than from tick");

                    var state = GetEntityState(player, uid, fromTick, md);

                    if (state.Empty)
                    {
                        Log.Error($@"{nameof(GetEntityState)} returned an empty state for a new entity.
Tick: {fromTick}--{_gameTiming.CurTick}
Entity: {ToPrettyString(uid)}
Last modified: {md.EntityLastModifiedTick}
Metadata last modified: {md.LastModifiedTick}
Transform last modified: {Transform(uid).LastModifiedTick}");
                        continue;
                    }

                    stateEntities.Add(state);
                }

                foreach (var uid in dirty)
                {
                    DebugTools.Assert(!add.Contains(uid));
                    if (!toSend.Add(uid) || !_metaQuery.TryGetComponent(uid, out var md))
                        continue;

                    DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized, $"Entity {ToPrettyString(uid)} has not been initialized");
                    DebugTools.Assert(md.EntityLifeStage < EntityLifeStage.Terminating, $"Entity {ToPrettyString(uid)} is/has been terminated");
                    DebugTools.Assert(md.EntityLastModifiedTick >= md.CreationTick, $"Entity {ToPrettyString(uid)} last modified tick is less than creation tick");
                    DebugTools.Assert(md.EntityLastModifiedTick > fromTick, $"Entity {ToPrettyString(uid)} last modified tick is less than from tick");

                    var state = GetEntityState(player, uid, fromTick, md);
                    if (!state.Empty)
                        stateEntities.Add(state);
                }
            }
        }

        _uidSetPool.Return(toSend);
        var deletions = _entityPvsCollection.GetDeletedIndices(fromTick);

        if (stateEntities.Count == 0)
            stateEntities = null;

        return (stateEntities, deletions, fromTick);
    }

    /// <summary>
    /// Generates a network entity state for the given entity.
    /// </summary>
    /// <param name="player">The player to generate this state for. This may be null if the state is for replay recordings.</param>
    /// <param name="entityUid">Uid of the entity to generate the state from.</param>
    /// <param name="fromTick">Only provide delta changes from this tick.</param>
    /// <param name="meta">The entity's metadata component</param>
    /// <returns>New entity State for the given entity.</returns>
    private EntityState GetEntityState(ICommonSession? player, EntityUid entityUid, GameTick fromTick, MetaDataComponent meta)
    {
        var bus = EntityManager.EventBus;
        var changed = new List<ComponentChange>();

        bool sendCompList = meta.LastComponentRemoved > fromTick;
        HashSet<ushort>? netComps = sendCompList ? new() : null;

        foreach (var (netId, component) in meta.NetComponents)
        {
            if (!component.NetSyncEnabled)
                continue;

            if (component.Deleted || !component.Initialized)
            {
                Log.Error("Entity manager returned deleted or uninitialized components while sending entity data");
                continue;
            }

            if (component.SendOnlyToOwner && player != null && player.AttachedEntity != entityUid)
                continue;

            if (component.LastModifiedTick <= fromTick)
            {
                if (sendCompList && (!component.SessionSpecific || player == null || EntityManager.CanGetComponentState(bus, component, player)))
                    netComps!.Add(netId);
                continue;
            }

            if (component.SessionSpecific && player != null && !EntityManager.CanGetComponentState(bus, component, player))
                continue;

            var state = EntityManager.GetComponentState(bus, component, player, fromTick);
            DebugTools.Assert(fromTick > component.CreationTick || state is not IComponentDeltaState delta || delta.FullState);
            changed.Add(new ComponentChange(netId, state, component.LastModifiedTick));

            if (sendCompList)
                netComps!.Add(netId);
        }

        DebugTools.Assert(meta.EntityLastModifiedTick >= meta.LastComponentRemoved);
        DebugTools.Assert(GetEntity(meta.NetEntity) == entityUid);
        var entState = new EntityState(meta.NetEntity, changed, meta.EntityLastModifiedTick, netComps);

        return entState;
    }

    /// <summary>
    ///     Variant of <see cref="GetEntityState"/> that includes all entity data, including data that can be inferred implicitly from the entity prototype.
    /// </summary>
    private EntityState GetFullEntityState(ICommonSession player, EntityUid entityUid, MetaDataComponent meta)
    {
        var bus = EntityManager.EventBus;
        var changed = new List<ComponentChange>();

        HashSet<ushort> netComps = new();

        foreach (var (netId, component) in meta.NetComponents)
        {
            if (!component.NetSyncEnabled)
                continue;

            if (component.SendOnlyToOwner && player.AttachedEntity != entityUid)
                continue;

            if (component.SessionSpecific && !EntityManager.CanGetComponentState(bus, component, player))
                continue;

            var state = EntityManager.GetComponentState(bus, component, player, GameTick.Zero);
            DebugTools.Assert(state is not IComponentDeltaState delta || delta.FullState);
            changed.Add(new ComponentChange(netId, state, component.LastModifiedTick));
            netComps.Add(netId);
        }

        var entState = new EntityState(meta.NetEntity, changed, meta.EntityLastModifiedTick, netComps);

        return entState;
    }

    private EntityUid[] GetSessionViewers(ICommonSession session)
    {
        if (session.Status != SessionStatus.InGame)
            return Array.Empty<EntityUid>();

        // Fast path
        if (session.ViewSubscriptions.Count == 0)
        {
            if (session.AttachedEntity == null)
                return Array.Empty<EntityUid>();

            return new[] { session.AttachedEntity.Value };
        }

        var viewers = new HashSet<EntityUid>();
        if (session.AttachedEntity != null)
            viewers.Add(session.AttachedEntity.Value);

        viewers.UnionWith(session.ViewSubscriptions);
        return viewers.ToArray();
    }

    // Read Safe
    private (Vector2 worldPos, float range, MapId mapId) CalcViewBounds(in EntityUid euid)
    {
        var xform = _xformQuery.GetComponent(euid);
        return (_transform.GetWorldPosition(xform, _xformQuery), _viewSize / 2f, xform.MapID);
    }

    public sealed class TreePolicy<T> : PooledObjectPolicy<RobustTree<T>> where T : notnull
    {
        public override RobustTree<T> Create()
        {
            var pool = new DefaultObjectPool<HashSet<T>>(new SetPolicy<T>(), MaxVisPoolSize);
            return new RobustTree<T>(pool);
        }

        public override bool Return(RobustTree<T> obj)
        {
            obj.Clear();
            return true;
        }
    }

    private sealed class ChunkPoolPolicy<T> : PooledObjectPolicy<Dictionary<T, int>> where T : notnull
    {
        public override Dictionary<T, int> Create()
        {
            return new Dictionary<T, int>(32);
        }

        public override bool Return(Dictionary<T, int> obj)
        {
            obj.Clear();
            return true;
        }
    }

    /// <summary>
    ///     Class used to store per-session data in order to avoid having to lock dictionaries.
    /// </summary>
    internal sealed class SessionPVSData
    {
        /// <summary>
        /// All <see cref="EntityUid"/>s that this session saw during the last <see cref="DirtyBufferSize"/> ticks.
        /// </summary>
        public readonly OverflowDictionary<GameTick, Dictionary<NetEntity, PvsEntityVisibility>> SentEntities = new(DirtyBufferSize);

        /// <summary>
        ///     The most recently acked entities
        /// </summary>
        public (GameTick Tick, Dictionary<NetEntity, PvsEntityVisibility> Data)? LastAcked;

        /// <summary>
        ///     Stores the last tick at which a given entity was acked by a player. Used to avoid re-sending the whole entity
        ///     state when an item re-enters PVS.
        /// </summary>
        public readonly Dictionary<NetEntity, GameTick> LastSeenAt = new();

        /// <summary>
        ///     Tick at which an entity last left a player's PVS view.
        /// </summary>
        public readonly Dictionary<NetEntity, GameTick> LastLeftView = new();

        /// <summary>
        ///     <see cref="SentEntities"/> overflow in case a player's last ack is more than <see cref="DirtyBufferSize"/> ticks behind the current tick.
        /// </summary>
        public (GameTick Tick, Dictionary<NetEntity, PvsEntityVisibility> SentEnts)? Overflow;

        /// <summary>
        ///     If true, the client has explicitly requested a full state. Unlike the first state, we will send them
        ///     all data, not just data that cannot be implicitly inferred from entity prototypes.
        /// </summary>
        public bool RequestedFull = false;

        /// <summary>
        ///     The tick of the most recently received client Ack. Will be used to update <see cref="LastAcked"/>
        /// </summary>
        /// <remarks>
        ///     As the server delays processing acks, this might not currently be the same as <see cref="LastAcked"/>
        /// </remarks>
        public GameTick LastReceivedAck;
    }
}

[ByRefEvent]
public struct ExpandPvsEvent
{
    public readonly ICommonSession Session;

    /// <summary>
    /// List of entities that will get added to this session's PVS set.
    /// </summary>
    public List<EntityUid>? Entities;

    /// <summary>
    /// List of entities that will get added to this session's PVS set. Unlike <see cref="Entities"/> this will also
    /// recursively add all children of the given entity.
    /// </summary>
    public List<EntityUid>? RecursiveEntities;

    public ExpandPvsEvent(ICommonSession session)
    {
        Session = session;
    }
}
