using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace GameObject
{
    public interface IComponent
    {
        ComponentFamily Family { get; }
        IEntity Owner { get; set; }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component
        /// </summary>
        void OnRemove();

        /// <summary>
        /// Called when the component gets added to an entity. 
        /// </summary>
        /// <param name="owner"></param>
        void OnAdd(IEntity owner);

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
    }

    public class Component : IComponent
    {
        public virtual IEntity Owner { get; set; }
        public ComponentFamily Family { get; protected set; }

        public Component()
        {
            Family = ComponentFamily.Generic;
        }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component
        /// </summary>
        public virtual void OnRemove()
        {
            Owner = null;
            Shutdown();
        }

        /// <summary>
        /// Called when the component gets added to an entity. 
        /// </summary>
        /// <param name="owner"></param>
        public virtual void OnAdd(IEntity owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Base method to shut down the component. 
        /// </summary>
        public virtual void Shutdown()
        {}

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
        {}

        /// <summary>
        /// Main method for updating the component. This is called from a big loop in Componentmanager.
        /// </summary>
        /// <param name="frameTime"></param>
        public virtual void Update(float frameTime)
        {}

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
        {}

        /// <summary>
        /// Empty method for handling incoming input messages from counterpart server/client components
        /// </summary>
        /// <param name="message">the message object</param>
        public virtual void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {}
    }
}
