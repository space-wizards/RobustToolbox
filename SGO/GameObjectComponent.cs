using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GameObject;

namespace SGO
{
    public class GameObjectComponent : GameObject.Component, IGameObjectComponent
    {
        /// <summary>
        /// The entity that owns this component
        /// </summary>
        new public IEntity Owner { get; set; }
        
        protected bool stateChanged = false;

        private Dictionary<string, Type> _sVars = new Dictionary<string, Type>(); 

        #region IGameObjectComponent Members
        
        /// <summary>
        /// Recieve a message from another component within the owner entity
        /// </summary>
        /// <param name="sender">the component that sent the message</param>
        /// <param name="type">the message type in CGO.MessageType</param>
        /// <param name="list">parameters list</param>
        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                            params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return reply;

            return reply;
        }
        
        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component, and removes it from the ComponentManager as well.
        /// </summary>
        public override void OnRemove()
        {
            Owner = null;
            Shutdown();
            //Send us to the manager so it knows we're dead.
            ComponentManager.Singleton.RemoveComponent(this);
        }

        /// <summary>
        /// Called when the component gets added to an entity. 
        /// This adds it to the component manager as well. No component should ever be owned
        /// by an entity without being in the ComponentManager.
        /// </summary>
        /// <param name="owner"></param>
        public override void OnAdd(GameObject.IEntity owner)
        {
            Owner = (IEntity)owner;
            //Send us to the manager so it knows we're active
            ComponentManager.Singleton.AddComponent(this);
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
        
        /// <summary>
        /// Handles a message that a client has just instantiated a component
        /// </summary>
        /// <param name="netConnection"></param>
        public virtual void HandleInstantiationMessage(NetConnection netConnection)
        {
        }

        #endregion
        
        /// <summary>
        /// Gets all available SVars for the entity. 
        /// This gets current values, or at least it should...
        /// </summary>
        /// <returns>Returns a list of component parameters for marshaling</returns>
        public List<MarshalComponentParameter> GetSVars()
        {
            return (from param in GetParameters() where SVarIsRegistered(param.MemberName) select new MarshalComponentParameter(Family, param)).ToList();
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

        /// <summary>
        /// Sets a component parameter via the sVar interface. Only
        /// parameters that are registered as sVars will be set through this 
        /// function.
        /// </summary>
        /// <param name="sVar">ComponentParameter</param>
        public void SetSVar(MarshalComponentParameter sVar)
        {
            var param = sVar.Parameter;

            //If it is registered, and the types match, set it.
            if(_sVars.ContainsKey(param.MemberName) && 
                _sVars[param.MemberName] == param.ParameterType)
                SetParameter(param);
        }


    }
}