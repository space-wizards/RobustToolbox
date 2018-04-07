using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.GameObjects.Serialization;

namespace SS14.Shared.GameObjects
{
    /// <inheritdoc />
    public sealed class Entity : IEntity
    {
        #region Members

        /// <summary>
        /// Holds this entity's components. Indexed by reference type. As such the values will contain duplicates.
        /// </summary>
        private readonly Dictionary<Type, IComponent> _componentReferences = new Dictionary<Type, IComponent>();
        private readonly Dictionary<uint, IComponent> _netIDs = new Dictionary<uint, IComponent>();
        private readonly List<IComponent> _components = new List<IComponent>();
        private string _name;
        private string _type = "entity";
        private string _id;

        /// <inheritdoc />
        public IEntityNetworkManager EntityNetworkManager { get; private set; }

        /// <inheritdoc />
        public IEntityManager EntityManager { get; private set; }

        /// <inheritdoc />
        public EntityUid Uid { get; private set; }

        /// <inheritdoc />
        public EntityPrototype Prototype { get; set; }

        /// <inheritdoc />
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <inheritdoc />
        public bool Initialized { get; private set; }

        /// <inheritdoc />
        public bool Deleted { get; private set; }

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
        public void SetManagers(IEntityManager entityManager, IEntityNetworkManager networkManager)
        {
            if (EntityManager != null)
            {
                throw new InvalidOperationException("Entity already has initialized managers.");
            }
            EntityManager = entityManager;
            EntityNetworkManager = networkManager;
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
            if(!uid.IsValid())
                throw new ArgumentException("Uid is not valid.", nameof(uid));

            if(Uid.IsValid())
                throw new InvalidOperationException("Entity already has a UID.");

            Uid = uid;
        }
        
        /// <summary>
        ///     Calls Initialize() on all registered components.
        /// </summary>
        public void InitializeComponents()
        {
            // Initialize() can modify _components.
            // TODO: This code can only handle additions to the list. Is there a better way?
            for (int i = 0; i < _components.Count; i++)
            {
                _components[i].Initialize();
            }
        }

        /// <summary>
        ///     Calls Startup() on all registered components.
        /// </summary>
        public void StartAllComponents()
        {
            // Startup() can modify _components
            // TODO: This code can only handle additions to the list. Is there a better way?
            for (int i = 0; i < _components.Count; i++)
            {
                _components[i].Startup();
            }
        }

