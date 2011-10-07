using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Reflection;
using GorgonLibrary;
using System.Collections;
using Lidgren.Network;
using SS3D_shared.GO;

namespace CGO
{
    /// <summary>
    /// Base entity class. Acts as a container for components, and a place to store location data.
    /// Should not contain any game logic whatsoever other than entity movement functions and 
    /// component management functions.
    /// </summary>
    public class Entity
    {
        #region Variables
        /// <summary>
        /// Holds this entity's components
        /// </summary>
        private Dictionary<ComponentFamily, IGameObjectComponent> components;

        private EntityNetworkManager m_entityNetworkManager;

        public string name;

        public event EntityMoveEvent OnMove;
        public delegate void EntityMoveEvent(Vector2D toPosition);


        /// <summary>
        /// Unique entity id
        /// </summary>
        private int uid;
        public int Uid
        {
            get
            {
                return uid;
            }
            set
            {
                uid = value;
            }
        }

        /// <summary>
        /// These are the only real pieces of data that the entity should have -- position and rotation.
        /// </summary>
        public Vector2D position;
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
            components = new Dictionary<ComponentFamily, IGameObjectComponent>();
        }

        /// <summary>
        /// Compatibility method for atoms. This should be eliminated eventually when the above naked constructor is eliminated.
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
            foreach (GameObjectComponent component in components.Values)
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
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        public void SendMessage(object sender, MessageType type, List<ComponentReplyMessage> replies, params object[] args)
        {
            foreach (IGameObjectComponent component in components.Values)
            {
                component.RecieveMessage(sender, type, replies, args);
            }
        }
        #endregion

        /// <summary>
        /// Public update method for the entity. This will be useless after the atom code is refactored.
        /// </summary>
        /// <param name="frameTime"></param>
        public virtual void Update(float frameTime)
        {
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
                OnMove(position);
        }

        internal void HandleComponentMessage(IncomingEntityComponentMessage message)
        {
            if (components.Keys.Contains(message.componentFamily))
            {
                components[message.componentFamily].HandleNetworkMessage(message);
            }
        }

        internal void HandleNetworkMessage(IncomingEntityMessage message)
        {
            switch (message.messageType)
            {
                case EntityMessage.PositionMessage:
                    break;
                case EntityMessage.ComponentMessage:
                    HandleComponentMessage((IncomingEntityComponentMessage)message.message);
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

        #region compatibility for atom transition
        public void SetNetworkManager(EntityNetworkManager manager)
        {
            m_entityNetworkManager = manager;
        }
        #endregion
    }

    public struct ComponentReplyMessage
    {
        public MessageType messageType;
        public List<object> paramsList;

        public ComponentReplyMessage(MessageType _messageType, params object[] _paramsList)
        {
            if (_paramsList != null)
                paramsList = _paramsList.ToList();
            else
                paramsList = new List<object>();

            messageType = _messageType;            
        }

        public static ComponentReplyMessage Null = new ComponentReplyMessage(MessageType.Empty);
    }
}
