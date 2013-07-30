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
    public class Entity : GameObject.Entity, IEntity
    {
        #region Variables

        #region Delegates

        public delegate void NetworkedOnJoinSpawnEvent(NetConnection client);

        public delegate void NetworkedSpawnEvent();
        
        #endregion
        
        private readonly bool _messageProfiling;

        private readonly IEntityNetworkManager m_entityNetworkManager;
        private bool _initialized;
        private string _name;
        
        //public event EntityMoveEvent OnMove;

        public int Uid { get; set; }
        
        private bool stateChanged = false;
                public event ShutdownEvent OnShutdown;
        public event NetworkedSpawnEvent OnNetworkedSpawn;
        public event NetworkedOnJoinSpawnEvent OnNetworkedJoinSpawn;

        #endregion

        #region Constructor/Destructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entityNetworkManager"></param>
        public Entity(EntityManager entityManager)
            :base(entityManager)
        {
            m_entityNetworkManager = entityManager.EntityNetworkManager;
            _messageProfiling = IoCManager.Resolve<IConfigurationManager>().MessageLogging;
            OnNetworkedJoinSpawn += SendDirectionUpdate;
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
        /// Sets up variables and shite
        /// </summary>
        public void Initialize(bool loaded = false)
        {
            _initialized = true;
        }

        #endregion

        #region Component Manipulation

        public void SendMessage(object sender, ComponentMessageType type, params object[] args)
        {
            LogComponentMessage(sender, type, args);

            foreach (IGameObjectComponent component in GetComponents())
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

            foreach (IGameObjectComponent component in GetComponents())
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
                return GetComponent<GameObjectComponent>(family).RecieveMessage(sender, type, args);
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
                var realsender = (GameObjectComponent) sender;
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

        #region entity systems
        /// <summary>
        /// Match
        /// 
        /// Allows us to fetch entities with a defined SET of components
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool Match(IEntityQuery query)
        {
            // Empty queries always result in a match - equivalent to SELECT * FROM ENTITIES
            if (!(query.Exclusionset.Any() || query.OneSet.Any() || query.AllSet.Any()))
                return true;

            //If there is an EXCLUDE set, and the entity contains any component types in that set, or subtypes of them, the entity is excluded.
            bool matched = !(query.Exclusionset.Any() && query.Exclusionset.Any(t => ComponentTypes.Any(t.IsAssignableFrom)));
         
            //If there are no matching exclusions, and the entity matches the ALL set, the entity is included
            if(matched && (query.AllSet.Any() && query.AllSet.Any(t => !ComponentTypes.Any(t.IsAssignableFrom))))
                matched = false;
            //If the entity matches so far, and it matches the ONE set, it matches.
            if(matched && (query.OneSet.Any() && query.OneSet.Any(t => ComponentTypes.Any(t.IsAssignableFrom))))
                matched = false;
            return matched;
        }
        #endregion
        //VARIABLES TO REFACTOR AT A LATER DATE

        //FUNCTIONS TO REFACTOR AT A LATER DATE
        public virtual void HandleClick(int clickerID)
        {
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
                GetComponent<GameObjectComponent>((ComponentFamily) message.message).HandleInstantiationMessage(message.client);
        }

        internal void HandleComponentMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (GetComponentFamilies().Contains(message.ComponentFamily))
            {
                GetComponent<IGameObjectComponent>(message.ComponentFamily).HandleNetworkMessage(message, client);
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
                GetComponent<GameObjectComponent>(parameter.Family).SetSVar(parameter);   
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
            foreach(IGameObjectComponent component in GetComponents())
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

        private void SendDirectionUpdate(NetConnection client)
        {
            return;
            NetOutgoingMessage message = m_entityNetworkManager.CreateEntityMessage();
            message.Write(Uid);
            message.Write((byte)EntityMessage.SetDirection);
            message.Write((byte)GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction);
            if (client != null) m_entityNetworkManager.SendMessage(message, client);
            else m_entityNetworkManager.SendToAll(message);
        }

        private void SendDirectionUpdate()
        {
            SendDirectionUpdate(null);
        }

        public EntityState GetEntityState()
        {
            var compStates = GetComponentStates();

            //Reset entity state changed to false

            var es = new EntityState(
                Uid, 
                compStates, 
                GetComponent<TransformComponent>(ComponentFamily.Transform).Position,
                GetComponent<VelocityComponent>(ComponentFamily.Velocity).Velocity, 
                GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction, 
                Template.Name, 
                Name);
            return es;
        }

        private List<ComponentState> GetComponentStates()
        {
            var stateComps = new List<ComponentState>();
            foreach(IGameObjectComponent component in GetComponents())
            {
                var componentState = component.GetComponentState();
                stateComps.Add(componentState);
            }
            return stateComps;
        }

        #endregion
    }
}