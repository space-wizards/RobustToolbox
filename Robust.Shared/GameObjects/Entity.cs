using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Physics;
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

        private EntityLifeStage _lifeStage;

        /// <inheritdoc cref="IEntity.LifeStage" />
        [ViewVariables]
        internal EntityLifeStage LifeStage
        {
            get => _lifeStage;
            set => _lifeStage = value;
        }

        /// <inheritdoc />
        EntityLifeStage IEntity.LifeStage { get => LifeStage; set => LifeStage = value; }

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
        public bool Initialized => LifeStage >= EntityLifeStage.Initialized;

        /// <inheritdoc />
        public bool Initializing => LifeStage == EntityLifeStage.Initializing;

        /// <inheritdoc />
        public bool Deleted => LifeStage >= EntityLifeStage.Deleted;

        [ViewVariables]
        public bool Paused
        {
            get => _paused;
            set
            {
                if (_paused == value || value && HasComponent<IgnorePauseComponent>())
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

        /// <inheritdoc />
        [ViewVariables]
        public IMetaDataComponent MetaData => _metaData ??= GetComponent<IMetaDataComponent>();

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

        /// <summary>
        ///     Calls Initialize() on all registered components.
        /// </summary>
        public void InitializeComponents()
        {
            DebugTools.Assert(LifeStage == EntityLifeStage.PreInit);
            LifeStage = EntityLifeStage.Initializing;

            // Initialize() can modify the collection of components.
            var components = EntityManager.ComponentManager.GetComponents(Uid)
                .OrderBy(x => x switch
                {
                    ITransformComponent _ => 0,
                    IPhysBody _ => 1,
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
            DebugTools.Assert(LifeStage == EntityLifeStage.Initializing);
            LifeStage = EntityLifeStage.Initialized;
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
                    IPhysBody _ => 1,
                    _ => int.MaxValue
                });

            foreach (var comp in comps)
            {
                if (comp != null && comp.Initialized && !comp.Deleted)
                {
                    comp.Running = true;
                }
            }
        }

        #endregion Initialization

        #region Component Messaging

        /// <inheritdoc />
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
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
            return EntityManager.ComponentManager.HasComponent<T>(Uid);
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
