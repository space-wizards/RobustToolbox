using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     Base component for the ECS system.
    /// </summary>
    [Reflect(false)]
    public abstract class Component : IComponent
    {
        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public virtual uint? NetID => null;

        /// <inheritdoc />
        public virtual bool NetworkSynchronizeExistence => false;

        /// <inheritdoc />
        public IEntity Owner { get; private set; }

        /// <inheritdoc />
        public virtual Type StateType => typeof(ComponentState);

        /// <inheritdoc />
        public virtual void OnRemove()
        {
            Shutdown();

            //Send us to the manager so it knows we're dead.
            IoCManager.Resolve<IComponentManager>().RemoveComponent(this);
            Owner = null;
        }

        /// <inheritdoc />
        public virtual void OnAdd(IEntity owner)
        {
            Owner = owner;

            //Send us to the manager so it knows we're active
            var manager = IoCManager.Resolve<IComponentManager>();
            manager.AddComponent(this);
        }

        /// <inheritdoc />
        public virtual void Initialize()
        {

        }

        /// <inheritdoc />
        public virtual void Shutdown()
        {
        }

        /// <inheritdoc />
        public virtual void LoadParameters(YamlMappingNode mapping)
        {
        }

        /// <inheritdoc />
        public virtual void Update(float frameTime)
        {
        }

        /// <inheritdoc />
        public virtual ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = ComponentReplyMessage.Empty;

            if (sender == this) //Don't listen to our own messages!
                return reply;

            return reply;
        }

        /// <inheritdoc />
        public virtual ComponentState GetComponentState()
        {
            if (NetID == null)
                throw new InvalidOperationException($"Cannot make state for component without Net ID: {GetType()}");

            return new ComponentState(NetID.Value);
        }

        /// <inheritdoc />
        public virtual void HandleComponentState(ComponentState state)
        {
        }

        /// <inheritdoc />
        public virtual void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
        }
    }
}
