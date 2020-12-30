using System;
using System.Collections.Generic;
using Prometheus;
using Robust.Server.GameObjects.Components;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
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
        private static readonly Gauge EntitiesCount = Metrics.CreateGauge(
            "robust_entities_count",
            "Amount of alive entities.");

        private const float MinimumMotionForMovers = 1 / 128f;

        #region IEntityManager Members

        [Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Shared.IoC.Dependency] private readonly IPauseManager _pauseManager = default!;
        [Shared.IoC.Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private ServerEntityLookupSystem _lookupSystem = default!;

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
                    continue;

                DebugTools.Assert(entity.Initialized);

                if (entity.LastModifiedTick <= fromTick)
                    continue;

                stateEntities.Add(GetEntityState(ComponentManager, entity.Uid, fromTick));
            }

            // no point sending an empty collection
            return stateEntities.Count == 0 ? default : stateEntities;
        }

        // TODO: Physics chunks doesn't need any of this shit, just needs to subscribe to MoveEvent IMO.

        public List<EntityState> GetEntityStates(GameTick fromTick, GameTick currentTick, IPlayerSession player, float range)
        {
            var playerEnt = player.AttachedEntity;
            if (playerEnt == null)
                // TODO: Return ALL
                return new List<EntityState>();

            var data = _lookupSystem.GetPlayerLastSeen(player);
            if (data == null)
                return new List<EntityState>();

            var entityStates = new List<EntityState>();
            var transform = playerEnt.Transform;
            var playerPos = transform.WorldPosition;
            var mapId = transform.MapID;

            // TODO: This is based on the old method but ideally it iterates through all of their eyes enlarged
            // TODO: We should also consider some stuff like lights which necessitate a higher PVS range
            // Probably just have a separate component that the lookups tracks separately
            var viewbox = new Box2(playerPos, playerPos).Enlarged(range);

            // TODO: Ideally each chunk has "LastModifiedTick" that is the latest of any entity contained within
            // Then we can just do a quicker check... stuff gets modified frequently but I think this will still work well...
            // Would also need to store last time we sent a chunk to a particular player given they don't get sent the whole chunk
            foreach (var entity in _lookupSystem.GetEntitiesIntersecting(mapId, viewbox, includeGrids: true, approximate: true))
            {
                // TODO: Probably don't send container data to clients maybe? Though we need to fix containers so they
                // don't throw if we don't send it (coz currently they do).
                // Though I guess sending contents is useful for prediction ahhhhhhhh

                // Get whether we need to send dat state
                // If we haven't seen it ever then force send that baby
                if (data.EntityLastSeen.TryGetValue(entity.Uid, out var lastSeen))
                {
                    if (entity.LastModifiedTick <= lastSeen ||
                        (entity.TryGetComponent(out VisibilityComponent? visibility) &&
                         (player.VisibilityMask & visibility.Layer) == 0))
                    {
                        continue;
                    }
                }

                var state = GetEntityState(ComponentManager, entity.Uid, fromTick);
                data.EntityLastSeen[entity.Uid] = entity.LastModifiedTick;

                // Sending nothing at all
                if (state.ComponentStates == null)
                    continue;

                entityStates.Add(state);
                // TODO: Look at that transform shit.
            }

            // Sort for the client.
            entityStates.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            return entityStates;

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
            _lookupSystem = EntitySystem.Get<ServerEntityLookupSystem>();
            Started = true;
        }

        /// <summary>
        /// Generates a network entity state for the given entity.
        /// </summary>
        /// <param name="compMan">ComponentManager that contains the components for the entity.</param>
        /// <param name="entityUid">Uid of the entity to generate the state from.</param>
        /// <param name="fromTick">Only provide delta changes from this tick.</param>
        /// <returns>New entity State for the given entity.</returns>
        public EntityState GetEntityState(IComponentManager compMan, EntityUid entityUid, GameTick fromTick)
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

        public override void Update(float frameTime, Histogram? histogram)
        {
            base.Update(frameTime, histogram);

            EntitiesCount.Set(AllEntities.Count);
        }
    }
}
