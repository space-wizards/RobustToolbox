using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{

    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ServerEntityManager : EntityManager, IServerEntityManagerInternal
    {

        private const float MinimumMotionForMovers = 1 / 128f;

        #region IEntityManager Members

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;

        [Dependency] private readonly IPauseManager _pauseManager;

        [Dependency] private readonly IConfigurationManager _configurationManager;
#pragma warning restore 649

        private float? _maxUpdateRangeCache;

        public float MaxUpdateRange => _maxUpdateRangeCache
            ??= _configurationManager.GetCVar<float>("net.maxupdaterange");

        private int _nextServerEntityUid = (int) EntityUid.FirstUid;

        private readonly List<(GameTick tick, EntityUid uid)> _deletionHistory = new List<(GameTick, EntityUid)>();

        public override void Update()
        {
            base.Update();
            _maxUpdateRangeCache = null;
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string prototypeName)
        {
            return CreateEntity(prototypeName);
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string prototypeName, GridCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName);
            if (coordinates.GridID != GridId.Invalid)
            {
                var gridEntityId = _mapManager.GetGrid(coordinates.GridID).GridEntityId;
                newEntity.Transform.AttachParent(GetEntity(gridEntityId));
                newEntity.Transform.LocalPosition = coordinates.Position;
            }

            return newEntity;
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string prototypeName, MapCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName);
            if (prototypeName != null)
            {
                // At this point in time, all data configure on the entity *should* be purely from the prototype.
                // As such, we can reset the modified ticks to Zero,
                // which indicates "not different from client's own deserialization".
                // So the initial data for the component or even the creation doesn't have to be sent over the wire.
                foreach (var component in ComponentManager.GetNetComponents(newEntity.Uid))
                {
                    ((Component) component).ClearTicks();
                }
            }

            newEntity.Transform.AttachParent(_mapManager.GetMapEntity(coordinates.MapId));
            newEntity.Transform.WorldPosition = coordinates.Position;
            return newEntity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntity(string protoName, GridCoordinates coordinates)
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
        public override IEntity SpawnEntity(string protoName, MapCoordinates coordinates)
        {
            var entity = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) entity);
            return entity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntityNoMapInit(string protoName, GridCoordinates coordinates)
        {
            var newEnt = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) newEnt);
            return newEnt;
        }

        /// <inheritdoc />
        public List<EntityState> GetEntityStates(GameTick fromTick)
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

        private readonly IDictionary<IPlayerSession, ISet<EntityUid>> _seenMovers
            = new Dictionary<IPlayerSession, ISet<EntityUid>>();

        private ISet<EntityUid> GetSeenMovers(IPlayerSession player)
        {
            if (!_seenMovers.TryGetValue(player, out var movers))
            {
                movers = new SortedSet<EntityUid>();
                _seenMovers.Add(player, movers);
            }

            return movers;
        }

        private readonly IDictionary<IPlayerSession, IDictionary<EntityUid, GameTick>> _playerLastSeen
            = new Dictionary<IPlayerSession, IDictionary<EntityUid, GameTick>>();

        private static readonly Vector2 Vector2NaN = new Vector2(float.NaN, float.NaN);

        private IDictionary<EntityUid, GameTick> GetLastSeen(IPlayerSession player)
        {
            if (!_playerLastSeen.TryGetValue(player, out var lastSeen))
            {
                lastSeen = new Dictionary<EntityUid, GameTick>();
                _playerLastSeen.Add(player, lastSeen);
            }

            return lastSeen;
        }

        private GameTick GetLastSeenTick(IPlayerSession player, EntityUid uid)
        {
            var lastSeen = GetLastSeen(player);

            if (!lastSeen.TryGetValue(uid, out var tick))
            {
                tick = GameTick.First;
            }

            return tick;
        }

        private GameTick UpdateLastSeenTick(IPlayerSession player, EntityUid uid, GameTick newTick)
        {
            var lastSeen = GetLastSeen(player);

            if (!lastSeen.TryGetValue(uid, out var oldTick))
            {
                oldTick = GameTick.First;
            }

            lastSeen[uid] = newTick;

            return oldTick;
        }

        private IEnumerable<EntityUid> GetLastSeenAfter(IPlayerSession player, GameTick fromTick)
        {
            var lastSeen = GetLastSeen(player);

            foreach (var (uid, tick) in lastSeen)
            {
                if (tick > fromTick)
                {
                    yield return uid;
                }
            }
        }

        private IEnumerable<EntityUid> GetLastSeenOn(IPlayerSession player, GameTick fromTick)
        {
            var lastSeen = GetLastSeen(player);

            foreach (var (uid, tick) in lastSeen)
            {
                if (tick == fromTick)
                {
                    yield return uid;
                }
            }
        }

        private void SetLastSeenTick(IPlayerSession player, EntityUid uid, GameTick tick)
        {
            var lastSeen = GetLastSeen(player);

            lastSeen[uid] = tick;
        }

        private void ClearLastSeenTick(IPlayerSession player, EntityUid uid)
        {
            var lastSeen = GetLastSeen(player);

            lastSeen.Remove(uid);
        }

        public void DropPlayerState(IPlayerSession player)
        {
            _playerLastSeen.Remove(player);
        }

        private IEnumerable<IEntity> IncludeRelatives(IEnumerable<IEntity> children)
        {
            var set = new HashSet<IEntity>();
            foreach (var child in children)
            {
                var ent = child;

                do
                {
                    set.Add(ent);

                    AddContainedRecursive(ent, set);

                    ent = ent.Transform.Parent?.Owner;
                } while (ent != null && !ent.Deleted);
            }

            return set;
        }

        private static void AddContainedRecursive(IEntity ent, ICollection<IEntity> set)
        {
            if (!ent.TryGetComponent(out ContainerManagerComponent contMgr))
            {
                return;
            }

            foreach (var container in contMgr.GetAllContainers())
            {
                foreach (var contEnt in container.ContainedEntities)
                {
                    set.Add(contEnt);
                    AddContainedRecursive(contEnt, set);
                }
            }
        }

        private readonly struct PlayerSeenEntityStatesResources
        {

            public readonly HashSet<EntityUid> IncludedEnts;

            public readonly List<EntityState> EntityStates;

            public readonly HashSet<EntityUid> NeededEnts;

            public PlayerSeenEntityStatesResources(bool memes = false)
            {
                IncludedEnts = new HashSet<EntityUid>();
                EntityStates = new List<EntityState>();
                NeededEnts = new HashSet<EntityUid>();
            }

        }

        private readonly PlayerSeenEntityStatesResources _playerSeenEntityStatesResources
            = new PlayerSeenEntityStatesResources(false);

        /// <inheritdoc />
        public List<EntityState> UpdatePlayerSeenEntityStates(GameTick fromTick, IPlayerSession player, float range)
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

            var checkedEnts = _playerSeenEntityStatesResources.IncludedEnts;
            var entityStates = _playerSeenEntityStatesResources.EntityStates;
            var neededEnts = _playerSeenEntityStatesResources.NeededEnts;
            checkedEnts.Clear();
            entityStates.Clear();
            neededEnts.Clear();

            foreach (var uid in seenMovers.ToList())
            {
                if (!TryGetEntity(uid, out var entity) || entity.Deleted)
                {
                    seenMovers.Remove(uid);
                    continue;
                }

                if (entity.TryGetComponent(out PhysicsComponent body))
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
                            if (entity.TryGetComponent(out TransformComponent txf) && txf.ChildCount > 0 )
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
                            var idx = state.ComponentStates
                                .FindIndex(x => x is TransformComponent.TransformComponentState);
                            if (idx != -1)
                            {
                                // mover changed positional data and can't be seen
                                var oldState = (TransformComponent.TransformComponentState) state.ComponentStates[idx];
                                var newState = new TransformComponent.TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID);
                                state.ComponentStates[idx] = newState;
                                seenMovers.Remove(uid);
                                ClearLastSeenTick(player, uid);

                                checkedEnts.Add(uid);

                                var needed = oldState.ParentID;
                                if (needed == null || checkedEnts.Contains(needed.Value))
                                {
                                    // either no parent attached or parent already included
                                    continue;
                                }

                                var neededUid = needed.Value;
                                if (GetLastSeenTick(player, neededUid) == GameTick.Zero)
                                {
                                    neededEnts.Add(needed.Value);
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
                        var oldState = (TransformComponent.TransformComponentState) entity.Transform.GetComponentState();
                        entityStates.Add(new EntityState(uid,
                            new List<ComponentChanged>
                            {
                                new ComponentChanged(false, NetIDs.TRANSFORM, "Transform")
                            },
                            new List<ComponentState>
                            {
                                new TransformComponent.TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID)
                            }));

                        seenMovers.Remove(uid);
                        ClearLastSeenTick(player, uid);
                        checkedEnts.Add(uid);

                        var needed = oldState.ParentID;
                        if (needed == null || checkedEnts.Contains(needed.Value))
                        {
                            // either no parent attached or parent already included
                            continue;
                        }

                        var neededUid = needed.Value;
                        if (GetLastSeenTick(player, neededUid) == GameTick.Zero)
                        {
                            neededEnts.Add(needed.Value);
                        }
                    }
                }
            }

            var currentTick = CurrentTick;

            // scan pvs box and include children and parents recursively
            foreach (var entity in IncludeRelatives(GetEntitiesInRange(mapId, position, range)))
            {
                DebugTools.Assert(entity.Initialized && !entity.Deleted);

                var lastChange = entity.LastModifiedTick;

                var uid = entity.Uid;

                var lastSeen = UpdateLastSeenTick(player, uid, currentTick);

                DebugTools.Assert(lastSeen != currentTick);

                if (uid != playerUid && entity.Prototype == playerEnt.Prototype && lastSeen < fromTick)
                {
                    Logger.DebugS("pvs", $"Player {playerUid} is seeing player {uid}.");
                }

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

                if (!entity.TryGetComponent(out PhysicsComponent body))
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
            foreach (var uid in GetLastSeenOn(player, priorTick))
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
                ClearLastSeenTick(player,uid);

                var idx = state.ComponentStates
                    .FindIndex(x => x is TransformComponent.TransformComponentState);

                if (idx == -1)
                {
                    // no transform changes
                    continue;
                }

                var oldState = (TransformComponent.TransformComponentState) state.ComponentStates[idx];
                var newState = new TransformComponent.TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID);
                state.ComponentStates[idx] = newState;


                var needed = oldState.ParentID;
                if (needed == null || checkedEnts.Contains(needed.Value))
                {
                    // don't need to include parent or already included
                    continue;
                }

                var neededUid = needed.Value;
                if (GetLastSeenTick(player, neededUid) == GameTick.First)
                {
                    neededEnts.Add(needed.Value);
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

                    var idx = state.ComponentStates
                        .FindIndex(x => x is TransformComponent.TransformComponentState);

                    if (idx == -1)
                    {
                        // no transform state
                        continue;
                    }

                    var oldState = (TransformComponent.TransformComponentState) state.ComponentStates[idx];
                    var newState = new TransformComponent.TransformComponentState(Vector2NaN, oldState.Rotation, oldState.ParentID);
                    state.ComponentStates[idx] = newState;
                    seenMovers.Remove(uid);

                    ClearLastSeenTick(player, uid);
                    var needed = oldState.ParentID;

                    if (needed == null || checkedEnts.Contains(needed.Value))
                    {
                        // done here
                        continue;
                    }

                    // check if further needed
                    var neededUid = needed.Value;
                    if (!checkedEnts.Contains(uid) && GetLastSeenTick(player, neededUid) == GameTick.Zero)
                    {
                        moreNeededEnts.Add(needed.Value);
                    }
                }

                neededEnts = moreNeededEnts;
            } while (neededEnts.Count > 0);

            // help the client out
            entityStates.Sort((a,b) => a.Uid.CompareTo(b.Uid));

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

            _deletionHistory.Add((CurrentTick, e.Uid));
        }

        public List<EntityUid> GetDeletedEntities(GameTick fromTick)
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
                || !Entities.ContainsKey(entity.Uid)
                || !entity.TryGetComponent(out ITransformComponent txf)
                || !txf.Initialized)
            {
                return updated;
            }

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

                if (!playerEnt.TryGetComponent(out ITransformComponent playerTxf))
                {
                    // not in world
                    continue;
                }

                var playerPos = playerTxf.WorldPosition;

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
                    var changes = GetEntityState(ComponentManager, entityUid, currentTick);
                    if (changes.ComponentStates != null && changes.ComponentStates
                        .Any(x => x is TransformComponent.TransformComponentState || x is PhysicsComponentState))
                    {
                        GetSeenMovers(player).Add(entityUid);
                    }
                }
            }

            return updated;
        }

        private bool AnyParentMoving(IPlayerSession player, EntityUid entityUid)
            => AnyParentInSet(entityUid, GetSeenMovers(player));

        private bool AnyParentInSet(EntityUid entityUid, ISet<EntityUid> set)
        {
            for (;;)
            {
                if (!TryGetEntity(entityUid, out var ent))
                {
                    return false;
                }

                if (!ent.TryGetComponent(out TransformComponent txf))
                {
                    return false;
                }

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

        IEntity IServerEntityManagerInternal.AllocEntity(string prototypeName, EntityUid? uid)
        {
            return AllocEntity(prototypeName, uid);
        }

        protected override EntityUid GenerateEntityUid()
        {
            return new EntityUid(_nextServerEntityUid++);
        }

        void IServerEntityManagerInternal.FinishEntityLoad(IEntity entity, IEntityLoadContext context)
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
                    changed.Add(ComponentChanged.Added(comp.NetID.Value, comp.Name));
                }
                else if (comp.Deleted && comp.LastModifiedTick >= fromTick)
                {
                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChanged.Removed(comp.NetID.Value));
                }
            }

            return new EntityState(entityUid, changed, compStates);
        }

    }

}
