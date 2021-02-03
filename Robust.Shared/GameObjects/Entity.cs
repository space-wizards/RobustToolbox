using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.GameObjects.Components;
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
        public IEntityManager EntityManager { get; private set; } = default!;

        /// <inheritdoc />
        [ViewVariables]
        public EntityUid Uid { get; private set; }

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
        [ViewVariables]
        // Every entity starts at tick 1, because they are conceptually created in the time between 0->1
        public GameTick LastModifiedTick { get; private set; } = new(1);

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public string Name
        {
            get => MetaData.EntityName;
            set => MetaData.EntityName = value;
        }

        /// <inheritdoc />
        [ViewVariables]
        public bool Initialized { get; private set; }

        [ViewVariables]
        public bool Initializing
        {
            get => _initializing;
            private set
            {
                _initializing = value;
                if (value)
                {
                    EntityManager.UpdateEntityTree(this);
                }
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; private set; }

        [ViewVariables]
        public bool Paused
        {
            get => _paused;
            set
            {
                if (_paused == value || value && HasComponent<SharedIgnorePauseComponent>())
                    return;

                _paused = value;
            }
        }

        private bool _paused;

        private ITransformComponent? _transform;

        /// <inheritdoc />
        [ViewVariables]
        public ITransformComponent Transform => _transform ??= GetComponent<ITransformComponent>();

        private IMetaDataComponent? _metaData;

        private bool _initializing;

        /// <inheritdoc />
        [ViewVariables]
        public IMetaDataComponent MetaData => _metaData ??= GetComponent<IMetaDataComponent>();

        #endregion Members

        #region Initialization

        /// <summary>
        ///     Sets fundamental managers after the entity has been created.
        /// </summary>
        /// <remarks>
        ///     This is a separate method because C# makes constructors painful.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the method is called and the entity already has initialized managers.
        /// </exception>
        public void SetManagers(IEntityManager entityManager)
        {
            if (EntityManager != null)
            {
                throw new InvalidOperationException("Entity already has initialized managers.");
            }

            EntityManager = entityManager;
        }

        /// <inheritdoc />
        public bool IsValid()
        {
            return !Deleted;
        }

        /// <summary>
        ///     Initialize the entity's UID. This can only be called once.
        /// </summary>
        /// <param name="uid">The new UID.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the method is called and the entity already has a UID.
        /// </exception>
        public void SetUid(EntityUid uid)
        {
            if (!uid.IsValid())
                throw new ArgumentException("Uid is not valid.", nameof(uid));

            if (Uid.IsValid())
                throw new InvalidOperationException("Entity already has a UID.");

            Uid = uid;
        }

        /// <summary>
        ///     Calls Initialize() on all registered components.
        /// </summary>
        public void InitializeComponents()
        {
            Initializing = true;
            // Initialize() can modify the collection of components.
            var components = EntityManager.ComponentManager.GetComponents(Uid)
                .OrderBy(x => x switch
                {
                    ITransformComponent _ => 0,
                    IPhysicsComponent _ => 1,
                    _ => int.MaxValue
                });

            foreach (var comp in components)
            {
                if (comp == null || comp.Initialized) continue;

                comp.Initialize();

                DebugTools.Assert(comp.Initialized, $"Component {comp.Name} did not call base {nameof(comp.Initialize)} in derived method.");
            }

#if DEBUG
            // Second integrity check in case of.
            foreach (var t in EntityManager.ComponentManager.GetComponents(Uid))
            {
                DebugTools.Assert(t.Initialized, $"Component {t.Name} was not initialized at the end of {nameof(InitializeComponents)}.");
            }

#endif
            Initialized = true;
            Initializing = false;
            EntityManager.EventBus.RaiseEvent(EventSource.Local, new EntityInitializedMessage(this));
        }

        /// <summary>
        ///     Calls Startup() on all registered components.
        /// </summary>
        public void StartAllComponents()
        {
            // Startup() can modify _components
            var compMan = EntityManager.ComponentManager;

            // This code can only handle additions to the list. Is there a better way? Probably not.
            var comps = compMan.GetComponents(Uid)
                .OrderBy(x => x switch
                {
                    ITransformComponent _ => 0,
                    IPhysicsComponent _ => 1,
                    _ => int.MaxValue
                });

            foreach (var comp in comps)
            {
                if (comp != null && comp.Initialized && !comp.Deleted)
                {
                    comp.Running = true;
                }
            }

            EntityManager.UpdateEntityTree(this);
        }

        #endregion Initialization

        #region Component Messaging

        /// <inheritdoc />
        public void SendMessage(IComponent? owner, ComponentMessage message)
        {
            var components = EntityManager.ComponentManager.GetComponents(Uid);
            foreach (var component in components)
            {
                if (owner != component)
                    component.HandleMessage(message, owner);
            }
        }

        /// <inheritdoc />
        public void SendNetworkMessage(IComponent owner, ComponentMessage message, INetChannel? channel = null)
        {
            EntityManager.EntityNetManager.SendComponentNetworkMessage(channel, this, owner, message);
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
            EntityManager.ComponentManager.AddComponent(this, component);
        }

        /// <inheritdoc />
        public T AddComponent<T>()
            where T : Component, new()
        {
            return EntityManager.ComponentManager.AddComponent<T>(this);
        }

        /// <inheritdoc />
        public void RemoveComponent<T>()
        {
            EntityManager.ComponentManager.RemoveComponent<T>(Uid);
        }

        /// <inheritdoc />
        public bool HasComponent<T>()
        {
            return EntityManager.ComponentManager.HasComponent(Uid, typeof(T));
        }

        /// <inheritdoc />
        public bool HasComponent(Type type)
        {
            return EntityManager.ComponentManager.HasComponent(Uid, type);
        }

        /// <inheritdoc />
        public T GetComponent<T>()
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return (T)EntityManager.ComponentManager.GetComponent(Uid, typeof(T));
        }

        /// <inheritdoc />
        public IComponent GetComponent(Type type)
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.ComponentManager.GetComponent(Uid, type);
        }

        /// <inheritdoc />
        public IComponent GetComponent(uint netId)
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.ComponentManager.GetComponent(Uid, netId);
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : class
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.ComponentManager.TryGetComponent(Uid, out component);
        }

        public T? GetComponentOrNull<T>() where T : class
        {
            return TryGetComponent(out T? component) ? component : default;
        }

        /// <inheritdoc />
        public bool TryGetComponent(Type type, [NotNullWhen(true)] out IComponent? component)
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.ComponentManager.TryGetComponent(Uid, type, out component);
        }

        public IComponent? GetComponentOrNull(Type type)
        {
            return TryGetComponent(type, out var component) ? component : null;
        }

        /// <inheritdoc />
        public bool TryGetComponent(uint netId, [NotNullWhen(true)] out IComponent? component)
        {
            DebugTools.Assert(!Deleted, "Tried to get component on a deleted entity.");

            return EntityManager.ComponentManager.TryGetComponent(Uid, netId, out component);
        }

        public IComponent? GetComponentOrNull(uint netId)
        {
            return TryGetComponent(netId, out var component) ? component : null;
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            EntityManager.ComponentManager.DisposeComponents(Uid);

            // Entity manager culls us because we're set to Deleted.
            Deleted = true;
        }

        /// <inheritdoc />
        public void Delete()
        {
            EntityManager.DeleteEntity(this);
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetAllComponents()
        {
            return EntityManager.ComponentManager.GetComponents(Uid);
        }

        /// <inheritdoc />
        public IEnumerable<T> GetAllComponents<T>()
        {
            return EntityManager.ComponentManager.GetComponents<T>(Uid);
        }

        #endregion Components

        #region GameState

        /// <summary>
        ///     Marks this entity as dirty so that the networking will sync it with clients.
        /// </summary>
        public void Dirty()
        {
            LastModifiedTick = EntityManager.CurrentTick;
        }

        #endregion GameState

        /// <inheritdoc />
        public override string ToString()
        {
            if (Deleted)
            {
                return $"{Name} ({Uid}, {Prototype?.ID})D";
            }
            return $"{Name} ({Uid}, {Prototype?.ID})";
        }
    }
}
