using System;
using System.Runtime.CompilerServices;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Reflection;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.GameObjects
{
    /// <inheritdoc />
    [Reflect(false)]
    public abstract class Component : IComponent
    {
        /// <inheritdoc />
        [ViewVariables]
        public abstract string Name { get; }

        /// <inheritdoc />
        [ViewVariables]
        public virtual uint? NetID => null;

        /// <inheritdoc />
        [ViewVariables]
        public virtual bool NetworkSynchronizeExistence => false;

        private bool _netSyncEnabled = true;
        /// <inheritdoc />
        [ViewVariables]
        public bool NetSyncEnabled
        {
            get => _netSyncEnabled;
            set => _netSyncEnabled = value;
        }

        /// <inheritdoc />
        [ViewVariables]
        public IEntity Owner { get; set; }

        /// <inheritdoc />
        public virtual Type StateType => typeof(ComponentState);

        /// <summary>
        ///     True if this entity is a client-only entity.
        ///     That is, it does not exist on the server, only THIS client.
        /// </summary>
        [ViewVariables]
        public bool IsClientSide => Owner.Uid.IsClientSide();

        [ViewVariables]
        public bool Initialized { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public bool Running { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; private set; }

        [ViewVariables]
        public uint LastModifiedTick { get; private set; }

        /// <inheritdoc />
        public virtual void OnRemove()
        {
            if (Running)
                throw new InvalidOperationException("Cannot Remove a running entity!");

            // We have been marked for deletion by the Component Manager.
            Deleted = true;
        }

        /// <summary>
        ///     Called when the component gets added to an entity.
        /// </summary>
        public virtual void OnAdd()
        {
            if (Initialized)
                throw new InvalidOperationException("Cannot Add an Initialized component!");

            if (Running)
                throw new InvalidOperationException("Cannot Add a running component!");

            if (Deleted)
                throw new InvalidOperationException("Cannot Add a Deleted component!");
        }

        /// <inheritdoc />
        public virtual void Initialize()
        {
            if (Initialized)
                throw new InvalidOperationException("Component already Initialized!");

            if (Running)
                throw new InvalidOperationException("Cannot Initialize a running component!");

            if (Deleted)
                throw new InvalidOperationException("Cannot Initialize a Deleted component!");

            Initialized = true;
        }

        /// <inheritdoc />
        public virtual void Startup()
        {
            if (!Initialized)
                throw new InvalidOperationException("Cannot Start an uninitialized component!");

            if (Running)
                throw new InvalidOperationException("Cannot Startup a running component!");

            if (Deleted)
                throw new InvalidOperationException("Cannot Start a Deleted component!");

            Running = true;
        }

        /// <inheritdoc />
        public virtual void Shutdown()
        {
            if (!Initialized)
                throw new InvalidOperationException("Cannot Shutdown an uninitialized component!");

            if (!Running)
                throw new InvalidOperationException("Cannot Shutdown an unstarted component!");

            if (Deleted)
                throw new InvalidOperationException("Cannot Shutdown a Deleted component!");

            Running = false;
        }

        /// <inheritdoc />
        public virtual void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _netSyncEnabled, "netsync", true);
        }

        /// <inheritdoc />
        public void Dirty()
        {
            if (Owner != null)
            {
                Owner.Dirty();
                LastModifiedTick = Owner.EntityManager.CurrentTick;
            }
        }

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
        /// <param name="channel">Network channel to send the message over. If null, broadcast to all channels.</param>
        protected void SendNetworkMessage(ComponentMessage message, INetChannel channel = null)
        {
            Owner.SendNetworkMessage(this, message, channel);
        }

        /// <inheritdoc />
        public virtual void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null) { }

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
    }
}
