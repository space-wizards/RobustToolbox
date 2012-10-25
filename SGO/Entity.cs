using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Server;
using SS13_Shared.ServerEnums;
using ServerInterfaces.Chat;
using ServerInterfaces.Configuration;
using ServerInterfaces.GameObject;
using ServerInterfaces.MessageLogging;
using ServerInterfaces.Player;
using ServerServices;
using ServerServices.Log;

namespace SGO
{
    /// <summary>
    /// Base entity class. Acts as a container for components, and a place to store location data.
    /// Should not contain any game logic whatsoever other than entity movement functions and 
    /// component management functions.
    /// </summary>
    public class Entity : IEntity
    {
        #region Variables

        #region Delegates

        public delegate void NetworkedOnJoinSpawnEvent(NetConnection client);

        public delegate void NetworkedSpawnEvent();
        
        #endregion

        /// <summary>
        /// Holds this entity's components
        /// </summary>
        private readonly Dictionary<ComponentFamily, IGameObjectComponent> _components =
            new Dictionary<ComponentFamily, IGameObjectComponent>();

        private readonly bool _messageProfiling;

        private readonly IEntityNetworkManager m_entityNetworkManager;
        private bool _initialized;
        private string _name;

        public IEntityTemplate Template { get; set; }

        public event EntityMoveEvent OnMove;

        public int Uid { get; set; }

        public Vector2 Position { get; set; }

        private Direction _direction = Direction.South;
        public Direction Direction
        { 
            get 
            {
                return _direction; 
            }
            set 
            {
                _direction = value;
                SendDirectionUpdate();
            } 
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                SendNameUpdate();
            }
        }

        public event ShutdownEvent OnShutdown;
        public event NetworkedSpawnEvent OnNetworkedSpawn;
        public event NetworkedOnJoinSpawnEvent OnNetworkedJoinSpawn;

        #endregion

        #region Constructor/Destructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entityNetworkManager"></param>
        public Entity(IEntityNetworkManager entityNetworkManager)
        {
            m_entityNetworkManager = entityNetworkManager;
            _messageProfiling = IoCManager.Resolve<IConfigurationManager>().MessageLogging;
            OnNetworkedJoinSpawn += SendNameUpdate;
            OnNetworkedJoinSpawn += SendDirectionUpdate;
            OnNetworkedSpawn += SendNameUpdate;
            OnNetworkedSpawn += SendDirectionUpdate;
        }

        public void FireNetworkedJoinSpawn(NetConnection client)
        {
            OnNetworkedJoinSpawn(client);
        }

        public void FireNetworkedSpawn()
        {
            OnNetworkedSpawn();
        }

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

