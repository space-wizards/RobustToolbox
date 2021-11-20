using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    public sealed class Entity : IEntity
    {
        #region Members

        /// <inheritdoc />
        public IEntityManager EntityManager { get; }

        /// <inheritdoc />
        [ViewVariables]
        public EntityUid Uid { get; }

        /// <inheritdoc />
        EntityLifeStage IEntity.LifeStage { get => LifeStage; set => LifeStage = value; }

        public EntityLifeStage LifeStage { get => MetaData.EntityLifeStage; internal set => MetaData.EntityLifeStage = value; }

        [ViewVariables]
        GameTick IEntity.LastModifiedTick { get => MetaData.EntityLastModifiedTick; set => MetaData.EntityLastModifiedTick = value; }


        /// <inheritdoc />
        [ViewVariables]
        public EntityPrototype? Prototype
        {
            get => MetaData.EntityPrototype;
            internal set => MetaData.EntityPrototype = value;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public string Description
        {
            get => MetaData.EntityDescription;
            set => MetaData.EntityDescription = value;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public string Name
        {
            get => MetaData.EntityName;
            set => MetaData.EntityName = value;
        }

        /// <inheritdoc />
        public bool Initialized => LifeStage >= EntityLifeStage.Initialized;

        /// <inheritdoc />
        public bool Initializing => LifeStage == EntityLifeStage.Initializing;

        /// <inheritdoc />
        public bool Deleted => LifeStage >= EntityLifeStage.Deleted;

        /// <inheritdoc />
        [ViewVariables]
        public bool Paused
        {
            get => !IgnorePaused && IoCManager.Resolve<IPauseManager>().IsMapPaused(Transform.MapID);

            [Obsolete("This does nothing. Use IPauseManager to pause the map for editing.")]
            set { }
        }

        /// <inheritdoc />
        [ViewVariables]
        public bool IgnorePaused
        {
            get => MetaData.IgnorePaused;
            set => MetaData.IgnorePaused = value;
        }

        private TransformComponent? _transform;

        /// <inheritdoc />
        [ViewVariables]
        public TransformComponent Transform => _transform ??= GetComponent<TransformComponent>();

        private MetaDataComponent? _metaData;

        /// <inheritdoc />
        [ViewVariables]
        public MetaDataComponent MetaData
        {
            get => _metaData ??= GetComponent<MetaDataComponent>();
            internal set => _metaData = value;
        }

        #endregion Members

        #region Initialization

        public Entity(IEntityManager entityManager, EntityUid uid)
        {
            EntityManager = entityManager;
            Uid = uid;
        }

        /// <inheritdoc />
        public bool IsValid()
        {
            return EntityManager.EntityExists(Uid);
        }

        #endregion Initialization

        #region Component Messaging

        /// <inheritdoc />
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public void SendMessage(IComponent? owner, ComponentMessage message)
        {
            var components = EntityManager.GetComponents(Uid);
            foreach (var component in components)
            {
                if (owner != component)
                    component.HandleMessage(message, owner);
            }
        }

        /// <inheritdoc />
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public void SendNetworkMessage(IComponent owner, ComponentMessage message, INetChannel? channel = null)
        {
            EntityManager.EntityNetManager?.SendComponentNetworkMessage(channel, this, owner, message);
        }

        #endregion Component Messaging

        #region Components

        /// <summary>
        ///     Public method to add a component to an entity.
        ///     Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddComponent(Component component)
        {
            EntityManager.AddComponent(this, component);
        }

        /// <inheritdoc />
        public T AddComponent<T>()
            where T : Component, new()
        {
            return EntityManager.AddComponent<T>(this);
        }

        /// <inheritdoc />
        public void RemoveComponent<T>()
        {
            EntityManager.RemoveComponent<T>(Uid);
        }

        /// <inheritdoc />
        public bool HasComponent<T>()
        {
            return EntityManager.HasComponent<T>(Uid);
        }

        /// <inheritdoc />
        public bool HasComponent(Type type)
        {
            return EntityManager.HasComponent(Uid, type);
        }

        /// <inheritdoc />
        public T GetComponent<T>()
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return (T)EntityManager.GetComponent(Uid, typeof(T));
        }

        /// <inheritdoc />
        public IComponent GetComponent(Type type)
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.GetComponent(Uid, type);
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : class
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.TryGetComponent(Uid, out component);
        }

        public T? GetComponentOrNull<T>() where T : class
        {
            return TryGetComponent(out T? component) ? component : default;
        }

        /// <inheritdoc />
        public bool TryGetComponent(Type type, [NotNullWhen(true)] out IComponent? component)
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.TryGetComponent(Uid, type, out component);
        }

        public IComponent? GetComponentOrNull(Type type)
        {
            return TryGetComponent(type, out var component) ? component : null;
        }

        /// <inheritdoc />
        public void QueueDelete()
        {
            EntityManager.QueueDeleteEntity(this);
        }

        /// <inheritdoc />
        public void Delete()
        {
            EntityManager.DeleteEntity(this);
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetAllComponents()
        {
            return EntityManager.GetComponents(Uid);
        }

        /// <inheritdoc />
        public IEnumerable<T> GetAllComponents<T>()
        {
            return EntityManager.GetComponents<T>(Uid);
        }

        #endregion Components

        /// <inheritdoc />
        public override string ToString()
        {
            return EntityManager.ToPrettyString(Uid);
        }
    }
}
