using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace GameObject
{
    public interface IComponent
    {
        ComponentFamily Family { get; }
        Entity Owner { get; set; }
        Type StateType { get; }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component
        /// </summary>
        void OnRemove();

        /// <summary>
        /// Called when the component gets added to an entity. 
        /// </summary>
        /// <param name="owner"></param>
        void OnAdd(Entity owner);

        /// <summary>
        /// Base method to shut down the component. 
        /// </summary>
        void Shutdown();

        /// <summary>
        /// This allows setting of the component's parameters once it is instantiated.
        /// This should basically be overridden by every inheriting component, as parameters will be different
        /// across the board.
        /// </summary>
        /// <param name="parameter">ComponentParameter object describing the parameter and the value</param>
        void SetParameter(ComponentParameter parameter);

        void HandleExtendedParameters(XElement extendedParameters);

        /// <summary>
        /// Main method for updating the component. This is called from a big loop in Componentmanager.
        /// </summary>
        /// <param name="frameTime"></param>
        void Update(float frameTime);

        /// <summary>
        /// Recieve a message from another component within the owner entity
        /// </summary>
        /// <param name="sender">the component that sent the message</param>
        /// <param name="type">the message type in CGO.MessageType</param>
        /// <param name="list">parameters list</param>
        ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                             params object[] list);

        /// <summary>
        /// Get the component's state for synchronizing
        /// </summary>
        /// <returns>ComponentState object</returns>
        ComponentState GetComponentState();

        void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender);
        void HandleComponentState(dynamic state);

        /// <summary>
        /// Handles a message that a client has just instantiated a component
        /// </summary>
        /// <param name="netConnection"></param>
        void HandleInstantiationMessage(NetConnection netConnection);

        /// <summary>
        /// This gets a list of runtime-settable component parameters, with CURRENT VALUES
        /// If it isn't going to return a current value, it shouldn't return it at all.
        /// </summary>
        /// <returns></returns>
        List<ComponentParameter> GetParameters();

        /// <summary>
        /// Gets all available SVars for the entity. 
        /// This gets current values, or at least it should...
        /// </summary>
        /// <returns>Returns a list of component parameters for marshaling</returns>
        List<MarshalComponentParameter> GetSVars();

        /// <summary>
        /// Sets a component parameter via the sVar interface. Only
        /// parameters that are registered as sVars will be set through this 
        /// function.
        /// </summary>
        /// <param name="sVar">ComponentParameter</param>
        void SetSVar(MarshalComponentParameter sVar);
    }

    public class Component : IComponent
    {
        private readonly Dictionary<string, Type> _sVars = new Dictionary<string, Type>();

        public Component()
        {
            Family = ComponentFamily.Generic;
        }

        #region IComponent Members

        public Entity Owner { get; set; }
        public ComponentFamily Family { get; protected set; }

        public virtual Type StateType
        {
            get { return null; }
        }

        //Contains SVars -- Server only

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component
        /// This should be called AFTER any inheriting classes OnRemove code has run. This should be last.
        /// </summary>
        public virtual void OnRemove()
        {
            Shutdown();
            //Send us to the manager so it knows we're dead.
            Owner.EntityManager.ComponentManager.RemoveComponent(this);
            Owner = null;
        }

        /// <summary>
        /// Called when the component gets added to an entity. 
        /// </summary>
        /// <param name="owner"></param>
        public virtual void OnAdd(Entity owner)
        {
            Owner = owner;
            //Send us to the manager so it knows we're active
            Owner.EntityManager.ComponentManager.AddComponent(this);
            if (Owner.Initialized && Owner.EntityManager.EngineType == EngineType.Client)
                Owner.SendComponentInstantiationMessage(this);
        }

        /// <summary>
        /// Base method to shut down the component. 
        /// </summary>
        public virtual void Shutdown()
        {
        }

        /// <summary>
        /// This allows setting of the component's parameters once it is instantiated.
        /// This should basically be overridden by every inheriting component, as parameters will be different
        /// across the board.
        /// </summary>
        /// <param name="parameter">ComponentParameter object describing the parameter and the value</param>
        public virtual void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "ExtendedParameters":
                    HandleExtendedParameters(parameter.GetValue<XElement>());
                    break;
            }
        }

        public virtual void HandleExtendedParameters(XElement extendedParameters)
        {
        }

        /// <summary>
        /// Main method for updating the component. This is called from a big loop in Componentmanager.
        /// </summary>
        /// <param name="frameTime"></param>
        public virtual void Update(float frameTime)
        {
        }

        /// <summary>
        /// Recieve a message from another component within the owner entity
        /// </summary>
        /// <param name="sender">the component that sent the message</param>
        /// <param name="type">the message type in CGO.MessageType</param>
        /// <param name="list">parameters list</param>
        public virtual ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                            params object[] list)
        {
            ComponentReplyMessage reply = ComponentReplyMessage.Empty;

            if (sender == this) //Don't listen to our own messages!
                return reply;

            //Client-only hack
            if (Owner.EntityManager.EngineType == EngineType.Client)
            {
                switch (type)
                {
                    case ComponentMessageType.Initialize:
                        Owner.SendComponentInstantiationMessage(this);
                        break;
                }
            }

            return reply;
        }

        /// <summary>
        /// Get the component's state for synchronizing
        /// </summary>
        /// <returns>ComponentState object</returns>
        public virtual ComponentState GetComponentState()
        {
            return new ComponentState(Family);
        }

        public virtual void HandleComponentState(dynamic state)
        {
        }

        /// <summary>
        /// Empty method for handling incoming input messages from counterpart server/client components
        /// </summary>
        /// <param name="message">the message object</param>
        public virtual void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
        }

        /// <summary>
        /// Handles a message that a client has just instantiated a component
        /// </summary>
        /// <param name="netConnection"></param>
        public virtual void HandleInstantiationMessage(NetConnection netConnection)
        {
        }

        /// <summary>
        /// This gets a list of runtime-settable component parameters, with CURRENT VALUES
        /// If it isn't going to return a current value, it shouldn't return it at all.
        /// </summary>
        /// <returns></returns>
        public virtual List<ComponentParameter> GetParameters()
        {
            return new List<ComponentParameter>();
        }

        #endregion

        #region SVars Stuff

        /// <summary>
        /// Gets all available SVars for the entity. 
        /// This gets current values, or at least it should...
        /// </summary>
        /// <returns>Returns a list of component parameters for marshaling</returns>
        public List<MarshalComponentParameter> GetSVars()
        {
            return (from param in GetParameters()
                    where SVarIsRegistered(param.MemberName)
                    select new MarshalComponentParameter(Family, param)).ToList();
        }

        /// <summary>
        /// Sets a component parameter via the sVar interface. Only
        /// parameters that are registered as sVars will be set through this 
        /// function.
        /// </summary>
        /// <param name="sVar">ComponentParameter</param>
        public void SetSVar(MarshalComponentParameter sVar)
        {
            ComponentParameter param = sVar.Parameter;

            //If it is registered, and the types match, set it.
            if (_sVars.ContainsKey(param.MemberName) &&
                _sVars[param.MemberName] == param.ParameterType)
                SetParameter(param);
        }

        /// <summary>
        /// Checks if an SVar is registered
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected bool SVarIsRegistered(string name)
        {
            if (!_sVars.ContainsKey(name))
                return false;
            return true;
        }

        /// <summary>
        /// Registers an SVar
        /// </summary>
        /// <param name="sVar"></param>
        /// <param name="type"></param>
        protected void RegisterSVar(string sVar, Type type)
        {
            if (!SVarIsRegistered(sVar))
                _sVars[sVar] = type;
            else
                _sVars.Add(sVar, type);
        }

        /// <summary>
        /// Unregisters an SVar
        /// </summary>
        /// <param name="name"></param>
        protected void UnRegisterSVar(string name)
        {
            if (SVarIsRegistered(name))
                _sVars.Remove(name);
        }

        #endregion
    }
}