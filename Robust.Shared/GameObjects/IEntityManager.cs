using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Holds a collection of entities and the components attached to them.
    /// </summary>
    [PublicAPI]
    public partial interface IEntityManager
    {
        /// <summary>
        ///     The current simulation tick being processed.
        /// </summary>
        GameTick CurrentTick { get; }

        void Initialize();
        void Startup();
        void Shutdown();

        /// <summary>
        ///     Drops every entity, component and entity system.
        /// </summary>
        void Cleanup();

        /// <summary>
        ///     Deletes all entities.
        /// </summary>
        void FlushEntities();

        /// <param name="noPredictions">
        /// Only run systems with <see cref="EntitySystem.UpdatesOutsidePrediction"/> set true.
        /// </param>
        void TickUpdate(float frameTime, bool noPredictions, Histogram? histogram=null);

        /// <summary>
        ///     Client-specific per-render frame updating.
        /// </summary>
        void FrameUpdate(float frameTime);

        IComponentFactory ComponentFactory { get; }
        IEntitySystemManager EntitySysManager { get; }
        IEntityNetworkManager EntityNetManager { get; }
        IEventBus EventBus { get; }

        #region Entity Management

        event Action<Entity<MetaDataComponent>>? EntityAdded;
        event Action<Entity<MetaDataComponent>>? EntityInitialized;
        event Action<Entity<MetaDataComponent>>? EntityDeleted;

        /// <summary>
        /// Invoked when an entity gets dirtied. This only gets raised after initialization, and at most once per tick.
        /// </summary>
        event Action<Entity<MetaDataComponent>>? EntityDirtied;

        /// <summary>
        /// Invoked just before all entities get deleted. See <see cref="FlushEntities"/>.
        /// </summary>
        public event Action? BeforeEntityFlush;

        /// <summary>
        /// Invoked just after all entities got deleted. See <see cref="FlushEntities"/>.
        /// </summary>
        public event Action? AfterEntityFlush;

        /// <summary>
        /// Creates an uninitialized entity.
        /// </summary>
        /// <param name="prototypeName"><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></param>
        /// <param name="euid">Does nothing. Used to be the forced EntityUid of the new entity.</param>
        /// <param name="overrides"><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></param>
        /// <returns><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></returns>
        [Obsolete($"Use one of the other {nameof(CreateEntityUninitialized)} overloads. euid no longer does anything.")]
        EntityUid CreateEntityUninitialized(string? prototypeName, EntityUid euid, ComponentRegistry? overrides = null);

        /// <summary>
        /// Creates an uninitialized entity.
        /// </summary>
        /// <param name="prototypeName"><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></param>
        /// <param name="overrides"><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></param>
        /// <returns><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></returns>
        EntityUid CreateEntityUninitialized(string? prototypeName, ComponentRegistry? overrides = null);

        /// <summary>
        /// Creates an uninitialized entity and sets its position to the EntityCoordinates provided.
        /// </summary>
        /// <param name="prototypeName"><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></param>
        /// <param name="coordinates">Coordinates to set position and parent of the newly spawned entity to.</param>
        /// <param name="overrides"><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></param>
        /// <returns><inheritdoc cref="CreateEntityUninitialized(string?, MapCoordinates , ComponentRegistry?, Angle)"/></returns>
        EntityUid CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default);

        /// <summary>
        /// Creates an uninitialized entity and puts it on the grid or map at the MapCoordinates provided.
        /// </summary>
        /// <param name="prototypeName">Name of the <see cref="EntityPrototype"/> to spawn.</param>
        /// <param name="coordinates">Coordinates to place the newly spawned entity.</param>
        /// <param name="overrides">Overrides to add or remove components that differ from the prototype.</param>
        /// <param name="rotation">Map rotation to set the newly spawned entity to.</param>
        /// <returns>A new uninitialized entity.</returns>
        /// <remarks>If there is a grid at the <paramref name="coordinates"/>, the entity will be parented to the grid.
        /// Otherwise, it will be parented to the map.</remarks>
        EntityUid CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default!);

        void InitializeAndStartEntity(EntityUid entity, MapId? mapId = null);

        void InitializeAndStartEntity(Entity<MetaDataComponent?> entity, bool doMapInit);

        void InitializeEntity(EntityUid entity, MetaDataComponent? meta = null);

        void StartEntity(EntityUid entity);

        /// <summary>
        /// How many entities are currently active.
        /// </summary>
        int EntityCount { get; }

        /// <summary>
        /// Returns all entities
        /// </summary>
        /// <returns></returns>
        IEnumerable<EntityUid> GetEntities();

        public void DirtyEntity(EntityUid uid, MetaDataComponent? metadata = null);

        [Obsolete("use override with an EntityUid")]
        public void Dirty(IComponent component, MetaDataComponent? metadata = null);

        public void Dirty(EntityUid uid, IComponent component, MetaDataComponent? meta = null);

        public void Dirty<T>(Entity<T> ent, MetaDataComponent? meta = null) where T : IComponent;

        public void Dirty<T1, T2>(Entity<T1, T2> ent, MetaDataComponent? meta = null)
            where T1 : IComponent
            where T2 : IComponent;

        public void Dirty<T1, T2, T3>(Entity<T1, T2, T3> ent, MetaDataComponent? meta = null)
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent;

        public void Dirty<T1, T2, T3, T4>(Entity<T1, T2, T3, T4> ent, MetaDataComponent? meta = null)
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent;

        /// <summary>
        /// Tries to QueueDeleteEntity if the entity is not already deleted.
        /// </summary>
        public bool TryQueueDeleteEntity(EntityUid? uid);

        public void QueueDeleteEntity(EntityUid? uid);

        public bool IsQueuedForDeletion(EntityUid uid);

        /// <summary>
        /// Tries to predict entity deletion. On the server it runs the normal code path and on the client the entity is detached.
        /// </summary>
        void PredictedDeleteEntity(Entity<MetaDataComponent?, TransformComponent?> ent);

        /// <summary>
        /// <see cref="PredictedDeleteEntity(Robust.Shared.GameObjects.Entity{Robust.Shared.GameObjects.MetaDataComponent?,Robust.Shared.GameObjects.TransformComponent?})"/>
        /// </summary>
        void PredictedDeleteEntity(Entity<MetaDataComponent?, TransformComponent?>? ent);

        /// <summary>
        /// Tries to predict entity deletion. On the server it runs the normal code path and on the client the entity is detached.
        /// </summary>
        void PredictedQueueDeleteEntity(Entity<MetaDataComponent?, TransformComponent?> ent);

        /// <summary>
        /// <see cref="PredictedQueueDeleteEntity(Robust.Shared.GameObjects.Entity{Robust.Shared.GameObjects.MetaDataComponent?,Robust.Shared.GameObjects.TransformComponent?})"/>
        /// </summary>
        void PredictedQueueDeleteEntity(Entity<MetaDataComponent?, TransformComponent?>? ent);

        /// <summary>
        /// Shuts-down and removes the entity with the given <see cref="Robust.Shared.GameObjects.EntityUid"/>. This is also broadcast to all clients.
        /// </summary>
        /// <param name="uid">Uid of entity to remove.</param>
        void DeleteEntity(EntityUid? uid);

        /// <summary>
        /// Shuts-down and removes the entity with the given <see cref="Robust.Shared.GameObjects.EntityUid"/>. This is also broadcast to all clients.
        /// </summary>
        void DeleteEntity(EntityUid uid, MetaDataComponent meta, TransformComponent xform);

        /// <summary>
        /// Checks whether an entity with the specified ID exists.
        /// </summary>
        bool EntityExists(EntityUid uid);

        /// <summary>
        /// Checks whether an entity with the specified ID exists.
        /// </summary>
        bool EntityExists([NotNullWhen(true)] EntityUid? uid);

        /// <summary>
        /// Returns true if entity is valid and paused.
        /// </summary>
        bool IsPaused([NotNullWhen(true)] EntityUid? uid, MetaDataComponent? metadata = null);

        /// <summary>
        /// Checks whether an entity with the specified ID has been deleted or is nonexistent.
        /// </summary>
        bool Deleted(EntityUid uid);

        /// <summary>
        /// Checks whether an entity with the specified ID has been deleted or is nonexistent.
        /// </summary>
        bool Deleted([NotNullWhen(false)] EntityUid? uid);

        void RunMapInit(EntityUid entity, MetaDataComponent meta);

        /// <summary>
        /// Returns a string representation of an entity with various information regarding it.
        /// </summary>
        EntityStringRepresentation ToPrettyString(EntityUid uid, MetaDataComponent? metadata);

        /// <summary>
        /// Returns a string representation of an entity with various information regarding it.
        /// </summary>
        EntityStringRepresentation ToPrettyString(Entity<MetaDataComponent?> uid);

        /// <summary>
        /// Returns a string representation of an entity with various information regarding it.
        /// </summary>
        EntityStringRepresentation ToPrettyString(NetEntity netEntity);

        /// <summary>
        /// Returns a string representation of an entity with various information regarding it.
        /// </summary>
        [return: NotNullIfNotNull("uid")]
        EntityStringRepresentation? ToPrettyString(EntityUid? uid, MetaDataComponent? metadata = null);

        /// <summary>
        /// Returns a string representation of an entity with various information regarding it.
        /// </summary>
        [return: NotNullIfNotNull("netEntity")]
        EntityStringRepresentation? ToPrettyString(NetEntity? netEntity);

        #endregion Entity Management

        /// <summary>
        ///     Sends a networked message to the server, while also repeatedly raising it locally for every time this tick gets re-predicted.
        /// </summary>
        void RaisePredictiveEvent<T>(T msg) where T : EntityEventArgs;
    }
}
