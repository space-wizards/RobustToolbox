using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.GameObjects.Serialization;

namespace SS14.Shared.GameObjects
{
    public delegate void EntityShutdownEvent(IEntity e);

    public class Entity : IEntity
    {
        #region Members

        /// <summary>
        /// Holds this entity's components. Indexed by reference type. As such the values will contain duplicates.
        /// </summary>
        private readonly Dictionary<Type, IComponent> _componentReferences = new Dictionary<Type, IComponent>();
        private readonly Dictionary<uint, IComponent> _netIDs = new Dictionary<uint, IComponent>();
        private readonly List<IComponent> _components = new List<IComponent>();
        private string _name;
        private EntityPrototype _prototype;
        private string _type = "entity";
        private string _id;

        public IEntityNetworkManager EntityNetworkManager { get; private set; }
        public IEntityManager EntityManager { get; private set; }

        /// <inheritdoc />
        public EntityUid Uid { get; private set; }

        public EntityPrototype Prototype
        {
            get { return _prototype; }
            set { _prototype = value; }
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public bool Initialized { get; set; }

        /// <inheritdoc />
        public bool Deleted { get; private set; }
        public event EntityShutdownEvent OnShutdown;

        #endregion Members

        #region Initialization

        public void SetManagers(IEntityManager entityManager, IEntityNetworkManager networkManager)
        {
            if (EntityManager != null)
            {
                throw new InvalidOperationException("Entity already has initialized managers.");
            }
            EntityManager = entityManager;
            EntityNetworkManager = networkManager;
        }

        public void SetUid(EntityUid uid)
        {
            if(!uid.IsValid())
                throw new ArgumentException("Uid is not valid.", nameof(uid));

            if(Uid.IsValid())
                throw new InvalidOperationException("Entity already has a UID.");

            Uid = uid;
        }

        /// <inheritdoc />
        public virtual void PreInitialize() { }

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

        public virtual void ExposeData(EntitySerializer serializer)
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
        /// Sets up variables and shite
        /// </summary>
        public virtual void Initialize()
        {
            Initialized = true;
        }

        #endregion Initialization

        #region Component Messaging

        public void SendMessage(object sender, ComponentMessageType type, params object[] args)
        {
            foreach (IComponent component in GetComponents())
            {
                if (_components.Contains(component))
                    //Check to see if the component is still a part of this entity --- collection may change in process.
                    component.ReceiveMessage(sender, type, args);
            }
        }

        /// <summary>
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="replies"></param>
        /// <param name="args">message parameters</param>
        public void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies,
                                params object[] args)
        {
            foreach (IComponent component in GetComponents())
            {
                //Check to see if the component is still a part of this entity --- collection may change in process.
                if (_components.Contains(component))
                {
                    if (replies != null)
                    {
                        ComponentReplyMessage reply = component.ReceiveMessage(sender, type, args);
                        if (reply.MessageType != ComponentMessageType.Empty)
                            replies.Add(reply);
                    }
                    else
                        component.ReceiveMessage(sender, type, args);
                }
            }
        }

        protected void HandleComponentMessage(IncomingEntityComponentMessage message)
        {
            if (_netIDs.TryGetValue(message.NetId, out IComponent component))
            {
                component.HandleNetworkMessage(message);
            }
        }

        #endregion Component Messaging

        #region Network messaging

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="messageParams">Parameters</param>
        public void SendComponentNetworkMessage(IComponent component, params object[] messageParams)
        {
            if (component.NetID == null)
            {
                throw new ArgumentException("Component has no Net ID and cannot be used across the network.");
            }
            EntityNetworkManager.SendComponentNetworkMessage(this, component.NetID.Value,
                                                             messageParams);
        }

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="recipient">The intended recipient netconnection (if null send to all)</param>
        /// <param name="messageParams">Parameters</param>
        public void SendDirectedComponentNetworkMessage(IComponent component, INetChannel recipient, params object[] messageParams)
        {
            if (component.NetID == null)
            {
                throw new ArgumentException("Component has no Net ID and cannot be used across the network.");
            }

            if (!Initialized)
            {
                return;
            }

            EntityNetworkManager.SendDirectedComponentNetworkMessage(this, component.NetID.Value, recipient,
                                                                     messageParams);
        }

        /// <summary>
        /// Func to handle an incoming network message
        /// </summary>
        /// <param name="message"></param>
        public virtual void HandleNetworkMessage(IncomingEntityMessage message)
        {
            switch (message.Message.Type)
            {
                case EntityMessageType.ComponentMessage:
                    HandleComponentMessage(new IncomingEntityComponentMessage(message.Message.NetId, message.Message.MsgChannel, message.Message.Parameters));
                    break;
            }
        }

        #endregion Network messaging

        #region IEntity Members

        /// <summary>
        /// Requests Description string from components and returns it. If no component answers, returns default description from template.
        /// </summary>
        public string GetDescriptionString() //This needs to go here since it can not be bound to any single component.
        {
            var replies = new List<ComponentReplyMessage>();

            SendMessage(this, ComponentMessageType.GetDescriptionString, replies);

            if (replies.Any())
                return
                    (string)
                    replies.First(x => x.MessageType == ComponentMessageType.GetDescriptionString).ParamsList[0];
            //If you dont answer with a string then fuck you.

            return null;
        }

        #region Component Events
        //Convenience thing.
        public void SubscribeEvent<T>(EntityEventHandler<EntityEventArgs> evh, IEntityEventSubscriber s) where T : EntityEventArgs
        {
            EntityManager.SubscribeEvent<T>(evh, s);
        }