        /// <summary>
        ///     Called after the entity is constructed by its prototype to load serializable fields
        ///     from the prototype's <c>data</c> field. Called any time during the Entities life to serialize
        ///     its fields.
        /// </summary>
        /// <param name="serializer">The serialization object that contains the <c>data</c> field.</param>
        public void ExposeData(EntitySerializer serializer)
        {
            _id = Prototype.ID;
            _type = Prototype.TypeString;

            serializer.EntityHeader();

            serializer.DataField(ref _type, "type", "entity", true);
            serializer.DataField(ref _id, "id", String.Empty, true);
            serializer.DataField(ref _name, "name", String.Empty, true);

            serializer.CompHeader();

            foreach (var component in _components)
            {
                string type = component.Name;

                serializer.CompStart(type);

                serializer.DataField(ref type, "type", component.Name, true);

                component.ExposeData(serializer);
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
            foreach (var component in _components)
            {
                if(owner != component)
                    component.HandleMessage(owner, message);
            }
        }

        /// <inheritdoc />
        public void SendNetworkMessage(IComponent owner, ComponentMessage message)
        {
            EntityNetworkManager.SendComponentNetworkMessage(this, owner, message);
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
                        if (_netIDs.TryGetValue(message.Message.NetId, out var component))
                                component.HandleMessage(compChannel, compMsg);
                    }
                    else
                    {
                        foreach (var component in _components)
                        {
                            component.HandleMessage(compChannel, compMsg);
                        }
                    }
                }
                    break;
            }
        }

        #endregion Network messaging

        #region IEntity Members

        /// <inheritdoc />
        public string GetDescriptionString()
        {
            var msg = new DescriptionStringMsg();
            SendMessage(null, msg);
            return msg.DescriptionString;
        }

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

        #region Entity Systems

        /// <inheritdoc />
        public bool Match(IEntityQuery query)
        {
            return query.Match(this);
        }

        #endregion Entity Systems

        #region Components

        /// <summary>
        ///     Public method to add a component to an entity.
        ///     Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddComponent(Component component)
        {
            AddComponent(component, false);
        }

        /// <inheritdoc />
        public T AddComponent<T>()
            where T : Component, new()
        {
            var factory = IoCManager.Resolve<IComponentFactory>();
            var newComponent = (Component)factory.GetComponent<T>();
            
            newComponent.Owner = this;
            AddComponent(newComponent);

            return (T)newComponent;
        }

        private void AddComponent(Component component, bool overwrite)
        {
            if (component.Owner != null && component.Owner != this)
            {
                throw new ArgumentException("Component already has an owner");
            }
            var reg = IoCManager.Resolve<IComponentFactory>().GetRegistration(component);

            // Check that there are no overlapping references.
            foreach (var type in reg.References)
            {
                if (!_componentReferences.TryGetValue(type, out var duplicate))
                    continue;

                if (!overwrite)
                    throw new InvalidOperationException($"Component reference type {type} already occupied by {duplicate}");

                RemoveComponent(type);
            }

            _components.Add(component);
            foreach (var type in reg.References)
            {
                _componentReferences[type] = component;
            }

            if (component.NetID != null)
            {
                _netIDs[component.NetID.Value] = component;
            }

            // Register the component with the ComponentManager.
            var manager = IoCManager.Resolve<IComponentManager>();
            manager.AddComponent(component);

            component.OnAdd();

            if (Initialized)
            {
                // If the component gets added AFTER primary entity initialization (prototype),
                // we initialize it here!
                component.Initialize();
            }
        }

        /// <summary>
        ///     Public method to remove a component from an entity.
        ///     Calls the onRemove method of the component, which handles removing it
        ///     from the component manager and shutting down the component.
        /// </summary>
        /// <param name="component">The component to remove.</param>
        public void RemoveComponent(IComponent component)
        {
            if (component.Owner != this)
            {
                throw new InvalidOperationException("Component is not owned by us");
            }

            component.Shutdown();

            InternalRemoveComponent(component);
        }

        private void RemoveComponent(Type type)
        {
            RemoveComponent(GetComponent(type));
        }

        /// <inheritdoc />
        public void RemoveComponent<T>()
        {
            RemoveComponent((IComponent)GetComponent<T>());
        }

        private void InternalRemoveComponent(IComponent component)
        {
            if (component.Owner != this)
                throw new InvalidOperationException("Component is not owned by us");

            var reg = IoCManager.Resolve<IComponentFactory>().GetRegistration(component);

            EntityManager.RemoveSubscribedEvents(component);
            component.OnRemove();
            _components.Remove(component);

            foreach (var t in reg.References)
            {
                _componentReferences.Remove(t);
            }

            if (component.NetID != null)
                _netIDs.Remove(component.NetID.Value);
        }

        /// <inheritdoc />
        public bool HasComponent<T>()
        {
            return HasComponent(typeof(T));
        }

        /// <inheritdoc />
        public bool HasComponent(Type type)
        {
            return _componentReferences.ContainsKey(type);
        }

        private bool HasComponent(uint netId)
        {
            return _netIDs.ContainsKey(netId);
        }

        /// <inheritdoc />
        public T GetComponent<T>()
        {
            return (T)_componentReferences[typeof(T)];
        }

        /// <inheritdoc />
        public IComponent GetComponent(Type type)
        {
            return _componentReferences[type];
        }

        /// <inheritdoc />
        public IComponent GetComponent(uint netId)
        {
            return _netIDs[netId];
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>(out T component)
            where T : class
        {
            if (!_componentReferences.ContainsKey(typeof(T)))
            {
                component = null;
                return false;
            }
            component = (T) _componentReferences[typeof(T)];
            return true;
        }

        /// <inheritdoc />
        public bool TryGetComponent(Type type, out IComponent component)
        {
            return _componentReferences.TryGetValue(type, out component);
        }

        /// <inheritdoc />
        public bool TryGetComponent(uint netId, out IComponent component)
        {
            return _netIDs.TryGetValue(netId, out component);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            // first we shut down every component.
            foreach (var component in _components)
            {
                component.Shutdown();
            }

            // then we remove every component.
            foreach (var component in _components.ToList())
            {
                InternalRemoveComponent(component);
            }

            // Entity manager culls us because we're set to Deleted.
            Deleted = true;
            _netIDs.Clear();
            _componentReferences.Clear();
        }

        /// <inheritdoc />
        public void Delete()
        {
            EntityManager.DeleteEntity(this);
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetComponents()
        {
            return _components.Where(component => !component.Deleted);
        }

        /// <inheritdoc />
        public IEnumerable<T> GetComponents<T>()
        {
            return _components.OfType<T>();
        }

        #endregion Components

        #region GameState

        /// <inheritdoc />
        public void HandleEntityState(EntityState state)
        {
            Name = state.StateData.Name;
            var synchedComponentTypes = state.StateData.SynchedComponentTypes;
            foreach (var t in synchedComponentTypes)
            {
                if (HasComponent(t.Item1) && GetComponent(t.Item1).Name != t.Item2)
                    RemoveComponent(GetComponent(t.Item1));

                if (!HasComponent(t.Item1))
                    AddComponent((Component)IoCManager.Resolve<IComponentFactory>().GetComponent(t.Item2), true);
            }

            foreach (var compState in state.ComponentStates)
            {
                compState.ReceivedTime = state.ReceivedTime;

                if (!TryGetComponent(compState.NetID, out var component))
                    continue;

                if (compState.GetType() != component.StateType)
                    throw new InvalidOperationException($"Incorrect component state type: {component.StateType}, component: {component.GetType()}");

                component.HandleComponentState(compState);
            }
        }

        /// <inheritdoc />
        public EntityState GetEntityState()
        {
            var compStates = GetComponentStates();
            var synchedComponentTypes = _netIDs
                .Where(t => t.Value.NetworkSynchronizeExistence)
                .Select(t => new Tuple<uint, string>(t.Key, t.Value.Name))
                .ToList();

            var es = new EntityState(
                Uid,
                compStates,
                Prototype.ID,
                Name,
                synchedComponentTypes);
            return es;
        }

        /// <summary>
        ///     Server-side method to get the state of all our components
        /// </summary>
        /// <returns></returns>
        private List<ComponentState> GetComponentStates()
        {
            return GetComponents()
                .Where(c => c.NetID != null)
                .Select(component => component.GetComponentState())
                .ToList();
        }

        #endregion GameState Stuff
    }
}
