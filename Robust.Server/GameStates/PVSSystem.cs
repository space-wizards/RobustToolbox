using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PVSSystem : EntitySystem
{
    [Shared.IoC.Dependency] private readonly IMapManagerInternal _mapManager = default!;
    [Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
    [Shared.IoC.Dependency] private readonly IConfigurationManager _configManager = default!;
    [Shared.IoC.Dependency] private readonly SharedTransformSystem _transform = default!;
    [Shared.IoC.Dependency] private readonly INetConfigurationManager _netConfigManager = default!;
    [Shared.IoC.Dependency] private readonly IServerGameStateManager _serverGameStateManager = default!;

    public const float ChunkSize = 8;

    // TODO make this a cvar. Make it in terms of seconds and tie it to tick rate?
    public const int TickBuffer = 20;
    // Note: If a client has ping higher than TickBuffer / TickRate, then the server will treat every entity as if it
    // had entered PVS for the first time. Note that due to the PVS budget, this buffer is easily overwhelmed.

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
    public HashSet<ICommonSession> SeenAllEnts = new();

    private readonly Dictionary<ICommonSession, SessionPVSData> _playerVisibleSets = new();

    private PVSCollection<EntityUid> _entityPvsCollection = default!;
    public PVSCollection<EntityUid> EntityPVSCollection => _entityPvsCollection;

    private readonly List<IPVSCollection> _pvsCollections = new();

    private readonly ObjectPool<Dictionary<EntityUid, PVSEntityVisiblity>> _visSetPool
        = new DefaultObjectPool<Dictionary<EntityUid, PVSEntityVisiblity>>(
            new DictPolicy<EntityUid, PVSEntityVisiblity>(), MaxVisPoolSize);

    private readonly ObjectPool<Stack<EntityUid>> _stackPool
        = new DefaultObjectPool<Stack<EntityUid>>(
            new StackPolicy<EntityUid>(), MaxVisPoolSize);

    private readonly ObjectPool<HashSet<EntityUid>> _uidSetPool
        = new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>(), MaxVisPoolSize);

    private readonly ObjectPool<Dictionary<EntityUid, MetaDataComponent>> _chunkCachePool =
        new DefaultObjectPool<Dictionary<EntityUid, MetaDataComponent>>(
            new DictPolicy<EntityUid, MetaDataComponent>(), MaxVisPoolSize);

    private readonly ObjectPool<HashSet<int>> _playerChunkPool =
        new DefaultObjectPool<HashSet<int>>(new SetPolicy<int>(), MaxVisPoolSize);

    private readonly ObjectPool<RobustTree<EntityUid>> _treePool =
        new DefaultObjectPool<RobustTree<EntityUid>>(new TreePolicy<EntityUid>(), MaxVisPoolSize);

    private readonly ObjectPool<Dictionary<MapChunkLocation, int>> _mapChunkPool =
        new DefaultObjectPool<Dictionary<MapChunkLocation, int>>(
            new ChunkPoolPolicy<MapChunkLocation>(), MaxVisPoolSize);

    private readonly ObjectPool<Dictionary<GridChunkLocation, int>> _gridChunkPool =
        new DefaultObjectPool<Dictionary<GridChunkLocation, int>>(
            new ChunkPoolPolicy<GridChunkLocation>(), MaxVisPoolSize);

    private readonly Dictionary<uint, Dictionary<MapChunkLocation, int>> _mapIndices = new(4);
    private readonly Dictionary<uint, Dictionary<GridChunkLocation, int>> _gridIndices = new(4);
    private readonly List<(uint, IChunkIndexLocation)> _chunkList = new(64);

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("PVS");

        _entityPvsCollection = RegisterPVSCollection<EntityUid>();

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

        _serverGameStateManager.ClientAck += OnClientAck;
        _serverGameStateManager.ClientRequestFull += OnClientRequestFull;

        InitializeDirty();
    }

    /// <summary>
    ///     Marks an entity's current chunk as dirty.
    /// </summary>
    internal void MarkDirty(EntityUid uid)
    {
        var query = GetEntityQuery<TransformComponent>();
        var xform = query.GetComponent(uid);
        var coordinates = _transform.GetMoverCoordinates(xform, query);
        _entityPvsCollection.MarkDirty(_entityPvsCollection.GetChunkIndex(coordinates));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        UnregisterPVSCollection(_entityPvsCollection);
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        EntityManager.EntityDeleted -= OnEntityDeleted;

        _configManager.UnsubValueChanged(CVars.NetPVS, SetPvs);
        _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnViewsizeChanged);

        _serverGameStateManager.ClientAck -= OnClientAck;
        _serverGameStateManager.ClientRequestFull -= OnClientRequestFull;

        ShutdownDirty();
    }

    // TODO rate limit this?
    private void OnClientRequestFull(ICommonSession session, GameTick tick, GameTick lastAcked, EntityUid? missingEntity)
    {
        if (!_playerVisibleSets.TryGetValue(session, out var sessionData))
            return;

        var sb = new StringBuilder();
        sb.Append($"Client {session} requested full state on tick {tick}. Last Acked: {lastAcked}.");

        if (missingEntity != null)
        {
            sb.Append($" Apparently they received an entity without metadata: {ToPrettyString(missingEntity.Value)}.");

            if (sessionData.LastSeenAt.TryGetValue(missingEntity.Value, out var lastSeenTick))
                sb.Append($" Entity last sent: {lastSeenTick.Value}");
        }

        _sawmill.Warning(sb.ToString());

        sessionData.LastSeenAt.Clear();

        if (sessionData.Overflow != null)
        {
            _visSetPool.Return(sessionData.Overflow.Value.SentEnts);
            sessionData.Overflow = null;
        }

        // return last acked to pool, but only if it is not still in the OverflowDictionary.
        if (sessionData.LastAcked != null && !sessionData.SentEntities.ContainsKey(lastAcked))
            _visSetPool.Return(sessionData.LastAcked);

        sessionData.LastAcked = null;
        sessionData.RequestedFull = true;
    }

    private void OnClientAck(ICommonSession session, GameTick ackedTick, GameTick lastAckedTick)
    {
        if (!_playerVisibleSets.TryGetValue(session, out var sessionData))
            return;

        if (sessionData.Overflow != null && sessionData.Overflow.Value.Tick < ackedTick)
        {
            var (overflowTick, overflowEnts) = sessionData.Overflow.Value;
            sessionData.Overflow = null;
            if (overflowTick == ackedTick)
            {
                ProcessAckedTick(sessionData, overflowEnts, ackedTick, lastAckedTick);
                return;
            }

            // Even though the acked tick is newer, we have no guarantee that the client received the cached set, so
            // we just discard it.
            _visSetPool.Return(overflowEnts);
        }

        if (sessionData.SentEntities.TryGetValue(ackedTick, out var ackedData))
            ProcessAckedTick(sessionData, ackedData, ackedTick, lastAckedTick);
    }

    private void ProcessAckedTick(SessionPVSData sessionData, Dictionary<EntityUid, PVSEntityVisiblity> ackedData, GameTick tick, GameTick lastAckedTick)
    {
        // return last acked to pool, but only if it is not still in the OverflowDictionary.
        if (sessionData.LastAcked != null && !sessionData.SentEntities.ContainsKey(lastAckedTick))
            _visSetPool.Return(sessionData.LastAcked);

        sessionData.LastAcked = ackedData;
        foreach (var ent in ackedData.Keys)
        {
            sessionData.LastSeenAt[ent] = tick;
        }

        // The client acked a tick. If they requested a full state, this ack happened some time after that, so we can safely set this to false
        sessionData.RequestedFull = false;
    }

    private void OnViewsizeChanged(float obj)
    {
        _viewSize = obj * 2;
    }

    private void SetPvs(bool value)
    {
        CullingEnabled = value;
    }

    public void ProcessCollections()
    {
        foreach (var collection in _pvsCollections)
        {
            collection.Process();
        }
    }

    public void Cleanup(IEnumerable<IPlayerSession> sessions)
    {
        var playerSessions = sessions.ToArray();

        if (!CullingEnabled)
        {
            foreach (var player in playerSessions)
            {
                SeenAllEnts.Add(player);
            }
        }
        else
        {
            SeenAllEnts.Clear();
        }

        CleanupDirty(playerSessions);

        foreach (var collection in _pvsCollections)
        {
            collection.ClearDirty();
        }
    }

    public void CullDeletionHistory(GameTick oldestAck)
    {
        _entityPvsCollection.CullDeletionHistoryUntil(oldestAck);
    }

    #region PVSCollection methods to maybe make public someday:tm:

    private PVSCollection<TIndex> RegisterPVSCollection<TIndex>() where TIndex : IComparable<TIndex>, IEquatable<TIndex>
    {
        var collection = new PVSCollection<TIndex>(EntityManager);
        _pvsCollections.Add(collection);
        return collection;
    }

    private bool UnregisterPVSCollection<TIndex>(PVSCollection<TIndex> pvsCollection) where TIndex : IComparable<TIndex>, IEquatable<TIndex> =>
        _pvsCollections.Remove(pvsCollection);

    #endregion

    #region PVSCollection Event Updates

    private void OnEntityDeleted(EntityUid e)
    {
        _entityPvsCollection.RemoveIndex(EntityManager.CurrentTick, e);

        var previousTick = _gameTiming.CurTick - 1;

        foreach (var sessionData in _playerVisibleSets.Values)
        {
            sessionData.LastSeenAt.Remove(e);
            if (sessionData.SentEntities.TryGetValue(previousTick, out var ents))
                ents.Remove(e);
        }
    }

    private void OnEntityMove(ref MoveEvent ev)
    {
        // GriddUid is only set after init.
        if (ev.Component.LifeStage < ComponentLifeStage.Initialized && ev.Component.GridUid == null)
            _transform.SetGridId(ev.Component, ev.Component.FindGridEntityId(GetEntityQuery<TransformComponent>()));

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

        var xformQuery = GetEntityQuery<TransformComponent>();
        var coordinates = _transform.GetMoverCoordinates(ev.Component, xformQuery);
        UpdateEntityRecursive(ev.Sender, ev.Component, coordinates, xformQuery, false, ev.ParentChanged);
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

        var xformQuery = GetEntityQuery<TransformComponent>();
        var coordinates = _transform.GetMoverCoordinates(component, xformQuery);
        UpdateEntityRecursive(uid, component, coordinates, xformQuery, false, false);
    }

    private void UpdateEntityRecursive(EntityUid uid, TransformComponent xform, EntityCoordinates coordinates, EntityQuery<TransformComponent> xformQuery, bool mover, bool forceDirty)
    {
        if (mover && !xform.LocalPosition.Equals(Vector2.Zero))
        {
            coordinates = _transform.GetMoverCoordinates(xform, xformQuery);
        }

        // since elements are cached grid-/map-relative, we don't need to update a given grids/maps children
        DebugTools.Assert(!_mapManager.IsGrid(uid) && !_mapManager.IsMap(uid));

        var indices = PVSCollection<EntityUid>.GetChunkIndices(coordinates.Position);
        if (xform.GridUid != null)
            _entityPvsCollection.UpdateIndex(uid, xform.GridUid.Value, indices, forceDirty: forceDirty);
        else
            _entityPvsCollection.UpdateIndex(uid, xform.MapID, indices, forceDirty: forceDirty);

        var children = xform.ChildEnumerator;

        // TODO PERFORMANCE
        // Given uid is the parent of its children, we already know that the child xforms will have to be relative to
        // coordiantes.EntityId. So instead of calling GetMoverCoordinates() for each child we should just calculate it
        // directly.
        while (children.MoveNext(out var child))
        {
            UpdateEntityRecursive(child.Value, xformQuery.GetComponent(child.Value), coordinates, xformQuery, true, forceDirty);
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            if (!_playerVisibleSets.TryAdd(e.Session, new()))
                _sawmill.Error($"Attempted to add player to _playerVisibleSets, but they were already present? Session:{e.Session}");

            foreach (var pvsCollection in _pvsCollections)
            {
                if (!pvsCollection.AddPlayer(e.Session))
                    _sawmill.Error($"Attempted to add player to pvsCollection, but they were already present? Session:{e.Session}");
            }
            return;
        }

        if (e.NewStatus != SessionStatus.Disconnected)
            return;

        foreach (var pvsCollection in _pvsCollections)
        {
            if (!pvsCollection.RemovePlayer(e.Session))
                _sawmill.Error($"Attempted to remove player from pvsCollection, but they were already removed? Session:{e.Session}");
        }

        if (!_playerVisibleSets.Remove(e.Session, out var data))
            return;

        if (data.Overflow != null)
            _visSetPool.Return(data.Overflow.Value.SentEnts);
        data.Overflow = null;

        if (data.LastAcked != null)
            _visSetPool.Return(data.LastAcked);

        foreach (var visSet in data.SentEntities.Values)
        {
            if (visSet != data.LastAcked)
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

        _entityPvsCollection.UpdateIndex(gridId);
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
        _entityPvsCollection.UpdateIndex(uid);
    }

    #endregion

    public (List<(uint, IChunkIndexLocation)> , HashSet<int>[], EntityUid[][] viewers) GetChunks(IPlayerSession[] sessions)
    {
        var playerChunks = new HashSet<int>[sessions.Length];
        var eyeQuery = EntityManager.GetEntityQuery<EyeComponent>();
        var transformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var viewerEntities = new EntityUid[sessions.Length][];

        _chunkList.Clear();
        // Keep track of the index of each chunk we use for a faster index lookup.
        // Pool it because this will allocate a lot across ticks as we scale in players.
        foreach (var chunks in _mapIndices.Values)
            _mapChunkPool.Return(chunks);

        foreach (var chunks in _gridIndices.Values)
            _gridChunkPool.Return(chunks);

        _mapIndices.Clear();
        _gridIndices.Clear();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var physicsQuery = GetEntityQuery<PhysicsComponent>();

        for (int i = 0; i < sessions.Length; i++)
        {
            var session = sessions[i];
            playerChunks[i] = _playerChunkPool.Get();

            var viewers = GetSessionViewers(session);
            viewerEntities[i] = viewers;

            for (var j = 0; j < viewers.Length; j++)
            {
                var eyeEuid = viewers[j];
                var (viewPos, range, mapId) = CalcViewBounds(in eyeEuid, transformQuery);

                if (mapId == MapId.Nullspace) continue;

                uint visMask = EyeComponent.DefaultVisibilityMask;
                if (eyeQuery.TryGetComponent(eyeEuid, out var eyeComp))
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

                var state = (i, transformQuery, viewPos, range, visMask, gridDict, playerChunks, _chunkList);

                _mapManager.FindGridsIntersectingApprox(mapId, new Box2(viewPos - range, viewPos + range),
                    ref state, static (
                        MapGridComponent mapGrid,
                        ref (int i,
                            EntityQuery<TransformComponent> transformQuery,
                            Vector2 viewPos,
                            float range,
                            uint visMask,
                            Dictionary<GridChunkLocation, int> gridDict,
                            HashSet<int>[] playerChunks,
                            List<(uint, IChunkIndexLocation)> _chunkList) tuple) =>
                    {
                        {
                            var localPos = tuple.transformQuery.GetComponent(mapGrid.Owner).InvWorldMatrix.Transform(tuple.viewPos);

                            var gridChunkEnumerator =
                                new ChunkIndicesEnumerator(localPos, tuple.range, ChunkSize);

                            while (gridChunkEnumerator.MoveNext(out var gridChunkIndices))
                            {
                                var chunkLocation = new GridChunkLocation(mapGrid.Owner, gridChunkIndices.Value);
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

    private Dictionary<(uint visMask, IChunkIndexLocation location), (Dictionary<EntityUid, MetaDataComponent> metadata,
        RobustTree<EntityUid> tree)?> _previousTrees = new();

    private HashSet<(uint visMask, IChunkIndexLocation location)> _reusedTrees = new();

    public void RegisterNewPreviousChunkTrees(
        List<(uint, IChunkIndexLocation)> chunks,
        (Dictionary<EntityUid, MetaDataComponent> metadata, RobustTree<EntityUid> tree)?[] trees,
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
        uint visMask,
        EntityQuery<TransformComponent> transform,
        EntityQuery<MetaDataComponent> metadata,
        out (Dictionary<EntityUid, MetaDataComponent> mData, RobustTree<EntityUid> tree)? result)
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
        foreach (var uid in chunk)
        {
            AddToChunkSetRecursively(in uid, visMask, tree, chunkSet, transform, metadata);
        }

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

    private bool AddToChunkSetRecursively(in EntityUid uid, uint visMask, RobustTree<EntityUid> tree, Dictionary<EntityUid, MetaDataComponent> set, EntityQuery<TransformComponent> transform,
        EntityQuery<MetaDataComponent> metadata)
    {
        //are we valid?
        //sometimes uids gets added without being valid YET (looking at you mapmanager) (mapcreate & gridcreated fire before the uids becomes valid)
        if (!uid.IsValid()) return false;

        if (set.ContainsKey(uid)) return true;

        var mComp = metadata.GetComponent(uid);

        // TODO: Don't need to know about parents so no longer need to use bool for this method.
        // If the eye is missing ANY layer this entity or any of its parents belongs to, it is considered invisible.
        if ((visMask & mComp.VisibilityMask) != mComp.VisibilityMask)
            return false;

        var parent = transform.GetComponent(uid).ParentUid;

        if (parent.IsValid() && //is it not a worldentity?
            !set.ContainsKey(parent) && //was the parent not yet added to toSend?
            !AddToChunkSetRecursively(in parent, visMask, tree, set, transform, metadata)) //did we just fail to add the parent?
            return false; //we failed? suppose we dont get added either

        //i want it to crash here if it gets added double bc that shouldnt happen and will add alot of unneeded cycles
        tree.Set(uid, parent);
        set.Add(uid, mComp);
        return true;
    }

    public (List<EntityState>? updates, List<EntityUid>? deletions, List<EntityUid>? leftPvs, GameTick fromTick) CalculateEntityStates(IPlayerSession session,
        GameTick fromTick, GameTick toTick,
        (Dictionary<EntityUid, MetaDataComponent> metadata, RobustTree<EntityUid> tree)?[] chunkCache,
        HashSet<int> chunkIndices, EntityQuery<MetaDataComponent> mQuery, EntityQuery<TransformComponent> tQuery,
        EntityUid[] viewerEntities)
    {
        DebugTools.Assert(session.Status == SessionStatus.InGame);
        var newEntityBudget = _netConfigManager.GetClientCVar(session.ConnectedClient, CVars.NetPVSEntityBudget);
        var enteredEntityBudget = _netConfigManager.GetClientCVar(session.ConnectedClient, CVars.NetPVSEntityEnterBudget);
        var newEntityCount = 0;
        var enteredEntityCount = 0;
        var sessionData = _playerVisibleSets[session];
        sessionData.SentEntities.TryGetValue(toTick - 1, out var lastSent);
        var lastAcked = sessionData.LastAcked;
        var lastSeen = sessionData.LastSeenAt;
        var visibleEnts = _visSetPool.Get();

        if (visibleEnts.Count != 0)
            throw new Exception("Encountered non-empty object inside of _visSetPool. Was the same object returned to the pool more than once?");

        var deletions = _entityPvsCollection.GetDeletedIndices(fromTick);

        var entStateCount = 0;

        var stack = _stackPool.Get();
        // TODO reorder chunks to prioritize those that are closest to the viewer? Helps make pop-in less visible.
        foreach (var i in chunkIndices)
        {
            var cache = chunkCache[i];
            if(!cache.HasValue) continue;
            foreach (var rootNode in cache.Value.tree.RootNodes)
            {
                RecursivelyAddTreeNode(in rootNode, cache.Value.tree, lastAcked, lastSent, visibleEnts, lastSeen, cache.Value.metadata, stack, in fromTick,
                        ref newEntityCount, ref enteredEntityCount, ref entStateCount,  in newEntityBudget, in enteredEntityBudget);
            }
        }
        _stackPool.Return(stack);

        var globalEnumerator = _entityPvsCollection.GlobalOverridesEnumerator;
        while (globalEnumerator.MoveNext())
        {
            var uid = globalEnumerator.Current;
            RecursivelyAddOverride(in uid, lastAcked, lastSent, visibleEnts, lastSeen, in mQuery, in tQuery, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
        }
        globalEnumerator.Dispose();

        var localEnumerator = _entityPvsCollection.GetElementsForSession(session);
        while (localEnumerator.MoveNext())
        {
            var uid = localEnumerator.Current;
            RecursivelyAddOverride(in uid, lastAcked, lastSent, visibleEnts, lastSeen, in mQuery, in tQuery, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
        }
        localEnumerator.Dispose();

        foreach (var viewerEntity in viewerEntities)
        {
            RecursivelyAddOverride(in viewerEntity, lastAcked, lastSent, visibleEnts, lastSeen, in mQuery, in tQuery, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
        }

        var expandEvent = new ExpandPvsEvent(session, new List<EntityUid>());
        RaiseLocalEvent(ref expandEvent);
        foreach (var entityUid in expandEvent.Entities)
        {
            RecursivelyAddOverride(in entityUid, lastAcked, lastSent, visibleEnts, lastSeen, in mQuery, in tQuery, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget);
        }

        var entityStates = new List<EntityState>(entStateCount);

        foreach (var (uid, visiblity) in visibleEnts)
        {
            // if an entity is visible, its parents should always be visible.
            DebugTools.Assert((tQuery.GetComponent(uid).ParentUid is not { Valid: true } parent) || visibleEnts.ContainsKey(parent),
                $"Attempted to send an entity without sending it's parents. Entity: {ToPrettyString(uid)}.");

            if (sessionData.RequestedFull)
            {
                entityStates.Add(GetFullEntityState(session, uid, mQuery.GetComponent(uid)));
                continue;
            }

            if (visiblity == PVSEntityVisiblity.StayedUnchanged)
                continue;

            var entered = visiblity == PVSEntityVisiblity.Entered;
            var entFromTick = entered ? lastSeen.GetValueOrDefault(uid) : fromTick;
            var state = GetEntityState(session, uid, entFromTick, mQuery.GetComponent(uid));

            if (entered || !state.Empty)
                entityStates.Add(state);
        }

        // tell a client to detach entities that have left their view
        var leftView = ProcessLeavePVS(visibleEnts, lastSent);

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

#if !FULL_RELEASE
                // This happens relatively frequently for the current TickBuffer value, and doesn't really provide any
                // useful info when not debugging/testing locally. Hence disabled on FULL_RELEASE.
                _sawmill.Debug($"Client {session} exceeded tick buffer.");
#endif
            }
            else if (oldEntry.Value.Value != lastAcked)
                _visSetPool.Return(oldEntry.Value.Value);
        }

        if (deletions.Count == 0) deletions = default;
        if (entityStates.Count == 0) entityStates = default;
        return (entityStates, deletions, leftView, sessionData.RequestedFull ? GameTick.Zero : fromTick);
    }

    /// <summary>
    ///     Figure out what entities are no longer visible to the client. These entities are sent reliably to the client
    ///     in a separate net message.
    /// </summary>
    private List<EntityUid>? ProcessLeavePVS(
        Dictionary<EntityUid, PVSEntityVisiblity> visibleEnts,
        Dictionary<EntityUid, PVSEntityVisiblity>? lastSent)
    {
        if (lastSent == null)
            return null;

        var leftView = new List<EntityUid>();
        foreach (var uid in lastSent.Keys)
        {
            if (!visibleEnts.ContainsKey(uid))
                leftView.Add(uid);
        }

        return leftView.Count > 0 ? leftView : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void RecursivelyAddTreeNode(in EntityUid nodeIndex,
        RobustTree<EntityUid> tree,
        Dictionary<EntityUid, PVSEntityVisiblity>? lastAcked,
        Dictionary<EntityUid, PVSEntityVisiblity>? lastSent,
        Dictionary<EntityUid, PVSEntityVisiblity> toSend,
        Dictionary<EntityUid, GameTick> lastSeen,
        Dictionary<EntityUid, MetaDataComponent> metaDataCache,
        Stack<EntityUid> stack,
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
            //are we valid?
            //sometimes uids gets added without being valid YET (looking at you mapmanager) (mapcreate & gridcreated fire before the uids becomes valid)

            // As every map is parented to uid 0 in the tree we still need to get their children, plus because we go top-down
            // we may find duplicate parents with children we haven't encountered before
            // on different chunks (this is especially common with direct grid children)
            if (currentNodeIndex.IsValid() && !toSend.ContainsKey(currentNodeIndex))
            {
                var (entered, shouldAdd) = ProcessEntry(in currentNodeIndex, lastAcked, lastSent, lastSeen,
                    ref newEntityCount, ref enteredEntityCount, newEntityBudget, enteredEntityBudget);

                if (!shouldAdd)
                    continue;

                AddToSendSet(in currentNodeIndex, metaDataCache[currentNodeIndex], toSend, fromTick, in entered, ref entStateCount);
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

    public bool RecursivelyAddOverride(
        in EntityUid uid,
        Dictionary<EntityUid, PVSEntityVisiblity>? lastAcked,
        Dictionary<EntityUid, PVSEntityVisiblity>? lastSent,
        Dictionary<EntityUid, PVSEntityVisiblity> toSend,
        Dictionary<EntityUid, GameTick> lastSeen,
        in EntityQuery<MetaDataComponent> metaQuery,
        in EntityQuery<TransformComponent> transQuery,
        in GameTick fromTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int entStateCount,
        in int newEntityBudget,
        in int enteredEntityBudget)
    {
        //are we valid?
        //sometimes uids gets added without being valid YET (looking at you mapmanager) (mapcreate & gridcreated fire before the uids becomes valid)
        if (!uid.IsValid()) return false;

        var parent = transQuery.GetComponent(uid).ParentUid;
        if (parent.IsValid() && !RecursivelyAddOverride(in parent, lastAcked, lastSent, toSend, lastSeen, in metaQuery, in transQuery, in fromTick,
                ref newEntityCount, ref enteredEntityCount, ref entStateCount, in newEntityBudget, in enteredEntityBudget))
            return false;

        //did we already get added?
        if (toSend.ContainsKey(uid)) return true;
        // Note that we check this AFTER adding parents. This is because while this entity may already have been added
        // to the toSend set, it doesn't guarantee that its parents have been. E.g., if a player ghost just teleported
        // to follow a far away entity, the player's own entity is still being sent, but we need to ensure that we also
        // send the new parents, which may otherwise be delayed because of the PVS budget..

        // TODO PERFORMANCE.
        // ProcessEntry() unnecessarily checks lastSent.ContainsKey() and maybe lastSeen.Contains(). Given that at this
        // point the budgets are just ignored, this should just bypass those checks. But then again 99% of the time this
        // is just the player's own entity + maybe a singularity. So currently not all that performance intensive.
        var (entered, _) = ProcessEntry(in uid, lastAcked, lastSent, lastSeen, ref newEntityCount, ref enteredEntityCount, newEntityBudget, enteredEntityBudget);

        AddToSendSet(in uid, metaQuery.GetComponent(uid), toSend, fromTick, in entered, ref entStateCount);
        return true;
    }

    private (bool Entered, bool ShouldAdd) ProcessEntry(in EntityUid uid,
        Dictionary<EntityUid, PVSEntityVisiblity>? lastAcked,
        Dictionary<EntityUid, PVSEntityVisiblity>? lastSent,
        Dictionary<EntityUid, GameTick> lastSeen,
        ref int newEntityCount,
        ref int enteredEntityCount,
        in int newEntityBudget,
        in int enteredEntityBudget)
    {
        var enteredSinceLastSent = lastSent == null || !lastSent.ContainsKey(uid);

        var entered = enteredSinceLastSent || // OR, entered since last ack:
                        lastAcked == null || !lastAcked.ContainsKey(uid);

        // If the entity is entering, but we already sent this entering entity in the last message, we won't add it to
        // the budget. Chances are the packet will arrive in a nice and orderly fashion, and the client will stick to
        // their requested budget. However this can cause issues if a packet gets dropped, because a player may create
        // 2x or more times the normal entity creation budget.
        //
        // The fix for that would be to just also give the PVS budget a client-side aspect that controls entity creation
        // rate.
        if (enteredSinceLastSent)
        {
            if (newEntityCount >= newEntityBudget || enteredEntityCount >= enteredEntityBudget)
                return (entered, false);

            enteredEntityCount++;
            if (!lastSeen.ContainsKey(uid))
                newEntityCount++;
        }

        return (entered, true);
    }

    private void AddToSendSet(in EntityUid uid, MetaDataComponent metaDataComponent, Dictionary<EntityUid, PVSEntityVisiblity> toSend, GameTick fromTick, in bool entered, ref int entStateCount)
    {
        // This check shouldn't be required, but temporarily adding it to try debug PVS errors.
        if (metaDataComponent.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            var rep = new EntityStringRepresentation(uid, metaDataComponent.EntityDeleted, metaDataComponent.EntityName, metaDataComponent.EntityPrototype?.ID);
            _sawmill.Error($"Attempted to add a deleted entity to PVS send set: '{rep}'. Trace:\n{Environment.StackTrace}");
            return;
        }

        if (entered)
        {
            toSend.Add(uid, PVSEntityVisiblity.Entered);
            entStateCount++;
            return;
        }

        if (metaDataComponent.EntityLastModifiedTick <= fromTick)
        {
            //entity has been sent before and hasnt been updated since
            toSend.Add(uid, PVSEntityVisiblity.StayedUnchanged);
            return;
        }

        //add us
        toSend.Add(uid, PVSEntityVisiblity.StayedChanged);
        entStateCount++;
    }

    /// <summary>
    ///     Gets all entity states that have been modified after and including the provided tick.
    /// </summary>
    public (List<EntityState>?, List<EntityUid>?, List<EntityUid>?, GameTick fromTick) GetAllEntityStates(ICommonSession player, GameTick fromTick, GameTick toTick)
    {
        var deletions = _entityPvsCollection.GetDeletedIndices(fromTick);
        // no point sending an empty collection
        if (deletions.Count == 0) deletions = default;

        var stateEntities = new List<EntityState>();
        var seenEnts = new HashSet<EntityUid>();
        var slowPath = false;
        var metadataQuery = EntityManager.GetEntityQuery<MetaDataComponent>();

        if (!SeenAllEnts.Contains(player))
        {
            // Give them E V E R Y T H I N G
            stateEntities = new List<EntityState>(EntityManager.EntityCount);

            // This is the same as iterating every existing entity.
            foreach (var md in EntityManager.EntityQuery<MetaDataComponent>(true))
            {
                DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized);
                stateEntities.Add(GetEntityState(player, md.Owner, GameTick.Zero, md));
            }

            return (stateEntities.Count == 0 ? default : stateEntities, deletions, null, fromTick);
        }

        // Just get the relevant entities that have been dirtied
        // This should be extremely fast.
        if (!slowPath)
        {
            for (var i = fromTick.Value; i <= toTick.Value; i++)
            {
                // Fallback to dumping every entity on them.
                var tick = new GameTick(i);
                if (!TryGetTick(tick, out var add, out var dirty))
                {
                    slowPath = true;
                    break;
                }

                foreach (var uid in add)
                {
                    if (!seenEnts.Add(uid)) continue;
                    // This is essentially the same as IEntityManager.EntityExists, but returning MetaDataComponent.
                    if (!metadataQuery.TryGetComponent(uid, out var md)) continue;

                    DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized);

                    if (md.EntityLastModifiedTick > fromTick)
                        stateEntities.Add(GetEntityState(player, uid, GameTick.Zero, md));
                }

                foreach (var uid in dirty)
                {
                    DebugTools.Assert(!add.Contains(uid));

                    if (!seenEnts.Add(uid)) continue;
                    if (!metadataQuery.TryGetComponent(uid, out var md)) continue;

                    DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized);

                    if (md.EntityLastModifiedTick > fromTick)
                        stateEntities.Add(GetEntityState(player, uid, fromTick, md));
                }
            }
        }

        if (!slowPath)
        {
            if (stateEntities.Count == 0) stateEntities = default;

            return (stateEntities, deletions, null, fromTick);
        }

        stateEntities = new List<EntityState>(EntityManager.EntityCount);

        // This is the same as iterating every existing entity.
        foreach (var md in EntityManager.EntityQuery<MetaDataComponent>(true))
        {
            DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized);

            if (md.EntityLastModifiedTick >= fromTick)
                stateEntities.Add(GetEntityState(player, md.Owner, fromTick, md));
        }

        // no point sending an empty collection
        if (stateEntities.Count == 0) stateEntities = default;

        return (stateEntities, deletions, null, fromTick);
    }

    /// <summary>
    /// Generates a network entity state for the given entity.
    /// </summary>
    /// <param name="player">The player to generate this state for.</param>
    /// <param name="entityUid">Uid of the entity to generate the state from.</param>
    /// <param name="fromTick">Only provide delta changes from this tick.</param>
    /// <param name="meta">The entity's metadata component</param>
    /// <returns>New entity State for the given entity.</returns>
    private EntityState GetEntityState(ICommonSession player, EntityUid entityUid, GameTick fromTick, MetaDataComponent meta)
    {
        var bus = EntityManager.EventBus;
        var changed = new List<ComponentChange>();

        bool sendCompList = meta.LastComponentRemoved > fromTick;
        HashSet<ushort>? netComps = sendCompList ? new() : null;

        foreach (var (netId, component) in EntityManager.GetNetComponents(entityUid))
        {
            if (!component.NetSyncEnabled)
                continue;

            if (component.Deleted || !component.Initialized)
            {
                _sawmill.Error("Entity manager returned deleted or uninitialized components while sending entity data");
                continue;
            }

            if (component.SendOnlyToOwner && player.AttachedEntity != component.Owner)
                continue;

            if (component.LastModifiedTick <= fromTick)
            {
                if (sendCompList && (!component.SessionSpecific || EntityManager.CanGetComponentState(bus, component, player)))
                    netComps!.Add(netId);
                continue;
            }

            if (component.SessionSpecific && !EntityManager.CanGetComponentState(bus, component, player))
                continue;

            var state = EntityManager.GetComponentState(bus, component, component.SessionSpecific ? player : null);
            changed.Add(new ComponentChange(netId, state, component.LastModifiedTick));

            if (sendCompList)
                netComps!.Add(netId);
        }

        DebugTools.Assert(meta.EntityLastModifiedTick >= meta.LastComponentRemoved);
        var entState = new EntityState(entityUid, changed, meta.EntityLastModifiedTick, netComps);

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

        foreach (var (netId, component) in EntityManager.GetNetComponents(entityUid))
        {
            if (!component.NetSyncEnabled)
                continue;

            if (component.SendOnlyToOwner && player.AttachedEntity != component.Owner)
                continue;

            if (component.SessionSpecific && !EntityManager.CanGetComponentState(bus, component, player))
                continue;

            changed.Add(new ComponentChange(netId, EntityManager.GetComponentState(bus, component, component.SessionSpecific ? player : null), component.LastModifiedTick));
            netComps.Add(netId);
        }

        var entState = new EntityState(entityUid, changed, meta.EntityLastModifiedTick, netComps);

        return entState;
    }

    private EntityUid[] GetSessionViewers(ICommonSession session)
    {
        if (session.Status != SessionStatus.InGame)
            return Array.Empty<EntityUid>();

        var viewers = _uidSetPool.Get();

        if (session.AttachedEntity != null)
        {
            // Fast path
            if (session is IPlayerSession { ViewSubscriptionCount: 0 })
            {
                _uidSetPool.Return(viewers);
                return new[] { session.AttachedEntity.Value };
            }

            viewers.Add(session.AttachedEntity.Value);
        }

        // This is awful, but we're not gonna add the list of view subscriptions to common session.
        if (session is IPlayerSession playerSession)
        {
            foreach (var uid in playerSession.ViewSubscriptions)
            {
                viewers.Add(uid);
            }
        }

        var viewerArray = viewers.ToArray();

        _uidSetPool.Return(viewers);
        return viewerArray;
    }

    // Read Safe
    private (Vector2 worldPos, float range, MapId mapId) CalcViewBounds(in EntityUid euid, EntityQuery<TransformComponent> transformQuery)
    {
        var xform = transformQuery.GetComponent(euid);
        return (xform.WorldPosition, _viewSize / 2f, xform.MapID);
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
    ///     Session data class used to avoid having to lock session dictionaries.
    /// </summary>
    private sealed class SessionPVSData
    {
        /// <summary>
        /// All <see cref="EntityUid"/>s that this session saw during the last <see cref="TickBuffer"/> ticks.
        /// </summary>
        public readonly OverflowDictionary<GameTick, Dictionary<EntityUid, PVSEntityVisiblity>> SentEntities = new(TickBuffer);

        /// <summary>
        ///     The most recently acked entities
        /// </summary>
        public Dictionary<EntityUid, PVSEntityVisiblity>? LastAcked = new();

        /// <summary>
        ///     Stores the last tick at which a given entity was acked by a player. Used to avoid re-sending the whole entity
        ///     state when an item re-enters PVS.
        /// </summary>
        public readonly Dictionary<EntityUid, GameTick> LastSeenAt = new();

        /// <summary>
        ///     <see cref="_sentData"/> overflow in case a player's last ack is more than <see cref="TickBuffer"/> ticks behind the current tick.
        /// </summary>
        public (GameTick Tick, Dictionary<EntityUid, PVSEntityVisiblity> SentEnts)? Overflow;

        /// <summary>
        ///     If true, the client has explicitly requested a full state. Unlike the first state, we will send them
        ///     all data, not just data that cannot be implicitly inferred from entity prototypes.
        /// </summary>
        public bool RequestedFull = false;
    }
}

[ByRefEvent]
public readonly struct ExpandPvsEvent
{
    public readonly IPlayerSession Session;
    public readonly List<EntityUid> Entities;

    public ExpandPvsEvent(IPlayerSession session, List<EntityUid> entities)
    {
        Session = session;
        Entities = entities;
    }
}