        public void UnsubscribeEvent<T>(IEntityEventSubscriber s) where T : EntityEventArgs
        {
            EntityManager.UnsubscribeEvent<T>(s);
        }

        public void RaiseEvent(EntityEventArgs toRaise)
        {
            EntityManager.RaiseEvent(this, toRaise);
        }
        #endregion Component Events

        #endregion IEntity Members

        #region Entity Systems

        public bool Match(IEntityQuery query)
        {
            return query.Match(this);
        }

        public virtual void Update(float frameTime)
        {
        }

        #endregion Entity Systems

        #region Components

        /// <summary>
        /// Public method to add a component to an entity.
        /// Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="component">The component.</param>
        public void AddComponent(IComponent component)
        {
            AddComponent(component, overwrite: false);
        }

        private void AddComponent(IComponent component, bool overwrite)
        {
            if (component.Owner != null)
            {
                throw new ArgumentException("Component already has an owner");
            }
            IComponentRegistration reg = IoCManager.Resolve<IComponentFactory>().GetRegistration(component);

            // Check that there are no overlapping references.
            foreach (Type t in reg.References)
            {
                if (_componentReferences.TryGetValue(t, out var duplicate))
                {
                    if (!overwrite)
                    {
                        throw new InvalidOperationException($"Component reference type {t} already occupied by {duplicate}");
                    }
                    else
                    {
                        RemoveComponent(t);
                    }
                }
            }

            _components.Add(component);
            foreach (Type t in reg.References)
            {
                _componentReferences[t] = component;
            }

            if (component.NetID != null)
            {
                _netIDs[component.NetID.Value] = component;
            }

            component.OnAdd(this);

            if (Initialized)
            {
                // If the component gets added AFTER primary entity initialization (prototype),
                // we initialize it here!
                component.Initialize();
            }
        }

        /// <summary>
        /// Public method to remove a component from an entity.
        /// Calls the onRemove method of the component, which handles removing it
        /// from the component manager and shutting down the component.
        /// </summary>
        /// <param name="component">Component to remove.</param>
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

        public void RemoveComponent<T>()
        {
            RemoveComponent((IComponent)GetComponent<T>());
        }

        private void InternalRemoveComponent(IComponent component)
        {
            if (component.Owner != this)
            {
                throw new InvalidOperationException("Component is not owned by us");
            }

            IComponentRegistration reg = IoCManager.Resolve<IComponentFactory>().GetRegistration(component);

            EntityManager.RemoveSubscribedEvents(component);
            component.OnRemove();
            _components.Remove(component);

            foreach (Type t in reg.References)
            {
                _componentReferences.Remove(t);
            }

            if (component.NetID != null)
            {
                _netIDs.Remove(component.NetID.Value);
            }
        }

        public bool HasComponent<T>()
        {
            return HasComponent(typeof(T));
        }

        public bool HasComponent(Type type)
        {
            return _componentReferences.ContainsKey(type);
        }

        public bool HasComponent(uint netId)
        {
            return _netIDs.ContainsKey(netId);
        }

        public T GetComponent<T>()
        {
            return (T)_componentReferences[typeof(T)];
        }

        public IComponent GetComponent(Type type)
        {
            return _componentReferences[type];
        }

        public IComponent GetComponent(uint netId)
        {
            return _netIDs[netId];
        }

        public bool TryGetComponent<T>(out T component) where T : class
        {
            if (!_componentReferences.ContainsKey(typeof(T)))
            {
                component = null;
                return false;
            }
            component = (T)_componentReferences[typeof(T)];
            return true;
        }

        public bool TryGetComponent(Type type, out IComponent component)
        {
            return _componentReferences.TryGetValue(type, out component);
        }

        public bool TryGetComponent(uint netId, out IComponent component)
        {
            return _netIDs.TryGetValue(netId, out component);
        }

        /// <inheritdoc />
        public virtual void Shutdown()
        {
            // first we shut down every component.
            foreach (var component in _components)
            {
                component.Shutdown();
            }

            // then we remove every component.
            foreach (IComponent component in _components.ToList())
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

        public IEnumerable<IComponent> GetComponents()
        {
            return _components.Where(component => !component.Deleted);
        }

        public IEnumerable<T> GetComponents<T>()
        {
            return _components.OfType<T>();
        }

        #endregion Components

        #region GameState Stuff

        /// <summary>
        /// Client method to handle an entity state object
        /// </summary>
        /// <param name="state"></param>
        public void HandleEntityState(EntityState state)
        {
            Name = state.StateData.Name;
            var synchedComponentTypes = state.StateData.SynchedComponentTypes;
            foreach (var t in synchedComponentTypes)
            {
                if (HasComponent(t.Item1) && GetComponent(t.Item1).Name != t.Item2)
                {
                    RemoveComponent(GetComponent(t.Item1));
                }

                if (!HasComponent(t.Item1))
                {
                    AddComponent(IoCManager.Resolve<IComponentFactory>().GetComponent(t.Item2), overwrite: true);
                }
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

        /// <summary>
        /// Serverside method to prepare an entity state object
        /// </summary>
        /// <returns></returns>
        public EntityState GetEntityState()
        {
            List<ComponentState> compStates = GetComponentStates();
            List<Tuple<uint, string>> synchedComponentTypes = _netIDs
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
        /// Server-side method to get the state of all our components
        /// </summary>
        /// <returns></returns>
        private List<ComponentState> GetComponentStates()
        {
            var stateComps = new List<ComponentState>();
            foreach (IComponent component in GetComponents().Where(c => c.NetID != null))
            {
                ComponentState componentState = component.GetComponentState();
                stateComps.Add(componentState);
            }
            return stateComps;
        }

        #endregion GameState Stuff
    }
}
