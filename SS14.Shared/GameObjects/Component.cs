using System;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Reflection;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.GameObjects
{
    /// <inheritdoc />
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
        public IEntity Owner { get; set; }

        /// <inheritdoc />
        public virtual Type StateType => typeof(ComponentState);

        /// <inheritdoc />
        public bool Running { get; private set; }

        /// <inheritdoc />
        public bool Deleted { get; private set; }

        /// <inheritdoc />
        public virtual void OnRemove()
        {
            Owner = null;
            // Component manager will cull us because we've set ourselves to deleted.
            Deleted = true;
        }

        /// <summary>
        ///     Called when the component gets added to an entity.
        /// </summary>
        public virtual void OnAdd() { }

        /// <inheritdoc />
        public virtual void Spawned()
        {
        }

        /// <inheritdoc />
        public virtual void Initialize()
        {
        }

        /// <inheritdoc />
        public virtual void Startup()
        {
            Running = true;
        }

        /// <inheritdoc />
        public virtual void Shutdown()
        {
            Running = false;
        }

        /// <inheritdoc />
        [Obsolete("Use the ExposeData serialization system.")]
        public virtual void LoadParameters(YamlMappingNode mapping) { }

        /// <inheritdoc />
        public virtual void ExposeData(EntitySerializer serializer) { }

        /// <inheritdoc />
        [Obsolete("Components should be updated through a system.")]
        public virtual void Update(float frameTime) { }

        /// <summary>
        ///     Sends a message to all other components in this entity.
        ///     This is an alias of 'Owner.SendMessage(this, message);'
        /// </summary>
        /// <param name="message">Message to send.</param>
        protected void SendMessage(ComponentMessage message)
        {
            Owner.SendMessage(this, message);
        }

        /// <summary>
        ///     Sends a message over the network to all other components on the networked entity. This works both ways.
        ///     This is an alias of 'Owner.SendNetworkMessage(this, message);'
        /// </summary>
        /// <param name="message">Message to send.</param>
        protected void SendNetworkMessage(ComponentMessage message)
        {
            Owner.SendNetworkMessage(this, message);
        }

        /// <inheritdoc />
        public virtual void HandleMessage(object owner, ComponentMessage message) { }

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
        [Obsolete("Use HandleMessage")]
        public virtual void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
        }
    }
}
