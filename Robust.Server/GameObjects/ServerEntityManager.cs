using System;
using System.Collections.Generic;
using Prometheus;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ServerEntityManager : EntityManager, IServerEntityManagerInternal, IEntityEventSubscriber
    {
        private static readonly Gauge EntitiesCount = Metrics.CreateGauge(
            "robust_entities_count",
            "Amount of alive entities.");

        #region IEntityManager Members

        [Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Shared.IoC.Dependency] private readonly IPauseManager _pauseManager = default!;
        [Shared.IoC.Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private EntityLookupSystem _lookupSystem = default!;

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
        public List<EntityState>? GetAllEntityStates(GameTick fromTick, IPlayerSession session)
        {
            var stateEntities = new List<EntityState>();
            foreach (var entity in AllEntities)
            {
                if (entity.Deleted)
                    continue;

                DebugTools.Assert(entity.Initialized);

                if (entity.LastModifiedTick <= fromTick)
                    continue;

                stateEntities.Add(GetEntityState(ComponentManager, entity.Uid, fromTick, session));
            }

            var data = _lookupSystem.GetPlayerLastSeen(session);
            if (data != null)
            {
                foreach (var state in stateEntities)
                {
                    data.EntityLastSeen[state.Uid] = fromTick;
                }
            }

            // no point sending an empty collection
            return stateEntities.Count == 0 ? default : stateEntities;
        }

        /// <summary>
        ///     AKA PVS (potentially visible set). Get all relevant ComponentStates in range of us.
        /// </summary>
        /// <param name="fromTick"></param>
        /// <param name="currentTick"></param>
        /// <param name="player"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public List<EntityState>? GetEntityStates(GameTick fromTick, GameTick currentTick, IPlayerSession player, float range)
        {
            // if you want to research yourself I think it's normally called interest management
            // Future optimisation pipeline idea:
            // Given most clients will just need the latest tick updated we could :
            // 1. pre-calculate the updated entities in every chunk and store it in a List.
            // (we can probably do this when doing HandleDirtyEvent)
            // 2. Work out the relevant chunks for each player in parallel all at once (i.e. GetChunksInRange)
            // 3. Sort players so that any with the same chunks to update get the same message object (should have big savings for busy areas like the bar)
            // This would essentially be the "fast path" and if needed we fallback to the below if they need more data.

            // Old PVS used to just get all for no session...
            var playerEnt = player.AttachedEntity;
            if (playerEnt == null)
                return GetAllEntityStates(fromTick, player);

            var data = _lookupSystem.GetPlayerLastSeen(player);
            if (data == null)
                return new List<EntityState>();

            // Probably a round restart so we'll dump all the data on them.
            if (data.EntityLastSeen.Count == 0)
                return GetAllEntityStates(fromTick, player);

            var entityStates = data.EntityStates;
            entityStates.Clear();
            var transform = playerEnt.Transform;
            var playerPos = transform.WorldPosition;
            var mapId = transform.MapID;

            // TODO: This is based on the old method but ideally it iterates through all of their eyes enlarged
            var viewbox = new Box2(playerPos, playerPos).Enlarged(range);

            var seenEntities = new HashSet<EntityUid>();

            // Send important entities (all maps and grids).
            // Could potentially trim this down in future however atm it'll throw or lead to unexpected behavior.
            foreach (var map in _mapManager.GetAllMapIds())
            {
                if (map == MapId.Nullspace) continue;
                var mapEntity = _mapManager.GetMapEntity(map);

                seenEntities.Add(mapEntity.Uid);
                AddEntityState(data, player, fromTick, mapEntity, entityStates);

                foreach (var grid in _mapManager.GetAllMapGrids(map))
                {
                    var gridEntity = GetEntity(grid.GridEntityId);
                    seenEntities.Add(gridEntity.Uid);
                    AddEntityState(data, player, fromTick, gridEntity, entityStates);
                }
            }

            // TODO: For stuff that needs a higher pvs range (e.g. lights) can either
            // A) Store the comps in the chunks directly and use an enlarged viewbox or
            // B) Make a separate DynamicTree for them (probably preferable?)
            foreach (var chunk in _lookupSystem.GetChunksInRange(mapId, viewbox))
            {
                var chunkLastSeen = data.LastSeen(chunk);

                if (chunkLastSeen != null && chunk.LastModifiedTick <= chunkLastSeen)
                    continue;

                data.UpdateChunk(currentTick, chunk);

                // TODO: Could maybe optimise this a bit more somehow
                foreach (var entity in chunk.GetEntities(unique: false, excluded: seenEntities))
                {
                    seenEntities.Add(entity.Uid);
                    // TODO: Probably don't send container data to clients maybe?
                    // Though I guess sending contents is useful for prediction ahhhhhhhh
                    AddEntityState(data, player, fromTick, entity, entityStates);
                }
            }

            // TODO: I think there may still be the issue of parents not being sent
            // If there is then to solve this just have each entity retrieved by the lookup
            // also check if its parent has been retrieved and if not then also retrieve it.

            // Sort for the client.
            entityStates.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            return entityStates;
        }

        /// <summary>
        ///     Try to add the entity state if it needs an update
        /// </summary>
        /// <param name="data"></param>
        /// <param name="player"></param>
        /// <param name="fromTick"></param>
        /// <param name="entity"></param>
        /// <param name="entityStates"></param>
        private void AddEntityState(PlayerLookupChunks data, IPlayerSession player, GameTick fromTick, IEntity entity, List<EntityState> entityStates)
        {
            EntityState state;

            if (data.EntityLastSeen.TryGetValue(entity.Uid, out var lastSeen))
            {
                if (entity.LastModifiedTick <= lastSeen ||
                    entity.TryGetComponent(out VisibilityComponent? visibility) &&
                    (player.VisibilityMask & visibility.Layer) == 0x0)
                {
                    return;
                }

                state = GetEntityState(ComponentManager, entity.Uid, lastSeen, player);
            }
            // Never before seen entity
            else
            {
                state = GetEntityState(ComponentManager, entity.Uid, fromTick, player);
            }

            data.EntityLastSeen[entity.Uid] = entity.LastModifiedTick;

            // No net components most likely (e.g. spawners)
            if (state.ComponentStates == null)
                return;

            entityStates.Add(state);
        }

        public override void DeleteEntity(IEntity e)
        {
            base.DeleteEntity(e);
            EventBus.QueueEvent(EventSource.Local, new EntityDeletedMessage(e));
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
            _lookupSystem = EntitySystem.Get<EntityLookupSystem>();
            Started = true;
        }

        /// <summary>
        ///     Generates a network entity state for the given entity.
        /// </summary>
        /// <param name="session">The player we're retrieving a state for.</param>
        /// <param name="compMan">ComponentManager that contains the components for the entity.</param>
        /// <param name="entityUid">Uid of the entity to generate the state from.</param>
        /// <param name="fromTick">Only provide delta changes from this tick.</param>
        /// <param name="player">The player to generate this state for.</param>
        /// <returns>New entity State for the given entity.</returns>
        private static EntityState GetEntityState(IComponentManager compMan, EntityUid entityUid, GameTick fromTick, IPlayerSession player)
        {
            var compStates = new List<ComponentState>();
            var changed = new List<ComponentChanged>();

            foreach (var comp in compMan.GetNetComponents(entityUid))
            {
                DebugTools.Assert(comp.Initialized);

                // TODO: This comment is a lie as A) It's being set to Tick 1 and B) Client throws

                // NOTE: When LastModifiedTick or CreationTick are 0 it means that the relevant data is
                // "not different from entity creation".
                // i.e. when the client spawns the entity and loads the entity prototype,
                // the data it deserializes from the prototype SHOULD be equal
                // to what the component state / ComponentChanged would send.
                // As such, we can avoid sending this data in this case since the client "already has it".

                if (comp.NetSyncEnabled && comp.LastModifiedTick != GameTick.Zero && comp.LastModifiedTick >= fromTick)
                    compStates.Add(comp.GetComponentState(player));

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

            // TODO: Force send only metadata if it was created on tick 1? Should be a speedup and
            // makes not sending every entity on connect more viable.

            return new EntityState(entityUid, changed.ToArray(), compStates.ToArray());
        }

        public override void Update(float frameTime, Histogram? histogram)
        {
            base.Update(frameTime, histogram);

            EntitiesCount.Set(AllEntities.Count);
        }
    }
}
