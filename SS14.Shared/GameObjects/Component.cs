using Lidgren.Network;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.GameObjects
{
    [Reflect(false)]
    public abstract class Component : IComponent
    {
        public abstract string Name { get; }
        public virtual uint? NetID => null;
        public virtual bool NetworkSynchronizeExistence => false;

        #region IComponent Members

        public IEntity Owner { get; private set; }
        public virtual Type StateType => typeof(ComponentState);

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component
        /// This should be called AFTER any inheriting classes OnRemove code has run. This should be last.
        /// </summary>
        public virtual void OnRemove()
        {
            Shutdown();
            //Send us to the manager so it knows we're dead.
            IoCManager.Resolve<IComponentManager>().RemoveComponent(this);
            Owner = null;
        }

        /// <summary>
        /// Called when the component gets added to an entity.
        /// </summary>
        /// <param name="owner"></param>
        public virtual void OnAdd(IEntity owner)
        {
            Owner = owner;
            //Send us to the manager so it knows we're active
            var manager = IoCManager.Resolve<IComponentManager>();
            manager.AddComponent(this);
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
        public virtual void LoadParameters(YamlMappingNode mapping)
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
        /// Receive a message from another component within the owner entity
        /// </summary>
        /// <param name="sender">the component that sent the message</param>
        /// <param name="type">the message type in CGO.MessageType</param>
        /// <param name="list">parameters list</param>
        public virtual ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                            params object[] list)
        {
            ComponentReplyMessage reply = ComponentReplyMessage.Empty;

            if (sender == this) //Don't listen to our own messages!
                return reply;

            // Leaving this gap here in case anybody wants to add something later.

            return reply;
        }

        public virtual void HandleComponentEvent<T>(T args)
        {
        }

        /// <summary>
        /// Get the component's state for synchronizing
        /// </summary>
        /// <returns>ComponentState object</returns>
        public virtual ComponentState GetComponentState()
        {
            if (NetID == null)
            {
                throw new InvalidOperationException($"Cannot make state for component without Net ID: {GetType()}");
            }
            return new ComponentState(NetID.Value);
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
        public virtual IList<ComponentParameter> GetParameters()
        {
            return new List<ComponentParameter>();
        }

        #endregion IComponent Members

        protected virtual void SubscribeEvents()
        { }
    }
}
