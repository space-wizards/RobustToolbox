using Lidgren.Network;
using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GameObjects
{
    public delegate void EntityShutdownEvent(Entity e);

    public interface IEntity
    {
        string Name { get; set; }
        EntityManager EntityManager { get; }
        int Uid { get; set; }

        bool Initialized { get; set; }

        EntityPrototype Prototype { get; set; }
        event EntityShutdownEvent OnShutdown;

        /// <summary>
        /// Match
        ///
        /// Allows us to fetch entities with a defined SET of components
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        bool Match(EntityQuery query);

        /// <summary>
        /// Public method to add a component to an entity.
        /// Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="family">the family of component -- there can only be one at a time per family.</param>
        /// <param name="component">The component.</param>
        void AddComponent(ComponentFamily family, IComponent component);

        /// <summary>
        /// Public method to remove a component from an entity.
        /// Calls the onRemove method of the component, which handles removing it
        /// from the component manager and shutting down the component.
        /// </summary>
        /// <param name="family"></param>
        void RemoveComponent(ComponentFamily family);

        /// <summary>
        /// Checks to see if a component of a certain family exists
        /// </summary>
        /// <param name="family">componentfamily to check</param>
        /// <returns>true if component exists, false otherwise</returns>
        bool HasComponent(ComponentFamily family);

        T GetComponent<T>(ComponentFamily family) where T : class;

        /// <summary>
        /// Gets the component of the specified family, if it exists
        /// </summary>
        /// <param name="family">componentfamily to get</param>
        /// <returns></returns>
        IComponent GetComponent(ComponentFamily family);

        void Shutdown();
        List<IComponent> GetComponents();
        List<ComponentFamily> GetComponentFamilies();
        void SendMessage(object sender, ComponentMessageType type, params object[] args);

        /// <summary>
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies,
                         params object[] args);

        ComponentReplyMessage SendMessage(object sender, ComponentFamily family, ComponentMessageType type,
                                          params object[] args);

        /// <summary>
        /// Requests Description string from components and returns it. If no component answers, returns default description from template.
        /// </summary>
        string GetDescriptionString() //This needs to go here since it can not be bound to any single component.
            ;

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="messageParams">Parameters</param>
        void SendComponentNetworkMessage(Component component, NetDeliveryMethod method, params object[] messageParams);

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="recipient">The intended recipient netconnection (if null send to all)</param>
        /// <param name="messageParams">Parameters</param>
        void SendDirectedComponentNetworkMessage(Component component, NetDeliveryMethod method,
                                                 NetConnection recipient, params object[] messageParams);

        /// <summary>
        /// Client message to server saying component has been instantiated and needs initial data
        /// </summary>
        /// <param name="component"></param>
        [Obsolete("Getting rid of this messaging paradigm")]
        void SendComponentInstantiationMessage(Component component);

        /// <summary>
        /// Sets up variables and shite
        /// </summary>
        void Initialize();

        void HandleNetworkMessage(IncomingEntityMessage message);

        /// <summary>
        /// Client method to handle an entity state object
        /// </summary>
        /// <param name="state"></param>
        void HandleEntityState(EntityState state);

        /// <summary>
        /// Serverside method to prepare an entity state object
        /// </summary>
        /// <returns></returns>
        EntityState GetEntityState();
    }

    public class Entity : IEntity
    {
        #region Members

        /// <summary>
        /// Holds this entity's components
        /// </summary>
        private readonly Dictionary<ComponentFamily, IComponent> _components =
            new Dictionary<ComponentFamily, IComponent>();

        protected List<Type> ComponentTypes = new List<Type>();
        protected IEntityNetworkManager EntityNetworkManager;

        public int Uid { get; set; }
        public EntityPrototype Prototype { get; set; }
        public string Name { get; set; }

        public bool Initialized { get; set; }
        public event EntityShutdownEvent OnShutdown;

        #endregion

        #region constructor

        public Entity(EntityManager entityManager)
        {
            EntityManager = entityManager;
            EntityNetworkManager = EntityManager.EntityNetworkManager;
            if (EntityManager.EngineType == EngineType.Client)
                Initialize();
        }

        public EntityManager EntityManager { get; private set; }

        #endregion

        #region Initialization

        /// <summary>
        /// Sets up variables and shite
        /// </summary>
        public virtual void Initialize()
        {
            SendMessage(this, ComponentMessageType.Initialize);
            Initialized = true;
        }

        #endregion

        #region Component Messaging

        public void SendMessage(object sender, ComponentMessageType type, params object[] args)
        {
            //LogComponentMessage(sender, type, args);

            foreach (Component component in GetComponents())
            {
                if (_components.ContainsValue(component))
                    //Check to see if the component is still a part of this entity --- collection may change in process.
                    component.RecieveMessage(sender, type, args);
            }
        }

        /// <summary>
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        public void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies,
                                params object[] args)
        {
            //LogComponentMessage(sender, type, args);

            foreach (Component component in GetComponents())
            {
                //Check to see if the component is still a part of this entity --- collection may change in process.
                if (_components.ContainsValue(component))
                {
                    if (replies != null)
                    {
                        ComponentReplyMessage reply = component.RecieveMessage(sender, type, args);
                        if (reply.MessageType != ComponentMessageType.Empty)
                            replies.Add(reply);
                    }
                    else
                        component.RecieveMessage(sender, type, args);
                }
            }
        }

        public ComponentReplyMessage SendMessage(object sender, ComponentFamily family, ComponentMessageType type,
                                                 params object[] args)
        {
            //LogComponentMessage(sender, type, args);

            if (HasComponent(family))
                return GetComponent<Component>(family).RecieveMessage(sender, type, args);

            return ComponentReplyMessage.Empty;
        }

        protected void HandleComponentMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (GetComponentFamilies().Contains(message.ComponentFamily))
            {
                GetComponent<Component>(message.ComponentFamily).HandleNetworkMessage(message, client);
            }
        }

        #endregion

        #region Network messaging

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="messageParams">Parameters</param>
        public void SendComponentNetworkMessage(Component component, NetDeliveryMethod method,
                                                params object[] messageParams)
        {
            EntityNetworkManager.SendComponentNetworkMessage(this, component.Family, NetDeliveryMethod.ReliableUnordered,
                                                             messageParams);
        }

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="recipient">The intended recipient netconnection (if null send to all)</param>
        /// <param name="messageParams">Parameters</param>
        public void SendDirectedComponentNetworkMessage(Component component, NetDeliveryMethod method,
                                                        NetConnection recipient, params object[] messageParams)
        {
            if (!Initialized)
                return;
            EntityNetworkManager.SendDirectedComponentNetworkMessage(this, component.Family,
                                                                     method, recipient,
                                                                     messageParams);
        }

        /// <summary>
        /// Client message to server saying component has been instantiated and needs initial data
        /// </summary>
        /// <param name="component"></param>
        [Obsolete("Getting rid of this messaging paradigm")]
        public void SendComponentInstantiationMessage(Component component)
        {
            if (EntityManager.EngineType == EngineType.Server)
                return;
            if (component == null)
                throw new Exception("Component is null");

            EntityNetworkManager.SendEntityNetworkMessage(this, EntityMessage.ComponentInstantiationMessage,
                                                          component.Family);
        }

        /// <summary>
        /// Func to handle an incoming network message
        /// </summary>
        /// <param name="message"></param>
        public virtual void HandleNetworkMessage(IncomingEntityMessage message)
        {
            switch (message.MessageType)
            {
                case EntityMessage.ComponentMessage:
                    HandleComponentMessage((IncomingEntityComponentMessage) message.Message, message.Sender);
                    break;
                case EntityMessage.ComponentInstantiationMessage: //Server Only
                    HandleComponentInstantiationMessage(message);
                    break;
            }
        }

        /// <summary>
        /// Server-side method to handle instantiation messages from client-side components
        /// asking for initialization data
        /// </summary>
        /// <param name="message">Message from client</param>
        protected void HandleComponentInstantiationMessage(IncomingEntityMessage message)
        {
            if (HasComponent((ComponentFamily) message.Message))
                GetComponent<Component>((ComponentFamily) message.Message).HandleInstantiationMessage(message.Sender);
        }

        #endregion

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

        #endregion

        #region Entity Systems

        /// <summary>
        /// Match
        ///
        /// Allows us to fetch entities with a defined SET of components
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool Match(EntityQuery query)
        {
            // Empty queries always result in a match - equivalent to SELECT * FROM ENTITIES
            if (!(query.Exclusionset.Any() || query.OneSet.Any() || query.AllSet.Any()))
                return true;

            //If there is an EXCLUDE set, and the entity contains any component types in that set, or subtypes of them, the entity is excluded.
            bool matched =
                !(query.Exclusionset.Any() && query.Exclusionset.Any(t => ComponentTypes.Any(t.IsAssignableFrom)));

            //If there are no matching exclusions, and the entity matches the ALL set, the entity is included
            if (matched && query.AllSet.Any() && query.AllSet.Any(t => !ComponentTypes.Any(t.IsAssignableFrom)))
                matched = false;
            //If the entity matches so far, and it matches the ONE set, it matches.
            if (matched && query.OneSet.Any() && !query.OneSet.Any(t => ComponentTypes.Any(t.IsAssignableFrom)))
                matched = false;
            return matched;
        }

        #endregion

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
        #endregion

        #region Components

        /// <summary>
        /// Public method to add a component to an entity.
        /// Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="family">the family of component -- there can only be one at a time per family.</param>
        /// <param name="component">The component.</param>
        public void AddComponent(ComponentFamily family, IComponent component)
        {
            if (_components.Keys.Contains(family))
                RemoveComponent(family);
            _components.Add(family, component);
            component.OnAdd(this);
            UpdateComponentTypes();
        }

        /// <summary>
        /// Public method to remove a component from an entity.
        /// Calls the onRemove method of the component, which handles removing it
        /// from the component manager and shutting down the component.
        /// </summary>
        /// <param name="family"></param>
        public void RemoveComponent(ComponentFamily family)
        {
            if (!_components.Keys.Contains(family)) return;
            UpdateComponentTypes();
            EntityManager.RemoveSubscribedEvents(_components[family]);
            _components[family].OnRemove();
            _components.Remove(family);
        }

        /// <summary>
        /// Checks to see if a component of a certain family exists
        /// </summary>
        /// <param name="family">componentfamily to check</param>
        /// <returns>true if component exists, false otherwise</returns>
        public bool HasComponent(ComponentFamily family)
        {
            return _components.ContainsKey(family);
        }

        public T GetComponent<T>(ComponentFamily family) where T : class
        {
            if (GetComponent(family) is T)
                return (T) GetComponent(family);
            return null;
        }

        /// <summary>
        /// Gets the component of the specified family, if it exists
        /// </summary>
        /// <param name="family">componentfamily to get</param>
        /// <returns></returns>
        public IComponent GetComponent(ComponentFamily family)
        {
            return _components.ContainsKey(family) ? _components[family] : null;
        }

        public virtual void Shutdown()
        {
            foreach (IComponent component in _components.Values)
            {
                component.Shutdown();
            }
            _components.Clear();
            ComponentTypes.Clear();
        }

        public List<IComponent> GetComponents()
        {
            return _components.Values.ToList();
        }

        public List<ComponentFamily> GetComponentFamilies()
        {
            return _components.Keys.ToList();
        }

        protected void UpdateComponentTypes()
        {
            ComponentTypes = _components.Values.Select(t => t.GetType()).ToList();
        }

        #endregion

        #region GameState Stuff

        /// <summary>
        /// Client method to handle an entity state object
        /// </summary>
        /// <param name="state"></param>
        public void HandleEntityState(EntityState state)
        {
            /*if(Position.X != state.StateData.Position.X || Position.Y != state.StateData.Position.Y)
            {
                Position = state.StateData.Position;
                Moved();
            }*/
            Name = state.StateData.Name;
            var synchedComponentTypes = state.StateData.SynchedComponentTypes;
            foreach(var t in synchedComponentTypes)
            {
                if(HasComponent(t.Item1) && GetComponent(t.Item1).GetType().Name != t.Item2)
                    RemoveComponent(t.Item1);

                if(!HasComponent(t.Item1))
                    AddComponent(t.Item1, EntityManager.ComponentFactory.GetComponent(t.Item2));
            }
            foreach (ComponentState compState in state.ComponentStates)
            {
                compState.ReceivedTime = state.ReceivedTime;
                if (HasComponent(compState.Family))
                {
                    IComponent comp = GetComponent(compState.Family);
                    Type stateType = comp.StateType;
                    if (compState.GetType() == stateType)
                    {
                        comp.HandleComponentState(compState);
                    }
                }
            }
        }

        /// <summary>
        /// Serverside method to prepare an entity state object
        /// </summary>
        /// <returns></returns>
        public EntityState GetEntityState()
        {
            List<ComponentState> compStates = GetComponentStates();

            List<Tuple<ComponentFamily, string>> synchedComponentTypes = _components
                .Where(t => EntityManager.SynchedComponentTypes.Contains(t.Key))
                .Select(
                    t => new Tuple<ComponentFamily, string>(t.Key, t.Value.Name)
                ).ToList();

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
            foreach (Component component in GetComponents())
            {
                ComponentState componentState = component.GetComponentState();
                stateComps.Add(componentState);
            }
            return stateComps;
        }

        #endregion
    }
}
