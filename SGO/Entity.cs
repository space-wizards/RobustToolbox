using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Reflection;
using System.Collections;
using Lidgren.Network;
using SS3D_shared;
using SS3D_shared.HelperClasses;
using SS3D_shared.GO;
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
        private Dictionary<ComponentFamily, IGameObjectComponent> components;

        private EntityNetworkManager m_entityNetworkManager;

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
        public void Initialize()
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
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        public void SendMessage(object sender, MessageType type, params object[] args)
        {
            foreach (IGameObjectComponent component in components.Values)
            {
                component.RecieveMessage(sender, type, args);
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

        #region Networking
        
        #endregion


        #region Movement
        /// <summary>
        /// Moves the entity to a new position in worldspace.
        /// </summary>
        /// <param name="toPosition"></param>
        public virtual void Translate(Vector2 toPosition)
        {
            Vector2 oldPosition = position;
            position += toPosition; // We move the sprite here rather than the position, as we can then use its updated AABB values.
        }

        /// <summary>
        /// Moves the entity Up
        /// </summary>
        public virtual void MoveUp()
        { }
        /// <summary>
        /// Moves the entity Down
        /// </summary>
        public virtual void MoveDown()
        { }
        /// <summary>
        /// Moves the entity Left
        /// </summary>
        public virtual void MoveLeft()
        { }
        /// <summary>
        /// Moves the entity Right
        /// </summary>
        public virtual void MoveRight()
        { }
        /// <summary>
        /// Moves the entity Up and Left
        /// </summary>
        public virtual void MoveUpLeft()
        { }
        /// <summary>
        /// Moves the entity Up and Right
        /// </summary>
        public virtual void MoveUpRight()
        { }
        /// <summary>
        /// Moves the entity Down and Left
        /// </summary>
        public virtual void MoveDownLeft()
        { }
        /// <summary>
        /// Moves the entity Down and Right
        /// </summary>
        public virtual void MoveDownRight()
        { }

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
        { }

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
                    HandleComponentMessage((IncomingEntityComponentMessage)message.message);
                    break;
            }
        }

        internal void HandleComponentMessage(IncomingEntityComponentMessage message)
        {
            if (components.Keys.Contains(message.componentFamily))
            {
                components[message.componentFamily].HandleNetworkMessage(message);
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

        #region compatibility for atom transition
        public void SetNetworkManager(EntityNetworkManager manager)
        {
            m_entityNetworkManager = manager;
        }
        #endregion
    }
}
