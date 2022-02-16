using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;
using NetSerializer;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PVSSystem : EntitySystem
{
    [Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
    [Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
    [Shared.IoC.Dependency] private readonly IConfigurationManager _configManager = default!;
    [Shared.IoC.Dependency] private readonly IServerGameStateManager _stateManager = default!;

    public const float ChunkSize = 8;

    /// <summary>
    /// Maximum number of pooled objects
    /// </summary>
    private const int MaxVisPoolSize = 1024;

    /// <summary>
    /// Is view culling enabled, or will we send the whole map?
    /// </summary>
    public bool CullingEnabled { get; private set; }

    /// <summary>
    /// How many new entities we can send per tick (dont wanna nuke the clients mailbox).
    /// </summary>
    private int _newEntityBudget;

    /// <summary>
    /// How many entered entities can be sent per tick.
    /// </summary>
    private int _entityBudget;

    /// <summary>
    /// Size of the side of the view bounds square.
    /// </summary>
    private float _viewSize;

    /// <summary>
    /// All <see cref="Robust.Shared.GameObjects.EntityUid"/>s a <see cref="ICommonSession"/> saw last iteration.
    /// </summary>
    private readonly Dictionary<ICommonSession, Dictionary<EntityUid, PVSEntityVisiblity>> _playerVisibleSets = new();
    /// <summary>
    /// All <see cref="Robust.Shared.GameObjects.EntityUid"/>s a <see cref="ICommonSession"/> saw along its entire connection.
    /// </summary>
    private readonly Dictionary<ICommonSession, HashSet<EntityUid>> _playerSeenSets = new();

    private PVSCollection<EntityUid> _entityPvsCollection = default!;
    public PVSCollection<EntityUid> EntityPVSCollection => _entityPvsCollection;
    private readonly List<IPVSCollection> _pvsCollections = new();

    private readonly ObjectPool<Dictionary<EntityUid, PVSEntityVisiblity>> _visSetPool
        = new DefaultObjectPool<Dictionary<EntityUid, PVSEntityVisiblity>>(
            new DictPolicy<EntityUid, PVSEntityVisiblity>(), MaxVisPoolSize);

    private readonly ObjectPool<HashSet<EntityUid>> _uidSetPool
        = new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>(), MaxVisPoolSize);

    private readonly ObjectPool<Dictionary<EntityUid, MetaDataComponent>> _chunkCachePool =
        new DefaultObjectPool<Dictionary<EntityUid, MetaDataComponent>>(
            new DictPolicy<EntityUid, MetaDataComponent>(), MaxVisPoolSize);

    private readonly ObjectPool<HashSet<int>> _playerChunkPool =
        new DefaultObjectPool<HashSet<int>>(new SetPolicy<int>(), MaxVisPoolSize);

    public override void Initialize()
    {
        base.Initialize();

        _entityPvsCollection = RegisterPVSCollection<EntityUid>();
        _mapManager.MapCreated += OnMapCreated;
        _mapManager.MapDestroyed += OnMapDestroyed;
        _mapManager.OnGridCreated += OnGridCreated;
        _mapManager.OnGridRemoved += OnGridRemoved;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<MoveEvent>(OnEntityMove);
        SubscribeLocalEvent<TransformComponent, ComponentInit>(OnTransformInit);
        EntityManager.EntityDeleted += OnEntityDeleted;

        _configManager.OnValueChanged(CVars.NetPVS, SetPvs, true);
        _configManager.OnValueChanged(CVars.NetMaxUpdateRange, OnViewsizeChanged, true);
        _configManager.OnValueChanged(CVars.NetPVSNewEntityBudget, OnNewEntityBudgetChanged, true);
        _configManager.OnValueChanged(CVars.NetPVSEntityBudget, OnEntityBudgetChanged, true);

        InitializeDirty();
    }

    private void OnEntityBudgetChanged(int obj)
    {
        _entityBudget = obj;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        UnregisterPVSCollection(_entityPvsCollection);
        _mapManager.MapCreated -= OnMapCreated;
        _mapManager.MapDestroyed -= OnMapDestroyed;
        _mapManager.OnGridCreated -= OnGridCreated;
        _mapManager.OnGridRemoved -= OnGridRemoved;
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        EntityManager.EntityDeleted -= OnEntityDeleted;

        _configManager.UnsubValueChanged(CVars.NetPVS, SetPvs);
        _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnViewsizeChanged);
        _configManager.UnsubValueChanged(CVars.NetPVSEntityBudget, OnEntityBudgetChanged);

        ShutdownDirty();
    }

    private void OnViewsizeChanged(float obj)
    {
        _viewSize = obj * 2;
    }

    private void SetPvs(bool value)
    {
        CullingEnabled = value;
    }

    private void OnNewEntityBudgetChanged(int obj)
    {
        _newEntityBudget = obj;
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
        CleanupDirty(sessions);
    }

    public void CullDeletionHistory(GameTick oldestAck)
    {
        _entityPvsCollection.CullDeletionHistoryUntil(oldestAck);
    }

    #region PVSCollection methods to maybe make public someday:tm:

    private PVSCollection<TIndex> RegisterPVSCollection<TIndex>() where TIndex : IComparable<TIndex>, IEquatable<TIndex>
    {
        var collection = new PVSCollection<TIndex>();
        _pvsCollections.Add(collection);
        return collection;
    }

    private bool UnregisterPVSCollection<TIndex>(PVSCollection<TIndex> pvsCollection) where TIndex : IComparable<TIndex>, IEquatable<TIndex> =>
        _pvsCollections.Remove(pvsCollection);

    #endregion

    #region PVSCollection Event Updates

    private void OnEntityDeleted(object? sender, EntityUid e)
    {
        _entityPvsCollection.RemoveIndex(EntityManager.CurrentTick, e);
    }

    private void OnEntityMove(ref MoveEvent ev)
    {
        UpdateEntityRecursive(ev.Sender, ev.Component);
    }

    private void OnTransformInit(EntityUid uid, TransformComponent component, ComponentInit args)
    {
        UpdateEntityRecursive(uid, component);
    }

    private void UpdateEntityRecursive(EntityUid uid, TransformComponent? transformComponent = null)
    {
        if(!Resolve(uid, ref transformComponent))
            return;

        _entityPvsCollection.UpdateIndex(uid, transformComponent.Coordinates);

        // since elements are cached grid-/map-relative, we dont need to update a given grids/maps children
        if(_mapManager.IsGrid(uid) || _mapManager.IsMap(uid)) return;

        var children = transformComponent.ChildEnumerator;

        while (children.MoveNext(out var child))
        {
            UpdateEntityRecursive(child.Value);
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            _playerVisibleSets.Add(e.Session, _visSetPool.Get());
            _playerSeenSets.Add(e.Session, new HashSet<EntityUid>());
            foreach (var pvsCollection in _pvsCollections)
            {
                pvsCollection.AddPlayer(e.Session);
            }
        }
        else if (e.NewStatus == SessionStatus.Disconnected)
        {
            _visSetPool.Return(_playerVisibleSets[e.Session]);
            _playerVisibleSets.Remove(e.Session);
            _playerSeenSets.Remove(e.Session);
            foreach (var pvsCollection in _pvsCollections)
            {
                pvsCollection.RemovePlayer(e.Session);
            }
        }
    }

    private void OnGridRemoved(MapId mapId, GridId gridId)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.RemoveGrid(gridId);
        }
    }

    private void OnGridCreated(MapId mapId, GridId gridId)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.AddGrid(gridId);
        }

        var euid = _mapManager.GetGridEuid(gridId);
        _entityPvsCollection.UpdateIndex(euid);
    }

    private void OnMapDestroyed(object? sender, MapEventArgs e)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.RemoveMap(e.Map);
        }
    }

    private void OnMapCreated(object? sender, MapEventArgs e)
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
        var chunkList = new List<(uint, IChunkIndexLocation)>();
        var playerChunks = new HashSet<int>[sessions.Length];
        var eyeQuery = EntityManager.GetEntityQuery<EyeComponent>();
        var transformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var viewerEntities = new EntityUid[sessions.Length][];

        // Keep track of the index of each chunk we use for a faster index lookup.
        var mapIndices = new Dictionary<uint, Dictionary<MapChunkLocation, int>>(4);
        var gridIndices = new Dictionary<uint, Dictionary<GridChunkLocation, int>>(4);

        for (int i = 0; i < sessions.Length; i++)
        {
            var session = sessions[i];
            playerChunks[i] = _playerChunkPool.Get();

            var viewers = GetSessionViewers(session);
            viewerEntities[i] = new EntityUid[viewers.Count];
            viewers.CopyTo(viewerEntities[i]);

            foreach (var eyeEuid in viewers)
            {
                var (viewPos, range, mapId) = CalcViewBounds(in eyeEuid, transformQuery);

                uint visMask = EyeComponent.DefaultVisibilityMask;
                if (eyeQuery.TryGetComponent(eyeEuid, out var eyeComp))
                    visMask = eyeComp.VisibilityMask;

                // Get the nyoom dictionary for index lookups.
                if (!mapIndices.TryGetValue(visMask, out var mapDict))
                {
                    mapDict = new Dictionary<MapChunkLocation, int>(32);
                    mapIndices[visMask] = mapDict;
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
                        playerChunks[i].Add(chunkList.Count);
                        mapDict.Add(chunkLocation, chunkList.Count);
                        chunkList.Add(entry);
                    }
                }

                // Get the nyoom dictionary for index lookups.
                if (!gridIndices.TryGetValue(visMask, out var gridDict))
                {
                    gridDict = new Dictionary<GridChunkLocation, int>(32);
                    gridIndices[visMask] = gridDict;
                }

                _mapManager.FindGridsIntersectingEnumerator(mapId, new Box2(viewPos - range, viewPos + range), out var gridEnumerator, true);
                while (gridEnumerator.MoveNext(out var mapGrid))
                {
                    var localPos = transformQuery.GetComponent(mapGrid.GridEntityId).InvWorldMatrix.Transform(viewPos);

                    var gridChunkEnumerator =
                        new ChunkIndicesEnumerator(localPos, range, ChunkSize);

                    while (gridChunkEnumerator.MoveNext(out var gridChunkIndices))
                    {
                        var chunkLocation = new GridChunkLocation(mapGrid.Index, gridChunkIndices.Value);
                        var entry = (visMask, chunkLocation);

                        if (gridDict.TryGetValue(chunkLocation, out var indexOf))
                        {
                            playerChunks[i].Add(indexOf);
                        }
                        else
                        {
                            playerChunks[i].Add(chunkList.Count);
                            gridDict.Add(chunkLocation, chunkList.Count);
                            chunkList.Add(entry);
                        }
                    }
                }
                }

            _uidSetPool.Return(viewers);
        }

        return (chunkList, playerChunks, viewerEntities);
    }

    public Dictionary<EntityUid, MetaDataComponent>? CalculateChunk(IChunkIndexLocation chunkLocation, uint visMask, EntityQuery<TransformComponent> transform, EntityQuery<MetaDataComponent> metadata)
    {
        var chunk = chunkLocation switch
        {
            GridChunkLocation gridChunkLocation => _entityPvsCollection.TryGetChunk(gridChunkLocation.GridId,
                gridChunkLocation.ChunkIndices, out var gridChunk)
                ? gridChunk
                : null,
            MapChunkLocation mapChunkLocation => _entityPvsCollection.TryGetChunk(mapChunkLocation.MapId,
                mapChunkLocation.ChunkIndices, out var mapChunk)
                ? mapChunk
                : null
        };
        if (chunk == null) return null;
        var chunkSet = _chunkCachePool.Get();
        foreach (var uid in chunk)
        {
            AddToChunkSetRecursively(in uid, visMask, chunkSet, transform, metadata);
        }

        return chunkSet;
    }

    public void ReturnToPool(Dictionary<EntityUid, MetaDataComponent>?[] chunkCache, HashSet<int>[] playerChunks)
    {
        foreach (var chunk in chunkCache)
        {
            if(chunk == null) continue;
            _chunkCachePool.Return(chunk);
        }

        foreach (var playerChunk in playerChunks)
        {
            _playerChunkPool.Return(playerChunk);
        }
    }

    private bool AddToChunkSetRecursively(in EntityUid uid, uint visMask, Dictionary<EntityUid, MetaDataComponent> set, EntityQuery<TransformComponent> transform,
        EntityQuery<MetaDataComponent> metadata)
    {
        //are we valid?
        //sometimes uids gets added without being valid YET (looking at you mapmanager) (mapcreate & gridcreated fire before the uids becomes valid)
        if (!uid.IsValid()) return false;

        if (set.ContainsKey(uid)) return false;

        var mComp = metadata.GetComponent(uid);

        // TODO: Don't need to know about parents so no longer need to use bool for this method.
        // If the eye is missing ANY layer this entity or any of its parents belongs to, it is considered invisible.
        if ((visMask & mComp.VisibilityMask) != mComp.VisibilityMask)
            return false;

        var parent = transform.GetComponent(uid).ParentUid;

        if (parent.IsValid() && //is it not a worldentity?
            !set.ContainsKey(parent) && //was the parent not yet added to toSend?
            !AddToChunkSetRecursively(in parent, visMask, set, transform, metadata)) //did we just fail to add the parent?
            return false; //we failed? suppose we dont get added either

        //todo paul i want it to crash here if it gets added double bc that shouldnt happen and will add alot of unneeded cycles, make this a simpl assignment at some point maybe idk
        set.Add(uid, mComp);
        return true;
    }

    public (List<EntityState>? updates, List<EntityUid>? deletions) CalculateEntityStates(IPlayerSession session,
        GameTick fromTick, GameTick toTick, Dictionary<EntityUid, MetaDataComponent>?[] chunkCache, HashSet<int> chunkIndices, EntityQuery<MetaDataComponent> mQuery, EntityUid[] viewerEntities)
    {
        DebugTools.Assert(session.Status == SessionStatus.InGame);
        var newEntitiesSent = 0;
        var entitiesSent = 0;
        var playerVisibleSet = _playerVisibleSets[session];
        var visibleEnts = _visSetPool.Get();
        var seenSet = _playerSeenSets[session];
        var deletions = _entityPvsCollection.GetDeletedIndices(fromTick);

        var globalEnumerator = _entityPvsCollection.GlobalOverridesEnumerator;
        while (globalEnumerator.MoveNext())
        {
            var uid = globalEnumerator.Current;
            TryAddToVisibleEnts(in uid, seenSet, playerVisibleSet, visibleEnts, fromTick, ref newEntitiesSent,
                ref entitiesSent, mQuery, dontSkip: true);
        }
        globalEnumerator.Dispose();

        var localEnumerator = _entityPvsCollection.GetElementsForSession(session);
        while (localEnumerator.MoveNext())
        {
            var uid = localEnumerator.Current;
            TryAddToVisibleEnts(in uid, seenSet, playerVisibleSet, visibleEnts, fromTick, ref newEntitiesSent,
                ref entitiesSent, mQuery, dontSkip: true);
        }
        localEnumerator.Dispose();

        foreach (var viewerEntity in viewerEntities)
        {
            TryAddToVisibleEnts(in viewerEntity, seenSet, playerVisibleSet, visibleEnts, fromTick, ref newEntitiesSent,
                ref entitiesSent, mQuery, dontSkip: true);
        }

        foreach (var i in chunkIndices)
        {
            var chunk = chunkCache[i];
            if(chunk == null) continue;
            foreach (var (uid, metadata) in chunk)
            {
                TryAddToVisibleEnts(in uid, seenSet, playerVisibleSet, visibleEnts, fromTick, ref newEntitiesSent,
                    ref entitiesSent, mQuery, metadata);

            }
        }

        var entityStates = new List<EntityState>();

        foreach (var (entityUid, visiblity) in visibleEnts)
        {
            if (visiblity == PVSEntityVisiblity.StayedUnchanged)
                continue;

            var @new = visiblity == PVSEntityVisiblity.Entered;

            var state = GetEntityState(session, entityUid, @new ? GameTick.Zero : fromTick);

            //this entity is not new & nothing changed
            if(!@new && state.Empty) continue;

            entityStates.Add(state);
        }

        foreach (var (entityUid, _) in playerVisibleSet)
        {
            // it was deleted, so we dont need to exit pvs
            if(deletions.Contains(entityUid)) continue;

            //TODO: HACK: somehow an entity left the view, transform does not exist (deleted?), but was not in the
            // deleted list. This seems to happen with the map entity on round restart.
            if (!EntityManager.EntityExists(entityUid))
                continue;

            entityStates.Add(new EntityState(entityUid, new NetListAsArray<ComponentChange>(new []
            {
                ComponentChange.Changed(_stateManager.TransformNetId, new TransformComponent.TransformComponentState(Vector2.Zero, Angle.Zero, EntityUid.Invalid, false, false)),
            }), true));
        }

        _playerVisibleSets[session] = visibleEnts;
        _visSetPool.Return(playerVisibleSet);

        if (deletions.Count == 0) deletions = default;
        if (entityStates.Count == 0) entityStates = default;
        return (entityStates, deletions);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void TryAddToVisibleEnts(
        in EntityUid uid,
        HashSet<EntityUid> seenSet,
        Dictionary<EntityUid, PVSEntityVisiblity> previousVisibleEnts,
        Dictionary<EntityUid, PVSEntityVisiblity> toSend,
        GameTick fromTick,
        ref int newEntitiesSent,
        ref int totalEnteredEntities,
        EntityQuery<MetaDataComponent> metadataQuery,
        MetaDataComponent? metaDataComponent = null,
        bool dontSkip = false)
    {
        //are we valid?
        //sometimes uids gets added without being valid YET (looking at you mapmanager) (mapcreate & gridcreated fire before the uids becomes valid)
        if (!uid.IsValid()) return;

        //did we already get added?
        //todo paul can this happen?
        if (toSend.ContainsKey(uid)) return;

        //are we new?
        var @new = !seenSet.Contains(uid);
        var entered = @new | !previousVisibleEnts.Remove(uid);

        if (entered)
        {
            if (!dontSkip && totalEnteredEntities >= _entityBudget)
                return;

            totalEnteredEntities++;
        }

        if (@new)
        {
            //we just entered pvs, do we still have enough budget to send us?
            if(!dontSkip && newEntitiesSent >= _newEntityBudget)
                return;

            newEntitiesSent++;
            seenSet.Add(uid);
        }

        if (entered)
        {
            toSend.Add(uid, PVSEntityVisiblity.Entered);
            return;
        }

        metaDataComponent ??= metadataQuery.GetComponent(uid);

        if (metaDataComponent.EntityLastModifiedTick < fromTick)
        {
            //entity has been sent before and hasnt been updated since
            toSend.Add(uid, PVSEntityVisiblity.StayedUnchanged);
            return;
        }

        //add us
        toSend.Add(uid, PVSEntityVisiblity.StayedChanged);
        return;
    }

    /// <summary>
    ///     Gets all entity states that have been modified after and including the provided tick.
    /// </summary>
    public (List<EntityState>? updates, List<EntityUid>? deletions) GetAllEntityStates(ICommonSession player, GameTick fromTick, GameTick toTick)
    {
        var deletions = _entityPvsCollection.GetDeletedIndices(fromTick);
        // no point sending an empty collection
        if (deletions.Count == 0) deletions = default;

        var stateEntities = new List<EntityState>();
        var seenEnts = new HashSet<EntityUid>();
        var slowPath = false;
        var metadataQuery = EntityManager.GetEntityQuery<MetaDataComponent>();

        for (var i = fromTick.Value; i <= toTick.Value; i++)
        {
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
                if (!metadataQuery.TryGetComponent(uid, out MetaDataComponent? md)) continue;

                DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized);

                if (md.EntityLastModifiedTick >= fromTick)
                    stateEntities.Add(GetEntityState(player, uid, GameTick.Zero));
            }

            foreach (var uid in dirty)
            {
                DebugTools.Assert(!add.Contains(uid));

                if (!seenEnts.Add(uid)) continue;
                if (!metadataQuery.TryGetComponent(uid, out MetaDataComponent? md)) continue;

                DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized);

                if (md.EntityLastModifiedTick >= fromTick)
                    stateEntities.Add(GetEntityState(player, uid, fromTick));
            }
        }

        if (!slowPath)
        {
            if (stateEntities.Count == 0) stateEntities = default;

            return (stateEntities, deletions);
        }

        stateEntities = new List<EntityState>(EntityManager.EntityCount);

        // This is the same as iterating every existing entity.
        foreach (var md in EntityManager.EntityQuery<MetaDataComponent>(true))
        {
            DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized);

            if (md.EntityLastModifiedTick >= fromTick)
                stateEntities.Add(GetEntityState(player, md.Owner, fromTick));
        }

        // no point sending an empty collection
        if (stateEntities.Count == 0) stateEntities = default;

        return (stateEntities, deletions);
    }

    /// <summary>
    /// Generates a network entity state for the given entity.
    /// </summary>
    /// <param name="player">The player to generate this state for.</param>
    /// <param name="entityUid">Uid of the entity to generate the state from.</param>
    /// <param name="fromTick">Only provide delta changes from this tick.</param>
    /// <returns>New entity State for the given entity.</returns>
    private EntityState GetEntityState(ICommonSession player, EntityUid entityUid, GameTick fromTick)
    {
        var bus = EntityManager.EventBus;
        var changed = new List<ComponentChange>();

        foreach (var (netId, component) in EntityManager.GetNetComponents(entityUid))
        {
            DebugTools.Assert(component.Initialized);

            // NOTE: When LastModifiedTick or CreationTick are 0 it means that the relevant data is
            // "not different from entity creation".
            // i.e. when the client spawns the entity and loads the entity prototype,
            // the data it deserializes from the prototype SHOULD be equal
            // to what the component state / ComponentChange would send.
            // As such, we can avoid sending this data in this case since the client "already has it".

            DebugTools.Assert(component.LastModifiedTick >= component.CreationTick);

            if (!EntityManager.CanGetComponentState(bus, component, player))
                continue;

            if (component.CreationTick != GameTick.Zero && component.CreationTick >= fromTick && !component.Deleted)
            {
                ComponentState? state = null;
                if (component.NetSyncEnabled && component.LastModifiedTick != GameTick.Zero &&
                    component.LastModifiedTick >= fromTick)
                    state = EntityManager.GetComponentState(bus, component);

                // Can't be null since it's returned by GetNetComponents
                // ReSharper disable once PossibleInvalidOperationException
                changed.Add(ComponentChange.Added(netId, state));
            }
            else if (component.NetSyncEnabled && component.LastModifiedTick != GameTick.Zero &&
                     component.LastModifiedTick >= fromTick)
            {
                changed.Add(ComponentChange.Changed(netId, EntityManager.GetComponentState(bus, component)));
            }
        }

        foreach (var netId in ((IServerEntityManager)EntityManager).GetDeletedComponents(entityUid, fromTick))
        {
            changed.Add(ComponentChange.Removed(netId));
        }

        return new EntityState(entityUid, changed.ToArray());
    }

    private HashSet<EntityUid> GetSessionViewers(ICommonSession session)
    {
        var viewers = _uidSetPool.Get();
        if (session.Status != SessionStatus.InGame)
            return viewers;

        if (session.AttachedEntity != null)
            viewers.Add(session.AttachedEntity.Value);

        // This is awful, but we're not gonna add the list of view subscriptions to common session.
        if (session is IPlayerSession playerSession)
        {
            foreach (var uid in playerSession.ViewSubscriptions)
            {
                viewers.Add(uid);
            }
        }

        return viewers;
    }

    // Read Safe
    private (Vector2 worldPos, float range, MapId mapId) CalcViewBounds(in EntityUid euid, EntityQuery<TransformComponent> transformQuery)
    {
        var xform = transformQuery.GetComponent(euid);
        return (xform.WorldPosition, _viewSize / 2f, xform.MapID);
    }

    public sealed class SetPolicy<T> : PooledObjectPolicy<HashSet<T>>
    {
        public override HashSet<T> Create()
        {
            return new HashSet<T>();
        }

        public override bool Return(HashSet<T> obj)
        {
            obj.Clear();
            return true;
        }
    }

    public sealed class DictPolicy<T1, T2> : PooledObjectPolicy<Dictionary<T1, T2>> where T1 : notnull
    {
        public override Dictionary<T1, T2> Create()
        {
            return new Dictionary<T1, T2>();
        }

        public override bool Return(Dictionary<T1, T2> obj)
        {
            obj.Clear();
            return true;
        }
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
