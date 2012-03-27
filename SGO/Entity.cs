using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Reflection;
using System.Collections;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using System.Runtime.Serialization;

namespace SGO
{
    /// <summary>
    /// Base entity class. Acts as a container for components, and a place to store location data.
    /// Should not contain any game logic whatsoever other than entity movement functions and 
    /// component management functions.
    /// </summary>
    [Serializable()]
    public class Entity : ISerializable
    {
        #region Variables
        /// <summary>
        /// Holds this entity's components
        /// </summary>
        private Dictionary<ComponentFamily, IGameObjectComponent> _components = new Dictionary<ComponentFamily, IGameObjectComponent>();
        
        private EntityNetworkManager m_entityNetworkManager;

        public EntityTemplate template;

        public event EntityMoveEvent OnMove;
        public delegate void EntityMoveEvent(Vector2 toPosition, Vector2 fromPosition);

        public delegate void ShutdownEvent(Entity e);
        public event ShutdownEvent OnShutdown;

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
        public Vector2 position;
        public float rotation;
        public string name;

        #endregion

        #region Constructor/Destructor
        /// <summary>
        /// Constructor
        /// </summary>
        public Entity()
        {
            Initialize();
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
        public virtual void Initialize(bool loaded = false)
        {     }

        /// <summary>
        /// Shuts down the entity gracefully for removal.
        /// </summary>
        public void Shutdown()
        {
            foreach (GameObjectComponent component in _components.Values)
            {
                component.OnRemove();
            }
            _components.Clear();
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
            if (_components.Keys.Contains(family))
                RemoveComponent(family);
            _components.Add(family, component);
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
            if (_components.Keys.Contains(family))
            {
                _components[family].OnRemove();
                _components.Remove(family); 
            }
        }

        /// <summary>
        /// Checks to see if a component of a certain family exists
        /// </summary>
        /// <param name="family">componentfamily to check</param>
        /// <returns>true if component exists, false otherwise</returns>
        public bool HasComponent(ComponentFamily family)
        {
            if (_components.ContainsKey(family))
                return true;
            return false;
        }

        /// <summary>
        /// Gets the component of the specified family, if it exists
        /// </summary>
        /// <param name="family">componentfamily to get</param>
        /// <returns></returns>
        public IGameObjectComponent GetComponent(ComponentFamily family)
        {
            if (_components.ContainsKey(family))
                return _components[family];
            return null;
        }

        /// <summary>
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        public void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] args)
        {
            //LogComponentMessage(sender, type, args);

            foreach (var component in _components.Values.ToArray())
            {
                if (replies != null)
                {
                    var reply = component.RecieveMessage(sender, type, args);
                    if (reply.MessageType != ComponentMessageType.Empty)
                        replies.Add(reply);
                }
                else
                    component.RecieveMessage(sender, type, args);
            }
        }

        public void SendMessage(object sender, ComponentMessageType type, params object[] args)
        {
            //LogComponentMessage(sender, type, args);

            foreach (var component in _components.Values.ToArray())
            {
                component.RecieveMessage(sender, type, args);
            }
        }

        public ComponentReplyMessage SendMessage(object sender, ComponentFamily family, ComponentMessageType type, params object[] args)
        {
            //LogComponentMessage(sender, type, args);

            if (HasComponent(family))
                return GetComponent(family).RecieveMessage(sender, type, args);
            else
                return ComponentReplyMessage.Empty;
        }

        #endregion

        public void Translate(Vector2 toPosition)
        {
            Vector2 oldPosition = position;
            position = toPosition;
            SendPositionUpdate();
            Moved(oldPosition);
        }

        public void Translate(Vector2 toPosition, float toRotation)
        {
            rotation = toRotation;
            Translate(toPosition);
        }

        #region Networking
        
        #endregion

        //VARIABLES TO REFACTOR AT A LATER DATE
        /// <summary>
        /// Movement speed of the entity. This should be refactored.
        /// </summary>
        public float speed = 6.0f;

        //FUNCTIONS TO REFACTOR AT A LATER DATE
        /// <summary>
        /// This should be refactored to some sort of component that sends entity movement input or something.
        /// </summary>
        public virtual void SendPositionUpdate()
        {
            SendMessage(this, ComponentMessageType.SendPositionUpdate);
        }

        public virtual void HandleClick(int clickerID) { }

        public void Moved(Vector2 fromPosition)
        {
            if(OnMove != null)
                OnMove(position, fromPosition);
        }

        #region Serialization

        public void SerializeBasicInfo(SerializationInfo info, StreamingContext ctxt)
        {
            name = (string)info.GetValue("name", typeof(string));
            position = (Vector2)info.GetValue("position", typeof(Vector2));
            rotation = (float)info.GetValue("rotation", typeof(float));
        }

        public Entity(SerializationInfo info, StreamingContext ctxt)
        {
            name = (string)info.GetValue("name", typeof(string));
            position = (Vector2)info.GetValue("position", typeof(Vector2));
            rotation = (float)info.GetValue("rotation", typeof(float));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("name", name);
            info.AddValue("position", position);
            info.AddValue("rotation", rotation);
        }

        #endregion

        internal void HandleNetworkMessage(IncomingEntityMessage message)
        {
            switch (message.messageType)
            {
                case EntityMessage.PositionMessage:
                    break;
                case EntityMessage.ComponentMessage:
                    HandleComponentMessage((IncomingEntityComponentMessage)message.message, message.client);
                    break;
                case EntityMessage.ComponentInstantiationMessage:
                    HandleComponentInstantiationMessage(message);
                    break;
            }
        }

        internal void HandleComponentInstantiationMessage(IncomingEntityMessage message)
        {
            if(HasComponent((ComponentFamily)message.message))
                GetComponent((ComponentFamily)message.message).HandleInstantiationMessage(message.client);
        }

        internal void HandleComponentMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (_components.Keys.Contains(message.componentFamily))
            {
                _components[message.componentFamily].HandleNetworkMessage(message, client);
            }
        }

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="recipient">The intended recipient netconnection (if null send to all)</param>
        /// <param name="messageParams">Parameters</param>
        public void SendComponentNetworkMessage(IGameObjectComponent component, NetDeliveryMethod method, NetConnection recipient, params object[] messageParams)
        {
            m_entityNetworkManager.SendComponentNetworkMessage(this, component.Family, NetDeliveryMethod.ReliableUnordered, recipient, messageParams);
        }

        #region compatibility for atom transition -- ???
        public void SetNetworkManager(EntityNetworkManager manager)
        {
            m_entityNetworkManager = manager;
        }
        #endregion
    }
}
