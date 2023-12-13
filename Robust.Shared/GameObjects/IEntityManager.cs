using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.Map;
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
        IEntityNetworkManager? EntityNetManager { get; }
        IEventBus EventBus { get; }

        #region Entity Management

        event Action<EntityUid>? EntityAdded;
        event Action<EntityUid>? EntityInitialized;
        event Action<EntityUid, MetaDataComponent>? EntityDeleted;
        event Action<EntityUid>? EntityDirtied; // only raised after initialization

        EntityUid CreateEntityUninitialized(string? prototypeName, EntityUid euid, ComponentRegistry? overrides = null);

        EntityUid CreateEntityUninitialized(string? prototypeName, ComponentRegistry? overrides = null);

        EntityUid CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates, ComponentRegistry? overrides = null);

        EntityUid CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates, ComponentRegistry? overrides = null);

        void InitializeAndStartEntity(EntityUid entity, MapId? mapId = null);

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

        public void QueueDeleteEntity(EntityUid? uid);

        public bool IsQueuedForDeletion(EntityUid uid);

        /// <summary>
        /// Shuts-down and removes the entity with the given <see cref="Robust.Shared.GameObjects.EntityUid"/>. This is also broadcast to all clients.
        /// </summary>
        /// <param name="uid">Uid of entity to remove.</param>
        void DeleteEntity(EntityUid? uid);

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
        EntityStringRepresentation ToPrettyString(EntityUid uid, MetaDataComponent? metadata = null);

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
