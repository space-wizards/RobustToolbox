using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Prometheus;
using Robust.Server.GameObjects.Components;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Shared.GameObjects.Components.Transform.TransformComponent;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ServerEntityManager : EntityManager, IServerEntityManagerInternal
    {
        private static readonly Gauge EntitiesCount = Metrics.CreateGauge(
            "robust_entities_count",
            "Amount of alive entities.");

        private const float MinimumMotionForMovers = 1 / 128f;

        #region IEntityManager Members

        [Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Shared.IoC.Dependency] private readonly IPauseManager _pauseManager = default!;
        [Shared.IoC.Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private float? _maxUpdateRangeCache;

        public float MaxUpdateRange => _maxUpdateRangeCache
            ??= _configurationManager.GetCVar<float>("net.maxupdaterange");

        private int _nextServerEntityUid = (int) EntityUid.FirstUid;

        private Dictionary<EntityUid, GameTick>? _lastSeen;
        private HashSet<EntityUid>? _checkedEnts;
        private List<EntityState>? _entityStates;
        private HashSet<EntityUid>? _neededEnts;
        private SortedSet<EntityUid>? _seenMovers2;
        private HashSet<IEntity>? _relatives;
        private Box2 _viewbox;

        private readonly List<(GameTick tick, EntityUid uid)> _deletionHistory = new List<(GameTick, EntityUid)>();


        public override void Update()
        {
            base.Update();
            _maxUpdateRangeCache = null;
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string? prototypeName)
        {
            return CreateEntityServer(prototypeName);
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string? prototypeName, GridCoordinates coordinates)
        {
            var newEntity = CreateEntityServer(prototypeName);
            if (coordinates.GridID != GridId.Invalid)
            {
                var gridEntityId = _mapManager.GetGrid(coordinates.GridID).GridEntityId;
                newEntity.Transform.AttachParent(GetEntity(gridEntityId));
                newEntity.Transform.LocalPosition = coordinates.Position;
            }

            return newEntity;
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates)
        {
            var newEntity = CreateEntityServer(prototypeName);
            newEntity.Transform.AttachParent(_mapManager.GetMapEntity(coordinates.MapId));
            newEntity.Transform.WorldPosition = coordinates.Position;
            return newEntity;
        }

        private Entity CreateEntityServer(string? prototypeName)
        {
            var entity = CreateEntity(prototypeName);

            if (prototypeName != null)
            {
                var prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);

                // At this point in time, all data configure on the entity *should* be purely from the prototype.
                // As such, we can reset the modified ticks to Zero,
                // which indicates "not different from client's own deserialization".
                // So the initial data for the component or even the creation doesn't have to be sent over the wire.
                foreach (var component in ComponentManager.GetNetComponents(entity.Uid))
                {
                    // Make sure to ONLY get components that are defined in the prototype.
                    // Others could be instantiated directly by AddComponent (e.g. ContainerManager).
                    // And those aren't guaranteed to exist on the client, so don't clear them.
                    if (prototype.Components.ContainsKey(component.Name))
                    {
                        ((Component) component).ClearTicks();
                    }
                }
            }

            return entity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntity(string? protoName, GridCoordinates coordinates)
        {
            if (coordinates.GridID == GridId.Invalid)
                throw new InvalidOperationException($"Tried to spawn entity {protoName} onto invalid grid.");

            var entity = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) entity);
            var grid = _mapManager.GetGrid(coordinates.GridID);
            if (_pauseManager.IsMapInitialized(grid.ParentMapId))
            {
                entity.RunMapInit();
            }

            return entity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntity(string? protoName, MapCoordinates coordinates)
        {
            var entity = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) entity);
            return entity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntityNoMapInit(string? protoName, GridCoordinates coordinates)
        {
            var newEnt = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) newEnt);
            return newEnt;
        }

        /// <inheritdoc />
        public List<EntityState>? GetEntityStates(GameTick fromTick)
        {
            var stateEntities = new List<EntityState>();
            foreach (var entity in AllEntities)
            {
                if (entity.Deleted)
                {
                    continue;
                }

                DebugTools.Assert(entity.Initialized);

                if (entity.LastModifiedTick <= fromTick)
                    continue;

                stateEntities.Add(GetEntityState(ComponentManager, entity.Uid, fromTick));
            }

            // no point sending an empty collection
            return stateEntities.Count == 0 ? default : stateEntities;
        }

        private readonly Dictionary<IPlayerSession, SortedSet<EntityUid>> _seenMovers
            = new Dictionary<IPlayerSession, SortedSet<EntityUid>>();

        // Is thread safe.
        private SortedSet<EntityUid> GetSeenMovers(IPlayerSession player)
        {
            lock (_seenMovers)
            {
                return GetSeenMoversUnlocked(player);
            }
        }

        private SortedSet<EntityUid> GetSeenMoversUnlocked(IPlayerSession player)
        {
            if (!_seenMovers.TryGetValue(player, out var movers))
            {
                movers = new SortedSet<EntityUid>();
                _seenMovers.Add(player, movers);
            }

            return movers;
        }

        private void AddToSeenMovers(IPlayerSession player, EntityUid entityUid)
        {
            var movers = GetSeenMoversUnlocked(player);

            movers.Add(entityUid);
        }

        private readonly Dictionary<IPlayerSession, Dictionary<EntityUid, GameTick>> _playerLastSeen
            = new Dictionary<IPlayerSession, Dictionary<EntityUid, GameTick>>();

        private static readonly Vector2 Vector2NaN = new Vector2(float.NaN, float.NaN);

        private Dictionary<EntityUid, GameTick> GetLastSeen(IPlayerSession player)
        {
            lock (_playerLastSeen)
            {
                if (!_playerLastSeen.TryGetValue(player, out var lastSeen))
                {
                    lastSeen = new Dictionary<EntityUid, GameTick>();
                    _playerLastSeen.Add(player, lastSeen);
                }

                return lastSeen;
            }
        }

        private static GameTick GetLastSeenTick(Dictionary<EntityUid, GameTick> lastSeen, EntityUid uid)
        {
            if (!lastSeen.TryGetValue(uid, out var tick))
            {
                tick = GameTick.First;
            }

            return tick;
        }

        private static GameTick UpdateLastSeenTick(Dictionary<EntityUid, GameTick> lastSeen, EntityUid uid, GameTick newTick)
        {
            if (!lastSeen.TryGetValue(uid, out var oldTick))
            {
                oldTick = GameTick.First;
            }

            lastSeen[uid] = newTick;

            return oldTick;
        }

        private IEnumerable<EntityUid> GetLastSeenOn(Dictionary<EntityUid, GameTick> lastSeen, GameTick fromTick)
        {
            foreach (var (uid, tick) in lastSeen)
            {
                if (tick == fromTick)
                {
                    yield return uid;
                }
            }
        }

        private static void ClearLastSeenTick(Dictionary<EntityUid, GameTick> lastSeen, EntityUid uid)
        {
            lastSeen.Remove(uid);
        }

        public void DropPlayerState(IPlayerSession player)
        {
            lock (_playerLastSeen)
            {
                _playerLastSeen.Remove(player);
            }
        }

        private static void IncludeRelatives(IEnumerable<IEntity> children, HashSet<IEntity> set)
        {
            foreach (var child in children)
            {
                var ent = child!;

                while (ent != null && !ent.Deleted)
                {
                    if (set.Add(ent))
                    {
                        AddContainedRecursive(ent, set);

                        ent = ent.Transform.Parent?.Owner!;
                    }
                    else
                    {
                        // Already processed this entity once.
                        break;
                    }
                }
            }
        }

        private static void AddContainedRecursive(IEntity ent, HashSet<IEntity> set)
        {
            if (!ent.TryGetComponent(out ContainerManagerComponent? contMgr))
            {
                return;
            }

            foreach (var container in contMgr.GetAllContainers())
            {
                // Manual for loop to cut out allocations.
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < container.ContainedEntities.Count; i++)
                {
                    var contEnt = container.ContainedEntities[i];
                    set.Add(contEnt);
                    AddContainedRecursive(contEnt, set);
                }
            }
        }

        private class PlayerSeenEntityStatesResources
        {
            public readonly HashSet<EntityUid> IncludedEnts = new HashSet<EntityUid>();

            public readonly List<EntityState> EntityStates = new List<EntityState>();

            public readonly HashSet<EntityUid> NeededEnts = new HashSet<EntityUid>();

            public readonly HashSet<IEntity> Relatives = new HashSet<IEntity>();
        }

        private readonly ThreadLocal<PlayerSeenEntityStatesResources> _playerSeenEntityStatesResources
            = new ThreadLocal<PlayerSeenEntityStatesResources>(() => new PlayerSeenEntityStatesResources());

        /// <inheritdoc />
        public List<EntityState>? UpdatePlayerSeenEntityStates(GameTick fromTick, IPlayerSession player, float range)
        {
            var playerEnt = player.AttachedEntity;
            if (playerEnt == null)
            {
                // super-observer?
                return GetEntityStates(fromTick);
            }

            var position = playerEnt.Transform.WorldPosition;
            _viewbox = new Box2(position, position).Enlarged(MaxUpdateRange);

            _seenMovers2 = GetSeenMovers(player);
            _lastSeen = GetLastSeen(player);

            var pseStateRes = _playerSeenEntityStatesResources.Value!;
            _checkedEnts = pseStateRes.IncludedEnts;
            _entityStates = pseStateRes.EntityStates;
            _neededEnts = pseStateRes.NeededEnts;
            _relatives = pseStateRes.Relatives;

            _checkedEnts.Clear();
            _entityStates.Clear();
            _neededEnts.Clear();
            _relatives.Clear();

            ProcessSeenMovers(fromTick);

            // scan pvs box and include children and parents recursively
            IncludeRelatives(GetEntitiesInRange(playerEnt.Transform.MapID, position, range, true), _relatives);

            // Exclude any entities that are currently invisible to the player.
            ExcludeInvisible(_relatives, player.VisibilityMask);

            // Always send updates for all grid and map entities.
            // If we don't, the client-side game state manager WILL blow up.
            // TODO: Make map manager netcode aware of PVS to avoid the need for this workaround.
            IncludeMapCriticalEntities(_relatives);

            ProcessRelatives(playerEnt.Uid);
            ProcessLastSeen(fromTick, new GameTick(fromTick.Value - 1), playerEnt.Uid);
            ProcessNeededEntities(fromTick);

            // help the client out
            _entityStates.Sort((a, b) => a.Uid.CompareTo(b.Uid));

#if DEBUG_NULL_ENTITY_STATES
            foreach ( var state in entityStates ) {
                if (state.ComponentStates == null)
                {
                    throw new NotImplementedException("Shouldn't send null states.");
                }
            }
#endif

            // no point sending an empty collection
            return _entityStates.Count == 0 ? default : _entityStates;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessSeenMovers(GameTick fromTick)
        {
            foreach (var uid in _seenMovers2.ToList())
            {
                if (!TryGetEntity(uid, out IEntity? entity) || entity.Deleted)
                {
                    _seenMovers2!.Remove(uid);
                    continue;
                }

                if (entity.TryGetComponent(out ICollidableComponent? body))
                {
                    if (body.LinearVelocity.EqualsApprox(Vector2.Zero, MinimumMotionForMovers))
                    {
                        // parent is moving
                        if (AnyParentInSet(uid, _seenMovers2!))
                            continue;

                        // has children spinning
                        if (MathF.Abs(body.AngularVelocity) > 0 && entity.TryGetComponent(out TransformComponent? txf) &&
                            txf.ChildCount > 0)
                            continue;

                        _seenMovers2!.Remove(uid);
                    }
                }

                var state = GetEntityState(ComponentManager, uid, fromTick);

                if (_checkedEnts!.Add(uid))
                {
                    _entityStates!.Add(state);

                    // Mover did not change or can be seen
                    if (state.ComponentStates == null || _viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                        continue;

                    int index = Array.FindIndex(state.ComponentStates, componentState => componentState is TransformComponentState);

                    if (index == -1)
                        continue;

                    // Mover changed positional data and can't be seen
                    var oldState = (TransformComponentState) state.ComponentStates[index];
                    var newState = new TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID);

                    state.ComponentStates[index] = newState;
                    _seenMovers2!.Remove(uid);
                    ClearLastSeenTick(_lastSeen!, uid);

                    _checkedEnts.Add(uid);

                    var needed = oldState.ParentID;

                    // either no parent attached or parent already included
                    if (!needed.IsValid() || _checkedEnts.Contains(needed))
                        continue;

                    if (GetLastSeenTick(_lastSeen!, needed) == GameTick.Zero)
                        _neededEnts!.Add(needed);
                }
                else
                {
                    // mover already added?
                    if (_viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                        continue;

                    // mover can't be seen
                    var oldState = (TransformComponentState) entity.Transform.GetComponentState();

                    var componentsChanged = new ComponentChanged[]
                    {
                        new ComponentChanged(false, NetIDs.TRANSFORM, "Transform")
                    };

                    var componentStates = new ComponentState[]
                    {
                        new TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID)
                    };

                    _entityStates!.Add(new EntityState(uid, componentsChanged, componentStates));

                    _seenMovers2!.Remove(uid);
                    ClearLastSeenTick(_lastSeen!, uid);
                    _checkedEnts.Add(uid);

                    var needed = oldState.ParentID;

                    // No parent attached or parent already included
                    if (!needed.IsValid() || _checkedEnts.Contains(needed))
                        continue;

                    if (GetLastSeenTick(_lastSeen!, needed) == GameTick.Zero)
                        _neededEnts!.Add(needed);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessRelatives(EntityUid playerUid)
        {
            foreach (var entity in _relatives!)
            {
                DebugTools.Assert(entity.Initialized && !entity.Deleted);

                var uid = entity.Uid;
                var lastSeen = UpdateLastSeenTick(_lastSeen!, uid, CurrentTick);

                DebugTools.Assert(lastSeen != CurrentTick);

                // If already checked or not changed, continue
                if (_checkedEnts!.Contains(uid) || entity.LastModifiedTick <= lastSeen)
                    continue;

                // should this be lastSeen or fromTick?
                // I don't know Q, should it?
                var entityState = GetEntityState(ComponentManager, uid, lastSeen);

                _checkedEnts.Add(uid);

                // no changes
                if (entityState.ComponentStates == null)
                    continue;

                _entityStates!.Add(entityState);

                if (uid == playerUid)
                    continue;

                // Continue if entity has no physics
                if (!entity.TryGetComponent(out ICollidableComponent? body))
                    continue;

                if (body.LinearVelocity.EqualsApprox(Vector2.Zero, MinimumMotionForMovers))
                {
                    _seenMovers2!.Remove(uid);
                }
                else
                {
                    _seenMovers2!.Add(uid);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessLastSeen(GameTick fromTick, GameTick priorTick, EntityUid playerUid)
        {
            foreach (var uid in GetLastSeenOn(_lastSeen!, priorTick))
            {
                if (_checkedEnts!.Contains(uid))
                    continue;

                if (uid == playerUid)
                    continue;

                // TODO: remove from states list being sent?
                if (!TryGetEntity(uid, out IEntity? entity) || entity.Deleted)
                    continue;

                // can be seen
                if (_viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                    continue;

                var state = GetEntityState(ComponentManager, uid, fromTick);

                // nothing changed
                if (state.ComponentStates == null)
                    continue;

                _checkedEnts.Add(uid);
                _entityStates!.Add(state);

                _seenMovers2!.Remove(uid);
                ClearLastSeenTick(_lastSeen!, uid);

                int index = Array.FindIndex(state.ComponentStates, componentState => componentState is TransformComponentState);

                // no transform changes
                if (index == -1)
                    continue;

                var oldState = (TransformComponentState) state.ComponentStates[index];
                var newState = new TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID);
                state.ComponentStates[index] = newState;

                var needed = oldState.ParentID;

                // don't need to include parent or already included
                if (!needed.IsValid() || _checkedEnts.Contains(needed))
                    continue;

                if (GetLastSeenTick(_lastSeen!, needed) == GameTick.First)
                    _neededEnts!.Add(needed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessNeededEntities(GameTick fromTick)
        {
            HashSet<EntityUid> neededEnts;
            do
            {
                var moreNeededEnts = new HashSet<EntityUid>();

                foreach (var uid in moreNeededEnts)
                {
                    if (_checkedEnts!.Contains(uid))
                        continue;

                    var entity = GetEntity(uid);
                    var state = GetEntityState(ComponentManager, uid, fromTick);

                    // no states or should already be seen
                    if (state.ComponentStates == null || _viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                        continue;

                    _checkedEnts.Add(uid);
                    _entityStates!.Add(state);

                    var idx = Array.FindIndex(state.ComponentStates,
                                              componentState => componentState is TransformComponentState);

                    // no transform state
                    if (idx == -1)
                        continue;

                    var oldState = (TransformComponentState) state.ComponentStates[idx];
                    var newState = new TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID);

                    state.ComponentStates[idx] = newState;
                    _seenMovers2!.Remove(uid);

                    ClearLastSeenTick(_lastSeen!, uid);
                    var needed = oldState.ParentID;

                    // done here
                    if (!needed.IsValid() || _checkedEnts.Contains(needed))
                        continue;

                    // check if further needed
                    if (!_checkedEnts.Contains(uid) && GetLastSeenTick(_lastSeen!, needed) == GameTick.Zero)
                        moreNeededEnts.Add(needed);
                }

                neededEnts = moreNeededEnts;
            } while (neededEnts.Count > 0);
        }

        public override void DeleteEntity(IEntity e)
        {
            base.DeleteEntity(e);

            _deletionHistory.Add((CurrentTick, e.Uid));
        }

        public List<EntityUid>? GetDeletedEntities(GameTick fromTick)
        {
            var list = new List<EntityUid>();
            foreach (var (tick, id) in _deletionHistory)
            {
                if (tick >= fromTick)
                {
                    list.Add(id);
                }
            }

            // no point sending an empty collection
            return list.Count == 0 ? default : list;
        }

        public void CullDeletionHistory(GameTick toTick)
        {
            _deletionHistory.RemoveAll(hist => hist.tick <= toTick);
        }

        public override bool UpdateEntityTree(IEntity entity)
        {
            var currentTick = CurrentTick;
            var updated = base.UpdateEntityTree(entity);

            if (entity.Deleted
                || !entity.Initialized
                || !Entities.ContainsKey(entity.Uid))
            {
                return updated;
            }

            DebugTools.Assert(entity.Transform.Initialized);

            // note: updated can be false even if something moved a bit

            foreach (var (player, lastSeen) in _playerLastSeen)
            {
                var playerEnt = player.AttachedEntity;
                if (playerEnt == null)
                {
                    // player has no entity, gaf?
                    continue;
                }

                var playerUid = playerEnt.Uid;

                var entityUid = entity.Uid;

                if (entityUid == playerUid)
                {
                    continue;
                }

                if (!lastSeen.TryGetValue(playerUid, out var playerTick))
                {
                    // player can't "see" itself, gaf?
                    continue;
                }

                var playerPos = playerEnt.Transform.WorldPosition;

                var viewbox = new Box2(playerPos, playerPos).Enlarged(MaxUpdateRange);

                if (!lastSeen.TryGetValue(entityUid, out var tick))
                {
                    // never saw it other than first tick or was cleared
                    if (!AnyParentMoving(player, entityUid))
                    {
                        continue;
                    }
                }

                if (tick >= currentTick)
                {
                    // currently seeing it
                    continue;
                }
                // saw it previously

                // player can't see it now
                if (!viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                {
                    var addToMovers = false;
                    if (entity.Transform.LastModifiedTick >= currentTick)
                    {
                        addToMovers = true;
                    }
                    else if (entity.TryGetComponent(out ICollidableComponent? physics)
                             && physics.LastModifiedTick >= currentTick)
                    {
                        addToMovers = true;
                    }

                    if (addToMovers)
                    {
                        AddToSeenMovers(player, entityUid);
                    }
                }
            }

            return updated;
        }

        private bool AnyParentMoving(IPlayerSession player, EntityUid entityUid)
        {
            var seenMovers = GetSeenMoversUnlocked(player);
            if (seenMovers == null)
            {
                return false;
            }

            return AnyParentInSet(entityUid, seenMovers);
        }

        private bool AnyParentInSet(EntityUid entityUid, SortedSet<EntityUid> set)
        {
            for (;;)
            {
                if (!TryGetEntity(entityUid, out var ent))
                {
                    return false;
                }

                var txf = ent.Transform;

                entityUid = txf.ParentUid;

                if (entityUid == EntityUid.Invalid)
                {
                    return false;
                }

                if (set.Contains(entityUid))
                {
                    return true;
                }
            }
        }

        #endregion IEntityManager Members

        IEntity IServerEntityManagerInternal.AllocEntity(string? prototypeName, EntityUid? uid)
        {
            return AllocEntity(prototypeName, uid);
        }

        protected override EntityUid GenerateEntityUid()
        {
            return new EntityUid(_nextServerEntityUid++);
        }

        void IServerEntityManagerInternal.FinishEntityLoad(IEntity entity, IEntityLoadContext? context)
        {
            LoadEntity((Entity) entity, context);
        }

        void IServerEntityManagerInternal.FinishEntityInitialization(IEntity entity)
        {
            InitializeEntity((Entity) entity);
        }

        void IServerEntityManagerInternal.FinishEntityStartup(IEntity entity)
        {
            StartEntity((Entity) entity);
        }

        /// <inheritdoc />
        public override void Startup()
        {
            base.Startup();
            EntitySystemManager.Initialize();
            Started = true;
        }

        /// <summary>
        /// Generates a network entity state for the given entity.
        /// </summary>
        /// <param name="compMan">ComponentManager that contains the components for the entity.</param>
        /// <param name="entityUid">Uid of the entity to generate the state from.</param>
        /// <param name="fromTick">Only provide delta changes from this tick.</param>
        /// <returns>New entity State for the given entity.</returns>
        private static EntityState GetEntityState(IComponentManager compMan, EntityUid entityUid, GameTick fromTick)
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
                    compStates.Add(comp.GetComponentState());

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


        private void IncludeMapCriticalEntities(HashSet<IEntity> set)
        {
            foreach (var mapId in _mapManager.GetAllMapIds())
            {
                if (_mapManager.HasMapEntity(mapId))
                {
                    set.Add(_mapManager.GetMapEntity(mapId));
                }
            }

            foreach (var grid in _mapManager.GetAllGrids())
            {
                if (grid.GridEntityId != EntityUid.Invalid)
                {
                    set.Add(GetEntity(grid.GridEntityId));
                }
            }
        }

        private void ExcludeInvisible(HashSet<IEntity> set, int visibilityMask)
        {
            foreach (var entity in set.ToArray())
            {
                if (!entity.TryGetComponent(out VisibilityComponent? visibility))
                    continue;

                if ((visibilityMask & visibility.Layer) == 0)
                    set.Remove(entity);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            EntitiesCount.Set(AllEntities.Count);
        }
    }
}