        /// <summary>
        /// Sets up variables and shite
        /// </summary>
        public void Initialize(bool loaded = false)
        {
            _initialized = true;
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

        public T GetComponent<T>(ComponentFamily family) where T : class
        {
            if (GetComponent(family) is T)
                return (T)GetComponent(family);

            return null;
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

        public void SendMessage(object sender, ComponentMessageType type, params object[] args)
        {
            LogComponentMessage(sender, type, args);

            foreach (IGameObjectComponent component in _components.Values.ToArray())
            {
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
            LogComponentMessage(sender, type, args);

            foreach (IGameObjectComponent component in _components.Values.ToArray())
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

        public ComponentReplyMessage SendMessage(object sender, ComponentFamily family, ComponentMessageType type,
                                                 params object[] args)
        {
            LogComponentMessage(sender, type, args);

            if (HasComponent(family))
                return GetComponent(family).RecieveMessage(sender, type, args);
            else
                return ComponentReplyMessage.Empty;
        }

        /// <summary>
        /// Logs a component message to the messaging profiler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="args"></param>
        private void LogComponentMessage(object sender, ComponentMessageType type, params object[] args)
        {
            if (!_messageProfiling)
                return;
            ComponentFamily senderfamily = ComponentFamily.Generic;
            int uid = 0;
            string sendertype = "";
            //if (sender.GetType().IsAssignableFrom(typeof(IGameObjectComponent)))
            if (typeof (IGameObjectComponent).IsAssignableFrom(sender.GetType()))
            {
                var realsender = (IGameObjectComponent) sender;
                senderfamily = realsender.Family;

                uid = realsender.Owner.Uid;
                sendertype = realsender.GetType().ToString();
            }
            else
            {
                sendertype = sender.GetType().ToString();
            }
            //Log the message
            var logger = IoCManager.Resolve<IMessageLogger>();
            logger.LogComponentMessage(uid, senderfamily, sendertype, type);
        }

        #endregion

        /// <summary>
        /// Movement speed of the entity. This should be refactored.
        /// </summary>
        public float speed = 6.0f;

        #region IEntity Members

        public void Translate(Vector2 toPosition)
        {
            Vector2 oldPosition = Position;
            Position = toPosition;
            SendPositionUpdate();
            Moved(oldPosition);
        }

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="recipient">The intended recipient netconnection (if null send to all)</param>
        /// <param name="messageParams">Parameters</param>
        public void SendComponentNetworkMessage(IGameObjectComponent component, NetDeliveryMethod method,
                                                NetConnection recipient, params object[] messageParams)
        {
            if (!_initialized)
                return;
            m_entityNetworkManager.SendComponentNetworkMessage(this, component.Family,
                                                               method, recipient,
                                                               messageParams);
        }

        #endregion

        //VARIABLES TO REFACTOR AT A LATER DATE

        //FUNCTIONS TO REFACTOR AT A LATER DATE
        /// <summary>
        /// This should be refactored to some sort of component that sends entity movement input or something.
        /// </summary>
        public virtual void SendPositionUpdate()
        {
            if(_initialized)
                SendMessage(this, ComponentMessageType.SendPositionUpdate);
        }

        public virtual void HandleClick(int clickerID)
        {
        }

        public void Moved(Vector2 fromPosition)
        {
            if (OnMove != null)
                OnMove(Position, fromPosition);
        }


        public void HandleNetworkMessage(ServerIncomingEntityMessage message)
        {
            switch (message.messageType)
            {
                case EntityMessage.PositionMessage:
                    break;
                case EntityMessage.ComponentMessage:
                    HandleComponentMessage((IncomingEntityComponentMessage) message.message, message.client);
                    break;
                case EntityMessage.ComponentInstantiationMessage:
                    HandleComponentInstantiationMessage(message);
                    break;
                case EntityMessage.SetSVar:
                    HandleSetSVar((MarshalComponentParameter)message.message, message.client);
                    break;
                case EntityMessage.GetSVars:
                    HandleGetSVars(message.client);
                    break;
            }
        }

        internal void HandleComponentInstantiationMessage(ServerIncomingEntityMessage message)
        {
            if (HasComponent((ComponentFamily) message.message))
                GetComponent((ComponentFamily) message.message).HandleInstantiationMessage(message.client);
        }

        internal void HandleComponentMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (_components.Keys.Contains(message.ComponentFamily))
            {
                _components[message.ComponentFamily].HandleNetworkMessage(message, client);
            }
        }

        #region SVar/CVar Marshalling
        /// <summary>
        /// This is all kinds of fucked, but basically it marshals an SVar from the client and poops
        /// it forward to the component named in the message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="client"></param>
        internal void HandleSetSVar(MarshalComponentParameter parameter, NetConnection client)
        {            
            //Check admin status -- only admins can get svars.
            var player = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(client);
            if (!player.adminPermissions.isAdmin)
            {
                LogManager.Log("Player " + player.name + " tried to set an SVar, but is not an admin!", LogLevel.Warning);
            }
            else
            {
                GetComponent(parameter.Family).SetSVar(parameter);   
                LogManager.Log("Player " + player.name + " set SVar."); //Make this message better
            }
            
        }

        /// <summary>
        /// Sends all available SVars to the client that requested them.
        /// </summary>
        /// <param name="client"></param>
        internal void SendSVars(NetConnection client)
        {
            var message = m_entityNetworkManager.CreateEntityMessage();
            message.Write(Uid);
            message.Write((byte)EntityMessage.GetSVars);

            var svars = new List<MarshalComponentParameter>();
            foreach(var component in _components.Values)
            {
                svars.AddRange(component.GetSVars());
            }

            message.Write(svars.Count);
            foreach(var svar in svars)
            {
                svar.Serialize(message);
            }
            m_entityNetworkManager.SendMessage(message, client, NetDeliveryMethod.ReliableUnordered);
            
        }

        /// <summary>
        /// Handle a getSVars message
        /// checks for admin access
        /// </summary>
        /// <param name="client"></param>
        private void HandleGetSVars(NetConnection client)
        {
            //Check admin status -- only admins can get svars.
            var player = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(client);
            if (!player.adminPermissions.isAdmin)
            {
                LogManager.Log("Player " + player.name + " tried to get SVars, but is not an admin!", LogLevel.Warning);
            }
            else
            {
                SendSVars(client);
                LogManager.Log("Sending SVars to " + player.name + " for entity " + Uid +":" + Name);
            }
        }
        #endregion

        public void Emote(string emote)
        {
            IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Emote, emote, Name, Uid);
        }

        #region Networking

        private void SendNameUpdate()
        {
            if (!_initialized || Name == null)
                return;
            NetOutgoingMessage message = CreateNameUpdateMessage();
            m_entityNetworkManager.SendToAll(message);
        }

        private void SendNameUpdate(NetConnection client)
        {
            if (!_initialized || Name == null)
                return;
            NetOutgoingMessage message = CreateNameUpdateMessage();
            m_entityNetworkManager.SendMessage(message, client);
        }

        private NetOutgoingMessage CreateNameUpdateMessage()
        {
            NetOutgoingMessage message = m_entityNetworkManager.CreateEntityMessage();
            message.Write(Uid); //Write this entity's UID
            message.Write((byte) EntityMessage.NameUpdate);
            message.Write(Name);
            return message;
        }

        private void SendDirectionUpdate(NetConnection client)
        {
            NetOutgoingMessage message = m_entityNetworkManager.CreateEntityMessage();
            message.Write(Uid);
            message.Write((byte)EntityMessage.SetDirection);
            message.Write((byte)Direction);
            if (client != null) m_entityNetworkManager.SendMessage(message, client);
            else m_entityNetworkManager.SendToAll(message);
        }

        private void SendDirectionUpdate()
        {
            SendDirectionUpdate(null);
        }
        #endregion
    }
}