using System;
using System.Collections.Generic;
using ClientInterfaces.GOC;
using SS13_Shared;
using SS13_Shared.GO;
using System.Xml.Linq;

namespace CGO
{
    public abstract class GameObjectComponent : GameObject.Component, IGameObjectComponent
    {
        /// <summary>
        /// The entity that owns this component
        /// </summary>
        new public IEntity Owner { get; set; }

        public virtual Type StateType { get { return null; }
        }

        /*
        /// <summary>
        /// Recieve a message from another component within the owner entity
        /// </summary>
        /// <param name="sender">the component that sent the message</param>
        /// <param name="type">the message type in CGO.MessageType</param>
        /// <param name="reply"></param>
        /// <param name="list">parameters list</param>
        public virtual void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            if (sender == this) //Don't listen to our own messages!
                return;
            switch(type)
            {
                case ComponentMessageType.Initialize:
                    Owner.SendComponentInstantiationMessage(this);
                    break;
            }
        }*/

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return reply;
            switch (type)
            {
                case ComponentMessageType.Initialize:
                    Owner.SendComponentInstantiationMessage(this);
                    break;
            }

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
            if (Owner.Initialized)
                Owner.SendComponentInstantiationMessage(this);
        }
    }
}
