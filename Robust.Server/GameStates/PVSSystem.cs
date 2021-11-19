using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;
using NetSerializer;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal partial class PVSSystem : EntitySystem
{
    [Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
    [Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
    [Shared.IoC.Dependency] private readonly IConfigurationManager _configManager = default!;

    /// <summary>
    /// Maximum number of pooled objects
    /// </summary>
    private const int MaxVisPoolSize = 1024;

    /// <summary>
    /// Starting number of entities that are in view
    /// </summary>
    private const int ViewSetCapacity = 256;

    /// <summary>
    /// Is view culling enabled, or will we send the whole map?
    /// </summary>
    public bool CullingEnabled { get; set; }

    /// <summary>
    /// Size of the side of the view bounds square.
    /// </summary>
    public float ViewSize { get; set; }

    /// <summary>
    /// All <see cref="EntityUid"/>s a <see cref="ICommonSession"/> saw last iteration.
    /// </summary>
    private readonly Dictionary<ICommonSession, HashSet<EntityUid>> _playerVisibleSets = new();


    private PVSCollection<EntityUid, IEntity> _entityPvsCollection = default!;
    private readonly Dictionary<Type, IPVSCollection> _pvsCollections = new();
    private readonly ObjectPool<HashSet<EntityUid>> _visSetPool
        = new DefaultObjectPool<HashSet<EntityUid>>(new VisSetPolicy(), MaxVisPoolSize);
    private readonly ObjectPool<HashSet<EntityUid>> _viewerEntsPool
        = new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), MaxVisPoolSize);

    public override void Initialize()
    {
        base.Initialize();

        _entityPvsCollection = RegisterPVSCollection<EntityUid, IEntity>(EntityManager.GetEntity);
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

        InitializeDirty();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _mapManager.MapCreated -= OnMapCreated;
        _mapManager.MapDestroyed -= OnMapDestroyed;
        _mapManager.OnGridCreated -= OnGridCreated;
        _mapManager.OnGridRemoved -= OnGridRemoved;
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        EntityManager.EntityDeleted -= OnEntityDeleted;

        _configManager.UnsubValueChanged(CVars.NetPVS, SetPvs);
        _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, OnViewsizeChanged);

        ShutdownDirty();
    }

    private void OnViewsizeChanged(float obj)
    {
        ViewSize = obj * 2;
    }

    private void SetPvs(bool value)
    {
        CullingEnabled = value;
    }

    //todo paul investigate
    public void Cleanup(IEnumerable<IPlayerSession> sessions)
    {
        CleanupDirty(sessions);
    }


    private void OnTransformInit(EntityUid uid, TransformComponent component, ComponentInit args)
    {
        _entityPvsCollection.AddIndex(uid, component.Coordinates);
    }

    public void CullDeletionHistory(GameTick oldestAck)
    {
        _entityPvsCollection.CullDeletionHistoryUntil(oldestAck);
    }

    #region PVSCollection methods to maybe make public someday:tm:

    private PVSCollection<TIndex, TElement> RegisterPVSCollection<TIndex, TElement>(Func<TIndex, TElement> getElementDelegate) where TIndex : IComparable<TIndex>, IEquatable<TIndex>
    {
        var collection = new PVSCollection<TIndex, TElement>(getElementDelegate);
        _pvsCollections.Add(typeof(TElement), collection);
        return collection;
    }

    private bool UnregisterPVSCollection<TIndex, TElement>(PVSCollection<TIndex, TElement> pvsCollection) where TIndex : IComparable<TIndex>, IEquatable<TIndex> =>
        _pvsCollections.Remove(typeof(TElement));

    private bool UnregisterPVSCollection<TElement>() => _pvsCollections.Remove(typeof(TElement));

    #endregion

    #region PVSCollection Event Updates

    private void OnEntityDeleted(object? sender, EntityUid e)
    {
        _entityPvsCollection.RemoveIndex(EntityManager.CurrentTick, e);
    }

    private void OnEntityMove(ref MoveEvent ev)
    {
        _entityPvsCollection.UpdateIndex(ev.Sender.Uid, ev.NewPosition);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            _playerVisibleSets.Add(e.Session, _visSetPool.Get());
            foreach (var (_, pvsCollection) in _pvsCollections)
            {
                pvsCollection.AddPlayer(e.Session);
            }
        }
        else if (e.NewStatus == SessionStatus.Disconnected)
        {
            _visSetPool.Return(_playerVisibleSets[e.Session]);
            _playerVisibleSets.Remove(e.Session);
            foreach (var (_, pvsCollection) in _pvsCollections)
            {
                pvsCollection.RemovePlayer(e.Session);
            }
        }
    }

    private void OnGridRemoved(MapId mapid, GridId gridid)
    {
        foreach (var (_, pvsCollection) in _pvsCollections)
        {
            pvsCollection.RemoveGrid(gridid);
        }
    }

    private void OnGridCreated(MapId mapid, GridId gridid)
    {
        foreach (var (_, pvsCollection) in _pvsCollections)
        {
            pvsCollection.AddGrid(gridid);
        }
    }

    private void OnMapDestroyed(object? sender, MapEventArgs e)
    {
        foreach (var (_, pvsCollection) in _pvsCollections)
        {
            pvsCollection.RemoveMap(e.Map);
        }
    }

    private void OnMapCreated(object? sender, MapEventArgs e)
    {
        foreach (var (_, pvsCollection) in _pvsCollections)
        {
            pvsCollection.AddMap(e.Map);
        }
    }

    #endregion

    public (List<EntityState>? updates, List<EntityUid>? deletions) CalculateEntityStates(ICommonSession session,
        GameTick fromTick, GameTick toTick)
    {
        DebugTools.Assert(session.Status == SessionStatus.InGame);
        _entityPvsCollection.Process();

        var deletions = _entityPvsCollection.GetDeletedIndices(fromTick);
        if (deletions.Count == 0) deletions = default;
        if (!CullingEnabled)
        {
            var allStates = GetAllEntityStates(session, fromTick, toTick);
            return (allStates, deletions);
        }

        var visibleEnts = _visSetPool.Get();
        foreach (var mapId in _mapManager.GetAllMapIds())
        {
            if (_mapManager.HasMapEntity(mapId))
            {
                visibleEnts.Add(_mapManager.GetMapEntityId(mapId));
            }
        }

        foreach (var grid in _mapManager.GetAllGrids())
        {
            if (grid.GridEntityId != EntityUid.Invalid)
            {
                visibleEnts.Add(grid.GridEntityId);
            }
        }

        var viewers = GetSessionViewers(session);

        foreach (var eyeEuid in viewers)
        {
            var (viewBox, mapId) = CalcViewBounds(in eyeEuid);

            uint visMask = 0;
            if (EntityManager.TryGetComponent<EyeComponent>(eyeEuid, out var eyeComp))
                visMask = eyeComp.VisibilityMask;

            var newUids = new HashSet<EntityUid>();
            _entityPvsCollection.GetElementsInViewport(_mapManager, viewBox, mapId, newUids);

            foreach (var entityUid in newUids)
            {
                RecursiveAdd(entityUid, visibleEnts, visMask);
            }
        }

        viewers.Clear();
        _viewerEntsPool.Return(viewers);

        List<EntityState>? entityStates = new List<EntityState>();
        if (visibleEnts.Count != 0)
        {
            foreach (var entityUid in visibleEnts)
            {
                entityStates.Add(GetEntityState(session, entityUid, fromTick));
            }
        }

        var culledEnts = _playerVisibleSets[session];
        culledEnts.ExceptWith(visibleEnts);
        foreach (var entityUid in culledEnts)
        {
            //create a state indicating it should be hidden
            entityStates.Add(new EntityState(entityUid, new NetListAsArray<ComponentChange>(), true));
        }

        _playerVisibleSets[session] = visibleEnts;
        _visSetPool.Return(culledEnts);

        return (entityStates, deletions);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private bool RecursiveAdd(EntityUid uid, HashSet<EntityUid> visSet, uint visMask)
    {
        // we are done, this ent has already been checked and is visible
        if (visSet.Contains(uid))
            return true;

        // if we are invisible, we are not going into the visSet, so don't worry about parents, and children are not going in
        if (EntityManager.TryGetComponent<VisibilityComponent>(uid, out var visComp))
        {
            if ((visMask & visComp.Layer) == 0)
                return false;
        }

        var xform = EntityManager.GetComponent<TransformComponent>(uid);

        var parentUid = xform.ParentUid;

        // this is the world entity, it is always visible
        if (!parentUid.IsValid())
        {
            visSet.Add(uid);
            return true;
        }

        // parent is already in the set
        if (visSet.Contains(parentUid))
        {
            visSet.Add(uid);
            return true;
        }

        // parent was not added, so we are not either
        if (!RecursiveAdd(parentUid, visSet, visMask))
            return false;

        // add us
        visSet.Add(uid);

        return true;
    }

    /// <summary>
    ///     Gets all entity states that have been modified after and including the provided tick.
    /// </summary>
    private List<EntityState>? GetAllEntityStates(ICommonSession player, GameTick fromTick, GameTick toTick)
    {
        List<EntityState> stateEntities;

        stateEntities = new List<EntityState>();
        var seenEnts = new HashSet<EntityUid>();
        var slowPath = false;

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
                if (!seenEnts.Add(uid) || !EntityManager.TryGetEntity(uid, out var entity) || entity.Deleted) continue;

                DebugTools.Assert(entity.Initialized);

                if (entity.LastModifiedTick >= fromTick)
                    stateEntities.Add(GetEntityState(player, entity.Uid, GameTick.Zero));
            }

            foreach (var uid in dirty)
            {
                DebugTools.Assert(!add.Contains(uid));

                if (!seenEnts.Add(uid) || !EntityManager.TryGetEntity(uid, out var entity) || entity.Deleted) continue;

                DebugTools.Assert(entity.Initialized);

                if (entity.LastModifiedTick >= fromTick)
                    stateEntities.Add(GetEntityState(player, entity.Uid, fromTick));
            }
        }

        if (!slowPath)
        {
            return stateEntities.Count == 0 ? default : stateEntities;
        }

        stateEntities = new List<EntityState>(EntityManager.EntityCount);

        foreach (var entity in EntityManager.GetEntities())
        {
            if (entity.Deleted)
            {
                continue;
            }

            DebugTools.Assert(entity.Initialized);

            if (entity.LastModifiedTick >= fromTick)
                stateEntities.Add(GetEntityState(player, entity.Uid, fromTick));
        }

        // no point sending an empty collection
        return stateEntities.Count == 0 ? default : stateEntities;
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

            if (component.CreationTick != GameTick.Zero && component.CreationTick >= fromTick && !component.Deleted)
            {
                ComponentState? state = null;
                if (component.NetSyncEnabled && component.LastModifiedTick != GameTick.Zero && component.LastModifiedTick >= fromTick)
                    state = EntityManager.GetComponentState(bus, component, player);

                // Can't be null since it's returned by GetNetComponents
                // ReSharper disable once PossibleInvalidOperationException
                changed.Add(ComponentChange.Added(netId, state));
            }
            else if (component.NetSyncEnabled && component.LastModifiedTick != GameTick.Zero && component.LastModifiedTick >= fromTick)
            {
                changed.Add(ComponentChange.Changed(netId, EntityManager.GetComponentState(bus, component, player)));
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
        var viewers = _viewerEntsPool.Get();
        if (session.Status != SessionStatus.InGame)
            return viewers;

        if(session.AttachedEntityUid.HasValue)
            viewers.Add(session.AttachedEntityUid.Value);

        // This is awful, but we're not gonna add the list of view subscriptions to common session.
        if (session is  IPlayerSession playerSession)
        {
            foreach (var uid in playerSession.ViewSubscriptions)
            {
                viewers.Add(uid);
            }
        }

        return viewers;
    }

    // Read Safe
    private (Box2 view, MapId mapId) CalcViewBounds(in EntityUid euid)
    {
        var xform = EntityManager.GetComponent<TransformComponent>(euid);

        var view = Box2.UnitCentered.Scale(ViewSize).Translated(xform.WorldPosition);
        var map = xform.MapID;

        return (view, map);
    }

    private sealed class VisSetPolicy : PooledObjectPolicy<HashSet<EntityUid>>
    {
        public override HashSet<EntityUid> Create()
        {
            return new(ViewSetCapacity);
        }

        public override bool Return(HashSet<EntityUid> obj)
        {
            // TODO: This clear can be pretty expensive so maybe make a custom datatype given we're swapping
            // 70 - 300 entities a tick? Or do we even need to clear given it's just value types?
            obj.Clear();
            return true;
        }
    }
}
