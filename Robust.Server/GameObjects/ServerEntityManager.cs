using System;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    [UsedImplicitly] // DI Container
    public sealed class ServerEntityManager : EntityManager, IServerEntityManagerInternal
    {
        private static readonly Gauge EntitiesCount = Metrics.CreateGauge(
            "robust_entities_count",
            "Amount of alive entities.");

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        private int _nextServerEntityUid = (int) EntityUid.FirstUid;

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

        /// <inheritdoc />
        public override IEntity SpawnEntity(string? protoName, EntityCoordinates coordinates)
        {
            if (!coordinates.IsValid(this))
                throw new InvalidOperationException($"Tried to spawn entity {protoName} on invalid coordinates {coordinates}.");

            var entity = CreateEntityUninitialized(protoName, coordinates);

            InitializeAndStartEntity((Entity) entity);

            if (_pauseManager.IsMapInitialized(coordinates.GetMapId(this))) entity.RunMapInit();

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
        public override void Startup()
        {
            EntitySystemManager.Initialize();
            base.Startup();
            Started = true;
        }

        /// <inheritdoc />
        public override void TickUpdate(float frameTime, Histogram? histogram)
        {
            base.TickUpdate(frameTime, histogram);

            EntitiesCount.Set(AllEntities.Count);
        }

        IEntity IServerEntityManagerInternal.AllocEntity(string? prototypeName, EntityUid? uid)
        {
            return AllocEntity(prototypeName, uid);
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
        protected override EntityUid GenerateEntityUid()
        {
            return new(_nextServerEntityUid++);
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
                    if (prototype.Components.ContainsKey(component.Name)) ((Component) component).ClearTicks();
                }
            }

            return entity;
        }
    }
}
