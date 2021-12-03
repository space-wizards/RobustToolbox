using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [CopyByRef, Serializable]
    public sealed class IEntity
    {
        #region Members

        public IEntityManager EntityManager => IoCManager.Resolve<IEntityManager>();

        [ViewVariables]
        public EntityUid Uid { get; }

        public EntityLifeStage LifeStage
        {
            get => !EntityManager.EntityExists(Uid) ? EntityLifeStage.Deleted : MetaData.EntityLifeStage;
            internal set => MetaData.EntityLifeStage = value;
        }

        [ViewVariables]
        public GameTick LastModifiedTick { get => MetaData.EntityLastModifiedTick; internal set => MetaData.EntityLastModifiedTick = value; }


        [ViewVariables]
        public EntityPrototype? Prototype
        {
            get => MetaData.EntityPrototype;
            internal set => MetaData.EntityPrototype = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string Description
        {
            get => MetaData.EntityDescription;
            set => MetaData.EntityDescription = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string Name
        {
            get => MetaData.EntityName;
            set => MetaData.EntityName = value;
        }

        public bool Initialized => LifeStage >= EntityLifeStage.Initialized;

        public bool Initializing => LifeStage == EntityLifeStage.Initializing;

        public bool Deleted => LifeStage >= EntityLifeStage.Deleted;

        [ViewVariables]
        public bool Paused { get => Deleted || MetaData.EntityPaused; set => MetaData.EntityPaused = value; }

        [ViewVariables]
        public TransformComponent Transform => EntityManager.GetComponent<TransformComponent>(Uid);

        [ViewVariables]
        public MetaDataComponent MetaData => EntityManager.GetComponent<MetaDataComponent>(Uid);

        #endregion Members

        #region Initialization

        public IEntity(EntityUid uid)
        {
            Uid = uid;
        }

        public bool IsValid()
        {
            return EntityManager.EntityExists(Uid);
        }

        #endregion Initialization

        #region Component Messaging

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

        public T AddComponent<T>()
            where T : Component, new()
        {
            return EntityManager.AddComponent<T>(this);
        }

        public void RemoveComponent<T>()
        {
            EntityManager.RemoveComponent<T>(Uid);
        }

        public bool HasComponent<T>()
        {
            return EntityManager.HasComponent<T>(Uid);
        }

        public bool HasComponent(Type type)
        {
            return EntityManager.HasComponent(Uid, type);
        }

        public T GetComponent<T>()
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.GetComponent<T>(Uid);
        }

        public IComponent GetComponent(Type type)
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.GetComponent(Uid, type);
        }

        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : class
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.TryGetComponent(Uid, out component);
        }

        public T? GetComponentOrNull<T>() where T : class
        {
            return TryGetComponent(out T? component) ? component : default;
        }

        public bool TryGetComponent(Type type, [NotNullWhen(true)] out IComponent? component)
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.TryGetComponent(Uid, type, out component);
        }

        public IComponent? GetComponentOrNull(Type type)
        {
            return TryGetComponent(type, out var component) ? component : null;
        }

        public void QueueDelete()
        {
            EntityManager.QueueDeleteEntity(this);
        }

        public void Delete()
        {
            EntityManager.DeleteEntity(this);
        }

        public IEnumerable<IComponent> GetAllComponents()
        {
            return EntityManager.GetComponents(Uid);
        }

        public IEnumerable<T> GetAllComponents<T>()
        {
            return EntityManager.GetComponents<T>(Uid);
        }

        #endregion Components

        public override string ToString()
        {
            return EntityManager.ToPrettyString(Uid);
        }
    }
}
