using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    public sealed class Entity : IEntity
    {
        #region Members

        /// <inheritdoc />
        public IEntityManager EntityManager { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public EntityUid Uid { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public EntityPrototype Prototype
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
        public GameTick LastModifiedTick { get; private set; } = new GameTick(1);

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

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; private set; }

        private ITransformComponent _transform;
        /// <inheritdoc />
        [ViewVariables]
        public ITransformComponent Transform => _transform ?? (_transform = GetComponent<ITransformComponent>());

        private IMetaDataComponent _metaData;
        /// <inheritdoc />
        [ViewVariables]
        public IMetaDataComponent MetaData => _metaData ?? (_metaData = GetComponent<IMetaDataComponent>());

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
            // Initialize() can modify the collection of components.
            var components = EntityManager.ComponentManager.GetComponents(Uid);
            foreach (var t in components)
            {
                var comp = (Component) t;
                if (comp != null && !comp.Initialized)
                    comp.Initialize();
            }
        }

        /// <summary>
        ///     Calls Startup() on all registered components.
        /// </summary>
        public void StartAllComponents()
        {
            // Startup() can modify _components
            // TODO: This code can only handle additions to the list. Is there a better way?
            var components = EntityManager.ComponentManager.GetComponents(Uid);
            foreach (var t in components)
            {
                var comp = (Component)t;
                if (comp != null && comp.Initialized && !comp.Running && !comp.Deleted)
                    comp.Startup();
            }
        }

        /// <summary>
        ///     Sets up the entity into a valid initial state.
        /// </summary>
        public void Initialize()
        {
            Initialized = true;
        }

        #endregion Initialization

        #region Unified Messaging

        /// <inheritdoc />
        public void SendMessage(IComponent owner, ComponentMessage message)
        {
            var components = EntityManager.ComponentManager.GetComponents(Uid);
            foreach (var component in components)
            {
                if (owner != component)
                    component.HandleMessage(message, null, owner);
            }
        }

        /// <inheritdoc />
        public void SendNetworkMessage(IComponent owner, ComponentMessage message, INetChannel channel = null)
        {
            if (message.Directed)
            {
                EntityManager.EntityNetManager.SendDirectedComponentNetworkMessage(channel, this, owner, message);
            }
            else
            {
                EntityManager.EntityNetManager.SendComponentNetworkMessage(this, owner, message);
            }
        }

        #endregion Unified Messaging

        #region Network messaging

        /// <inheritdoc />
        public void HandleNetworkMessage(IncomingEntityMessage message)
        {
            switch (message.Message.Type)
            {
                case EntityMessageType.ComponentMessage:
                    {
                        var compMsg = message.Message.ComponentMessage;
                        var compChannel = message.Message.MsgChannel;
                        compMsg.Remote = true;

                        if (compMsg.Directed)
                        {
                            if (EntityManager.ComponentManager.TryGetComponent(Uid, message.Message.NetId, out var component))
                                component.HandleMessage(compMsg, compChannel);
                        }
                        else
                        {
                            foreach (var component in EntityManager.ComponentManager.GetComponents(Uid))
                            {
                                component.HandleMessage(compMsg, compChannel);
                            }
                        }
                    }
                    break;
            }
        }

        #endregion Network messaging

        #region IEntity Members

        #region Component Events

        /// <inheritdoc />
        public void SubscribeEvent<T>(EntityEventHandler<EntityEventArgs> evh, IEntityEventSubscriber s)
            where T : EntityEventArgs
        {
            EntityManager.SubscribeEvent<T>(evh, s);
        }

        /// <inheritdoc />
        public void UnsubscribeEvent<T>(IEntityEventSubscriber s)
            where T : EntityEventArgs
        {
            EntityManager.UnsubscribeEvent<T>(s);
        }

        /// <inheritdoc />
        public void RaiseEvent(EntityEventArgs toRaise)
        {
            EntityManager.RaiseEvent(this, toRaise);
        }

        #endregion Component Events

        #endregion IEntity Members

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

        private void AddComponent(Component component, bool overwrite)
        {
            EntityManager.ComponentManager.AddComponent(this, component, overwrite);
        }

        /// <inheritdoc />
        public void RemoveComponent<T>()
        {
            EntityManager.ComponentManager.RemoveComponent<T>(Uid);
        }

        private void RemoveComponent(IComponent component)
        {
            EntityManager.ComponentManager.RemoveComponent(Uid, component);
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

        private bool HasComponent(uint netId)
        {
            return EntityManager.ComponentManager.HasComponent(Uid, netId);
        }

        /// <inheritdoc />
        public T GetComponent<T>()
        {
            return (T)EntityManager.ComponentManager.GetComponent(Uid, typeof(T));
        }

        /// <inheritdoc />
        public IComponent GetComponent(Type type)
        {
            return EntityManager.ComponentManager.GetComponent(Uid, type);
        }

        /// <inheritdoc />
        public IComponent GetComponent(uint netId)
        {
            return EntityManager.ComponentManager.GetComponent(Uid, netId);
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>(out T component)
            where T : class
        {
            return EntityManager.ComponentManager.TryGetComponent(Uid, out component);
        }

        /// <inheritdoc />
        public bool TryGetComponent(Type type, out IComponent component)
        {
            return EntityManager.ComponentManager.TryGetComponent(Uid, type, out component);
        }

        /// <inheritdoc />
        public bool TryGetComponent(uint netId, out IComponent component)
        {
            return EntityManager.ComponentManager.TryGetComponent(Uid, netId, out component);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            EntityManager.ComponentManager.RemoveComponents(Uid);

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

        private readonly Dictionary<uint, (ComponentState curState, ComponentState nextState)> _compStateWork
            = new Dictionary<uint, (ComponentState curState, ComponentState nextState)>();

        /// <summary>
        ///     Applies an entity state to this entity.
        /// </summary>
        /// <param name="curState">State to apply.</param>
        internal void HandleEntityState(EntityState curState, EntityState nextState)
        {
            _compStateWork.Clear();

            if(curState?.ComponentChanges != null)
            {
                foreach (var compChange in curState.ComponentChanges)
                {
                    if (compChange.Deleted)
                    {
                        if (TryGetComponent(compChange.NetID, out var comp))
                        {
                            RemoveComponent(comp);
                        }
                    }
                    else
                    {
                        if (HasComponent(compChange.NetID))
                            continue;

                        var newComp = (Component) IoCManager.Resolve<IComponentFactory>().GetComponent(compChange.ComponentName);
                        newComp.Owner = this;
                        AddComponent(newComp, true);
                    }
                }
            }

            if(curState?.ComponentStates != null)
            {
                foreach (var compState in curState.ComponentStates)
                {
                    _compStateWork[compState.NetID] = (compState, null);
                }
            }

            if(nextState?.ComponentStates != null)
            {
                foreach (var compState in nextState.ComponentStates)
                {
                    if (_compStateWork.TryGetValue(compState.NetID, out var state))
                    {
                        _compStateWork[compState.NetID] = (state.curState, compState);
                    }
                    else
                    {
                        _compStateWork[compState.NetID] = (null, compState);
                    }
                }
            }

            foreach (var kvStates in _compStateWork)
            {
                if (TryGetComponent(kvStates.Key, out var component))
                {
                    if (kvStates.Value.curState == null || kvStates.Value.curState.GetType() == component.StateType)
                    {
                        component.HandleComponentState(kvStates.Value.curState, kvStates.Value.nextState);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Incorrect component state type: {component.StateType}, component: {component.GetType()}");
                    }
                }
            }
        }

        /// <inheritdoc />
        public EntityState GetEntityState(GameTick fromTick)
        {
            var compStates = GetComponentStates(fromTick);

            var changed = new List<ComponentChanged>();

            foreach (var c in EntityManager.ComponentManager.GetNetComponents(Uid))
            {
                if (c.CreationTick >= fromTick && !c.Deleted)
                {
                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChanged.Added(c.NetID.Value, c.Name));
                }
                else if (c.Deleted && c.LastModifiedTick >= fromTick)
                {
                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChanged.Removed(c.NetID.Value));
                }
            }

            var es = new EntityState(Uid, changed, compStates);
            return es;
        }

        /// <summary>
        ///     Server-side method to get the state of all our components
        /// </summary>
        /// <returns></returns>
        private List<ComponentState> GetComponentStates(GameTick fromTick)
        {
            var list = new List<ComponentState>();
            foreach (var component in GetAllComponents())
            {
                if (component.NetID == null || !component.NetSyncEnabled || component.LastModifiedTick < fromTick)
                {
                    continue;
                }

                list.Add(component.GetComponentState());
            }

            return list;
        }

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
            return $"{Name} ({Uid}, {Prototype.ID})";
        }
    }
}
