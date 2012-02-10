using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ClientInterfaces.MessageLogging;
using SS13.IoC;

namespace CGO
{
    /// <summary>
    /// Base entity class. Acts as a container for components, and a place to store location data.
    /// Should not contain any game logic whatsoever other than entity movement functions and 
    /// component management functions.
    /// </summary>
    public class Entity : IEntity
    {
        #region Variables
        /// <summary>
        /// Holds this entity's components
        /// </summary>
        private Dictionary<ComponentFamily, IGameObjectComponent> components = new Dictionary<ComponentFamily, IGameObjectComponent>();

        private EntityNetworkManager m_entityNetworkManager;

        public IEntityTemplate Template { get; set; }

        public string Name { get; set; }

        public event EventHandler<VectorEventArgs> OnMove;

        public bool Initialized { get; set; }

        public int Uid { get; set; }

        /// <summary>
        /// These are the only real pieces of data that the entity should have -- position and rotation.
        /// </summary>
        public Vector2D Position { get; set; }

        public float rotation;
        #endregion

        #region Constructor/Destructor
        /// <summary>
        /// Constructor
        /// </summary>
        public Entity()
        {
            //Initialize();
        }

        /// <summary>
        /// Constructor for realz. This one should be used eventually instead of the naked one.
        /// </summary>
        /// <param name="entityNetworkManager"></param>
        public Entity(EntityNetworkManager entityNetworkManager)
        {
            m_entityNetworkManager = entityNetworkManager;
            Initialize();
        }

        /// <summary>
        /// Sets up variables and shite
        /// </summary>
        public virtual void Initialize()
        {
            SendMessage(this, ComponentMessageType.Initialize, null);
            Initialized = true;
        }

        /// <summary>
        /// Compatibility method for entity. This should be eliminated eventually when the above naked constructor is eliminated.
        /// </summary>
        /// <param name="entityNetworkManager"></param>
        public void InitializeEntityNetworking(EntityNetworkManager entityNetworkManager)
        {
            m_entityNetworkManager = entityNetworkManager;
        }
        
        /// <summary>
        /// Shuts down the entity gracefully for removal.
        /// </summary>
        public void Shutdown()
        {
            foreach (var component in components.Values)
            {
                component.OnRemove();
            }
            components.Clear();
        }
        #endregion

        #region Component Manipulation
        /// <summary>
        /// Public method to add a component to an entity.
        /// Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="family">the family of component -- there can only be one at a time per family.</param>
        /// <param name="component">The component.</param>
        public void AddComponent(ComponentFamily family, IGameObjectComponent component)
        {
            if (components.Keys.Contains(family))
                RemoveComponent(family);
            components.Add(family, component);
            component.OnAdd(this); 
        }

        /// <summary>
        /// Public method to remove a component from an entity.
        /// Calls the onRemove method of the component, which handles removing it 
        /// from the component manager and shutting down the component.
        /// </summary>
        /// <param name="family"></param>
        public void RemoveComponent(ComponentFamily family)
        {
            if (components.Keys.Contains(family))
            {
                components[family].OnRemove();
                components.Remove(family); 
            }
        }

        /// <summary>
        /// Returns the component in the specified family
        /// </summary>
        /// <param name="family">the family</param>
        /// <returns></returns>
        public IGameObjectComponent GetComponent(ComponentFamily family)
        {
            if (components.ContainsKey(family))
                return components[family];
            return null;
        }

        /// <summary>
        /// Checks to see if a component of a certain family exists
        /// </summary>
        /// <param name="family">componentfamily to check</param>
        /// <returns>true if component exists, false otherwise</returns>
        public bool HasComponent(ComponentFamily family)
        {
            return components.ContainsKey(family);
        }

        /// <summary>
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        public void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] args)
        {
            
#if MESSAGEDEBUG
            var senderfamily = ComponentFamily.Generic;
            var uid = 0;
            var sendertype = "";
            if(sender.GetType().IsAssignableFrom(typeof(IGameObjectComponent)))
            {
                var realsender = (GameObjectComponent)sender;
                senderfamily = realsender.Family;

                uid = realsender.Owner.Uid;
                sendertype = realsender.GetType().ToString();
            }
            //Log the message
            IMessageLogger logger = IoCManager.Resolve<IMessageLogger>();
            logger.LogComponentMessage(uid, senderfamily, sendertype, type);
#endif
            foreach (IGameObjectComponent component in components.Values.ToArray())
            {
                component.RecieveMessage(sender, type, replies, args);
            }
        }
        #endregion

        /// <summary>
        /// Requests Description string from components and returns it. If no component answers, returns default description from template.
        /// </summary>
        public string GetDescriptionString() //This needs to go here since it can not be bound to any single component.
        {
            var replies = new List<ComponentReplyMessage>();

            this.SendMessage(this, ComponentMessageType.GetDescriptionString, replies);

            if (replies.Any())
                return (string)replies.First(x => x.MessageType == ComponentMessageType.GetDescriptionString).ParamsList[0]; //If you dont answer with a string then fuck you.
            else
                return this.Template.Description;
        }

        //VARIABLES TO REFACTOR AT A LATER DATE
        /// <summary>
        /// Movement speed of the entity. This should be refactored.
        /// </summary>
        //public float speed = 600.0f;

        //FUNCTIONS TO REFACTOR AT A LATER DATE
        /// <summary>
        /// This should be refactored to some sort of component that sends entity movement input or something.
        /// </summary>

        public void Moved()
        {
            if(OnMove != null)
                OnMove(this, new VectorEventArgs(Position));
        }

        internal void HandleComponentMessage(IncomingEntityComponentMessage message)
        {
            if (components.Keys.Contains(message.ComponentFamily))
            {
                components[message.ComponentFamily].HandleNetworkMessage(message);
            }
        }

        public void HandleNetworkMessage(IncomingEntityMessage message)
        {
            switch (message.MessageType)
            {
                case EntityMessage.PositionMessage:
                    break;
                case EntityMessage.ComponentMessage:
                    HandleComponentMessage((IncomingEntityComponentMessage)message.Message);
                    break;
            }
        }

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="messageParams">Parameters</param>
        public void SendComponentNetworkMessage(IGameObjectComponent component, NetDeliveryMethod method, params object[] messageParams)
        {
            m_entityNetworkManager.SendComponentNetworkMessage(this, component.Family, NetDeliveryMethod.ReliableUnordered, messageParams);
        }

        public void SendComponentInstantiationMessage(IGameObjectComponent component)
        {
            if (component == null)
                throw new Exception("Component is null");
          
            m_entityNetworkManager.SendEntityNetworkMessage(this, EntityMessage.ComponentInstantiationMessage, component.Family);
        }

        #region compatibility for entity transition
        public void SetNetworkManager(EntityNetworkManager manager)
        {
            m_entityNetworkManager = manager;
        }
        #endregion
    }
}
