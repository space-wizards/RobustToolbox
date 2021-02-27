using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Prometheus;
using Robust.Server.Player;
using Robust.Server.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
            ??= _configurationManager.GetCVar(CVars.NetMaxUpdateRange);

        private int _nextServerEntityUid = (int) EntityUid.FirstUid;

        private readonly List<(GameTick tick, EntityUid uid)> _deletionHistory = new();

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
        public override IEntity CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates)
        {
            var newEntity = CreateEntityServer(prototypeName);

            if (TryGetEntity(coordinates.EntityId, out var entity))
            {
                newEntity.Transform.AttachParent(entity);
                newEntity.Transform.Coordinates = coordinates;
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
        public override IEntity SpawnEntity(string? protoName, EntityCoordinates coordinates)
        {
            if (!coordinates.IsValid(this))
                throw new InvalidOperationException($"Tried to spawn entity {protoName} on invalid coordinates {coordinates}.");

            var entity = CreateEntityUninitialized(protoName, coordinates);

            InitializeAndStartEntity((Entity) entity);

            if (_pauseManager.IsMapInitialized(coordinates.GetMapId(this)))
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
        public override IEntity SpawnEntityNoMapInit(string? protoName, EntityCoordinates coordinates)
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
            = new();

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
            = new();

        private static readonly Vector2 Vector2NaN = new(float.NaN, float.NaN);

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

        private static IEnumerable<EntityUid> GetLastSeenAfter(Dictionary<EntityUid, GameTick> lastSeen, GameTick fromTick)
        {
            foreach (var (uid, tick) in lastSeen)
            {
                if (tick > fromTick)
                {
                    yield return uid;
                }
            }
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

        private static void SetLastSeenTick(Dictionary<EntityUid, GameTick> lastSeen, EntityUid uid, GameTick tick)
        {
            lastSeen[uid] = tick;
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

        private void IncludeRelatives(IEnumerable<IEntity> children, HashSet<IEntity> set)
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
            public readonly HashSet<EntityUid> IncludedEnts = new();

            public readonly List<EntityState> EntityStates = new();

            public readonly HashSet<EntityUid> NeededEnts = new();

            public readonly HashSet<IEntity> Relatives = new();
        }

        private readonly ThreadLocal<PlayerSeenEntityStatesResources> _playerSeenEntityStatesResources
            = new(() => new PlayerSeenEntityStatesResources());

        /// <inheritdoc />
        public List<EntityState>? UpdatePlayerSeenEntityStates(GameTick fromTick, IPlayerSession player, float range)
        {
            var playerEnt = player.AttachedEntity;
            if (playerEnt == null)
            {
                // super-observer?
                return GetEntityStates(fromTick);
            }

            var playerUid = playerEnt.Uid;

            var transform = playerEnt.Transform;
            var position = transform.WorldPosition;
            var mapId = transform.MapID;
            var viewbox = new Box2(position, position).Enlarged(MaxUpdateRange);

            var seenMovers = GetSeenMovers(player);
            var lSeen = GetLastSeen(player);

            var pseStateRes = _playerSeenEntityStatesResources.Value!;
            var checkedEnts = pseStateRes.IncludedEnts;
            var entityStates = pseStateRes.EntityStates;
            var neededEnts = pseStateRes.NeededEnts;
            var relatives = pseStateRes.Relatives;
            checkedEnts.Clear();
            entityStates.Clear();
            neededEnts.Clear();
            relatives.Clear();

            foreach (var uid in seenMovers.ToList())
            {
                if (!TryGetEntity(uid, out var entity) || entity.Deleted)
                {
                    seenMovers.Remove(uid);
                    continue;
                }

                if (entity.TryGetComponent(out IPhysBody? body))
                {
                    if (body.LinearVelocity.EqualsApprox(Vector2.Zero, MinimumMotionForMovers))
                    {
                        if (AnyParentInSet(uid, seenMovers))
                        {
                            // parent is moving
                            continue;
                        }

                        if (MathF.Abs(body.AngularVelocity) > 0)
                        {
                            if (entity.TryGetComponent(out TransformComponent? txf) && txf.ChildCount > 0)
                            {
                                // has children spinning
                                continue;
                            }
                        }

                        seenMovers.Remove(uid);
                    }
                }

                var state = GetEntityState(ComponentManager, uid, fromTick);

                if (checkedEnts.Add(uid))
                {
                    entityStates.Add(state);

                    // mover did not change
                    if (state.ComponentStates != null)
                    {
                        // mover can be seen
                        if (!viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                        {
                            // mover changed and can't be seen
                            var idx = Array.FindIndex(state.ComponentStates,
                                x => x is TransformComponent.TransformComponentState);

                            if (idx != -1)
                            {
                                // mover changed positional data and can't be seen
                                var oldState =
                                    (TransformComponent.TransformComponentState) state.ComponentStates[idx];
                                var newState = new TransformComponent.TransformComponentState(Vector2NaN,
                                    oldState.Rotation, oldState.ParentID, oldState.NoLocalRotation);
                                state.ComponentStates[idx] = newState;
                                seenMovers.Remove(uid);
                                ClearLastSeenTick(lSeen, uid);

                                checkedEnts.Add(uid);

                                var needed = oldState.ParentID;
                                if (!needed.IsValid() || checkedEnts.Contains(needed))
                                {
                                    // either no parent attached or parent already included
                                    continue;
                                }

                                if (GetLastSeenTick(lSeen, needed) == GameTick.Zero)
                                {
                                    neededEnts.Add(needed);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // mover already added?
                    if (!viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                    {
                        // mover can't be seen
                        var oldState =
                            (TransformComponent.TransformComponentState) entity.Transform.GetComponentState();
                        entityStates.Add(new EntityState(uid,
                            new ComponentChanged[]
                            {
                                new(false, NetIDs.TRANSFORM, "Transform")
                            },
                            new ComponentState[]
                            {
                                new TransformComponent.TransformComponentState(Vector2NaN, oldState.Rotation,
                                    oldState.ParentID, oldState.NoLocalRotation)
                            }));

                        seenMovers.Remove(uid);
                        ClearLastSeenTick(lSeen, uid);
                        checkedEnts.Add(uid);

                        var needed = oldState.ParentID;
                        if (!needed.IsValid() || checkedEnts.Contains(needed))
                        {
                            // either no parent attached or parent already included
                            continue;
                        }

                        if (GetLastSeenTick(lSeen, needed) == GameTick.Zero)
                        {
                            neededEnts.Add(needed);
                        }
                    }
                }
            }

            var currentTick = CurrentTick;

            // scan pvs box and include children and parents recursively
            IncludeRelatives(GetEntitiesInRange(mapId, position, range, true), relatives);

            // Exclude any entities that are currently invisible to the player.
            ExcludeInvisible(relatives, player.VisibilityMask);

            // Always send updates for all grid and map entities.
            // If we don't, the client-side game state manager WILL blow up.
            // TODO: Make map manager netcode aware of PVS to avoid the need for this workaround.
            IncludeMapCriticalEntities(relatives);

            foreach (var entity in relatives)
            {
                DebugTools.Assert(entity.Initialized && !entity.Deleted);

                var lastChange = entity.LastModifiedTick;

                var uid = entity.Uid;

                var lastSeen = UpdateLastSeenTick(lSeen, uid, currentTick);

                DebugTools.Assert(lastSeen != currentTick);

                /*
                if (uid != playerUid && entity.Prototype == playerEnt.Prototype && lastSeen < fromTick)
                {
                    Logger.DebugS("pvs", $"Player {playerUid} is seeing player {uid}.");
                }
                */

                if (checkedEnts.Contains(uid))
                {
                    // already have it
                    continue;
                }

                if (lastChange <= lastSeen)
                {
                    // hasn't changed since last seen
                    continue;
                }

                // should this be lastSeen or fromTick?
                var entityState = GetEntityState(ComponentManager, uid, lastSeen);

                checkedEnts.Add(uid);

                if (entityState.ComponentStates == null)
                {
                    // no changes
                    continue;
                }

                entityStates.Add(entityState);

                if (uid == playerUid)
                {
                    continue;
                }

                if (!entity.TryGetComponent(out IPhysBody? body))
                {
                    // can't be a mover w/o physics
                    continue;
                }

                if (!body.LinearVelocity.EqualsApprox(Vector2.Zero, MinimumMotionForMovers))
                {
                    // has motion
                    seenMovers.Add(uid);
                }
                else
                {
                    // not moving
                    seenMovers.Remove(uid);
                }
            }

            var priorTick = new GameTick(fromTick.Value - 1);
            foreach (var uid in GetLastSeenOn(lSeen, priorTick))
            {
                if (checkedEnts.Contains(uid))
                {
                    continue;
                }

                if (uid == playerUid)
                {
                    continue;
                }

                if (!TryGetEntity(uid, out var entity) || entity.Deleted)
                {
                    // TODO: remove from states list being sent?
                    continue;
                }

                if (viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                {
                    // can be seen
                    continue;
                }

                var state = GetEntityState(ComponentManager, uid, fromTick);

                if (state.ComponentStates == null)
                {
                    // nothing changed
                    continue;
                }

                checkedEnts.Add(uid);
                entityStates.Add(state);

                seenMovers.Remove(uid);
                ClearLastSeenTick(lSeen, uid);

                var idx = Array.FindIndex(state.ComponentStates, x => x is TransformComponent.TransformComponentState);

                if (idx == -1)
                {
                    // no transform changes
                    continue;
                }

                var oldState = (TransformComponent.TransformComponentState) state.ComponentStates[idx];
                var newState =
                    new TransformComponent.TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID, oldState.NoLocalRotation);
                state.ComponentStates[idx] = newState;


                var needed = oldState.ParentID;
                if (!needed.IsValid() || checkedEnts.Contains(needed))
                {
                    // don't need to include parent or already included
                    continue;
                }

                if (GetLastSeenTick(lSeen, needed) == GameTick.First)
                {
                    neededEnts.Add(needed);
                }
            }

            do
            {
                var moreNeededEnts = new HashSet<EntityUid>();

                foreach (var uid in moreNeededEnts)
                {
                    if (checkedEnts.Contains(uid))
                    {
                        continue;
                    }

                    var entity = GetEntity(uid);
                    var state = GetEntityState(ComponentManager, uid, fromTick);

                    if (state.ComponentStates == null || viewbox.Intersects(GetWorldAabbFromEntity(entity)))
                    {
                        // no states or should already be seen
                        continue;
                    }


                    checkedEnts.Add(uid);
                    entityStates.Add(state);

                    var idx = Array.FindIndex(state.ComponentStates,
                        x => x is TransformComponent.TransformComponentState);

                    if (idx == -1)
                    {
                        // no transform state
                        continue;
                    }

                    var oldState = (TransformComponent.TransformComponentState) state.ComponentStates[idx];
                    var newState =
                        new TransformComponent.TransformComponentState(Vector2NaN, oldState.Rotation,
                            oldState.ParentID, oldState.NoLocalRotation);
                    state.ComponentStates[idx] = newState;
                    seenMovers.Remove(uid);

                    ClearLastSeenTick(lSeen, uid);
                    var needed = oldState.ParentID;

                    if (!needed.IsValid() || checkedEnts.Contains(needed))
                    {
                        // done here
                        continue;
                    }

                    // check if further needed
                    if (!checkedEnts.Contains(uid) && GetLastSeenTick(lSeen, needed) == GameTick.Zero)
                    {
                        moreNeededEnts.Add(needed);
                    }
                }

                neededEnts = moreNeededEnts;
            } while (neededEnts.Count > 0);

            // help the client out
            entityStates.Sort((a, b) => a.Uid.CompareTo(b.Uid));

#if DEBUG_NULL_ENTITY_STATES
            foreach ( var state in entityStates ) {
                if (state.ComponentStates == null)
                {
                    throw new NotImplementedException("Shouldn't send null states.");
                }
            }
#endif

            // no point sending an empty collection
            return entityStates.Count == 0 ? default : entityStates;
        }

        public override void DeleteEntity(IEntity e)
        {
            base.DeleteEntity(e);
            EventBus.RaiseEvent(EventSource.Local, new EntityDeletedMessage(e));
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
                    else if (entity.TryGetComponent(out IPhysBody? physics)
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
            return new(_nextServerEntityUid++);
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
            set.RemoveWhere(e =>
            {
                if (!e.TryGetComponent(out VisibilityComponent? visibility))
                {
                    return false;
                }

                return (visibilityMask & visibility.Layer) == 0;
            });
        }

        public override void Update(float frameTime, Histogram? histogram)
        {
            base.Update(frameTime, histogram);

            EntitiesCount.Set(AllEntities.Count);
        }
    }
}
