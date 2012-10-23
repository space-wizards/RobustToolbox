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
using ClientInterfaces.Configuration;

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
        private readonly Dictionary<ComponentFamily, IGameObjectComponent> _components = new Dictionary<ComponentFamily, IGameObjectComponent>();

        private EntityNetworkManager _entityNetworkManager;

        public IEntityTemplate Template { get; set; }

        public string Name { get; set; }

        public event EventHandler<VectorEventArgs> OnMove;
        public event EventHandler<GetSVarsEventArgs> GetSVarsCallback; 

        public bool Initialized { get; set; }

        public int Uid { get; set; }

        private bool _messageProfiling;

        public delegate void ShutdownEvent(Entity e);
        public event ShutdownEvent OnShutdown;

        public Vector2D Position { get; set; }

        public int _rotation = 0;
        public int Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
                //Send update
            }
        }

        #endregion

        #region Constructor/Destructor
        /// <summary>
        /// Constructor for realz. This one should be used eventually instead of the naked one.
        /// </summary>
        /// <param name="entityNetworkManager"></param>
        public Entity(EntityNetworkManager entityNetworkManager)
        {
            _entityNetworkManager = entityNetworkManager;
            Initialize();

            var cfg = IoCManager.Resolve<IConfigurationManager>();
            _messageProfiling = cfg.GetMessageLogging();
        }

        /// <summary>
        /// Sets up variables and shite
        /// </summary>
        public virtual void Initialize()
        {
            SendMessage(this, ComponentMessageType.Initialize);
            Initialized = true;
        }

        /// <summary>
        /// Shuts down the entity gracefully for removal.
        /// </summary>
        public void Shutdown()
        {
            foreach (var component in _components.Values)
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
            if (_components.Keys.Contains(family)) RemoveComponent(family);

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
        /// Returns the component in the specified family
        /// </summary>
        /// <param name="family">the family</param>
        /// <returns></returns>
        public IGameObjectComponent GetComponent(ComponentFamily family)
        {
            return _components.ContainsKey(family) ? _components[family] : null;
        }

        public T GetComponent<T>(ComponentFamily family) where T : class
        {
            if (GetComponent(family) is T)
                return (T)GetComponent(family);

            return null;
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

        /// <summary>
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        public void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] args)
        {
            LogComponentMessage(sender, type, args);
         
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
            LogComponentMessage(sender, type, args);

            foreach (var component in _components.Values.ToArray())
            {
                component.RecieveMessage(sender, type, args);
            }
        }

        public ComponentReplyMessage SendMessage(object sender, ComponentFamily family, ComponentMessageType type, params object[] args)
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
            var senderfamily = ComponentFamily.Generic;
            var uid = 0;
            var sendertype = "";
            //if (sender.GetType().IsAssignableFrom(typeof(IGameObjectComponent)))
            if (typeof(IGameObjectComponent).IsAssignableFrom(sender.GetType()))
            {
                var realsender = (IGameObjectComponent)sender;
                senderfamily = realsender.Family;

                uid = realsender.Owner.Uid;
                sendertype = realsender.GetType().ToString();
            }
            else
            {
                sendertype = sender.GetType().ToString();
            }
            //Log the message
            IMessageLogger logger = IoCManager.Resolve<IMessageLogger>();
            logger.LogComponentMessage(uid, senderfamily, sendertype, type);
        }

        #endregion

        /// <summary>
        /// Requests Description string from components and returns it. If no component answers, returns default description from template.
        /// </summary>
        public string GetDescriptionString() //This needs to go here since it can not be bound to any single component.
        {
            var replies = new List<ComponentReplyMessage>();

            SendMessage(this, ComponentMessageType.GetDescriptionString, replies);

            if (replies.Any()) return (string)replies.First(x => x.MessageType == ComponentMessageType.GetDescriptionString).ParamsList[0]; //If you dont answer with a string then fuck you.
            
            return Template.Description;
        }

        //FUNCTIONS TO REFACTOR AT A LATER DATE
        /// <summary>
        /// This should be refactored to some sort of component that sends entity movement input or something.
        /// </summary>

        public void Moved()
        {
            if(OnMove != null) OnMove(this, new VectorEventArgs(Position));
        }

        internal void HandleComponentMessage(IncomingEntityComponentMessage message)
        {
            if (_components.Keys.Contains(message.ComponentFamily))
            {
                _components[message.ComponentFamily].HandleNetworkMessage(message);
            }
        }

        public void HandleNetworkMessage(ClientIncomingEntityMessage message)
        {
            switch (message.MessageType)
            {
                case EntityMessage.PositionMessage:
                    break;
                case EntityMessage.ComponentMessage:
                    HandleComponentMessage((IncomingEntityComponentMessage)message.Message);
                    break;
                case EntityMessage.NameUpdate:
                    Name = message.Message as string;
                    break;
                case EntityMessage.GetSVars:
                    HandleGetSVars(message);
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
            _entityNetworkManager.SendComponentNetworkMessage(this, component.Family, NetDeliveryMethod.ReliableUnordered, messageParams);
        }

        public void SendComponentInstantiationMessage(IGameObjectComponent component)
        {
            if (component == null)
                throw new Exception("Component is null");
          
            _entityNetworkManager.SendEntityNetworkMessage(this, EntityMessage.ComponentInstantiationMessage, component.Family);
        }

        #region SVar/CVar Marshalling

        public void SetSVar(MarshalComponentParameter svar)
        {
            _entityNetworkManager.SendEntityNetworkMessage(this, 
                EntityMessage.SetSVar, 
                svar.Family, 
                svar.Parameter.MemberName, 
                svar.Parameter.GetValue());
        }

        public void GetSVars()
        {
            _entityNetworkManager.SendEntityNetworkMessage(this, 
                EntityMessage.GetSVars);
        }

        public void HandleGetSVars(ClientIncomingEntityMessage message)
        {
            //If nothing's listening, then why bother with this shit?
            if (GetSVarsCallback == null)
                return;
            var msg = (NetIncomingMessage)message.Message;

            var count = msg.ReadInt32();
            var svars = new List<MarshalComponentParameter>();
            for(int i = 0;i<count;i++)
            {
                svars.Add(MarshalComponentParameter.Deserialize(msg));
            }
            
            GetSVarsCallback(this, new GetSVarsEventArgs(svars));
            GetSVarsCallback = null;
        }

        #endregion
    }
}
