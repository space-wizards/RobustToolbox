using System;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    [Reflect(false)]
    [ImplicitDataDefinitionForInheritorsAttribute]
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

        [DataField("netsync")]
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
        public IEntity Owner { get; set; } = default!;

        /// <inheritdoc />
        [ViewVariables]
        public bool Paused => Owner.Paused;

        /// <summary>
        ///     True if this entity is a client-only entity.
        ///     That is, it does not exist on the server, only THIS client.
        /// </summary>
        [ViewVariables]
        public bool IsClientSide => Owner.Uid.IsClientSide();

        [ViewVariables]
        public bool Initialized { get; private set; }

        private bool _running;
        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Running
        {
            get => _running;
            set
            {
                if(_running == value)
                    return;

                if(value)
                    Startup();
                else
                    Shutdown();

                _running = value;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; private set; }

        [ViewVariables]
        public GameTick CreationTick { get; private set; }

        [ViewVariables]
        public GameTick LastModifiedTick { get; private set; }

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

            CreationTick = Owner.EntityManager.CurrentTick;
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

        /// <summary>
        ///     Starts up a component. This is called automatically after all components are Initialized and the entity is Initialized.
        ///     This can be called multiple times during the component's life, and at any time.
        /// </summary>
        protected virtual void Startup()
        {
            if (!Initialized)
                throw new InvalidOperationException("Cannot Start an uninitialized component!");

            if (Running)
                throw new InvalidOperationException("Cannot Startup a running component!");

            if (Deleted)
                throw new InvalidOperationException("Cannot Start a Deleted component!");

            _running = true;
        }

        /// <summary>
        ///     Shuts down the component. The is called Automatically by OnRemove. This can be called multiple times during
        ///     the component's life, and at any time.
        /// </summary>
        protected virtual void Shutdown()
        {
            if (!Initialized)
                throw new InvalidOperationException("Cannot Shutdown an uninitialized component!");

            if (!Running)
                throw new InvalidOperationException("Cannot Shutdown an unstarted component!");

            if (Deleted)
                throw new InvalidOperationException("Cannot Shutdown a Deleted component!");

            _running = false;
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
        protected void SendNetworkMessage(ComponentMessage message, INetChannel? channel = null)
        {
            Owner.SendNetworkMessage(this, message, channel);
        }

        /// <inheritdoc />
        public virtual void HandleMessage(ComponentMessage message, IComponent? component) { }

        /// <inheritdoc />
        public virtual void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null) { }

        /// <param name="player"></param>
        /// <inheritdoc />
        public virtual ComponentState GetComponentState(ICommonSession player)
        {
            if (NetID == null)
                throw new InvalidOperationException($"Cannot make state for component without Net ID: {GetType()}");

            return new ComponentState(NetID.Value);
        }

        /// <inheritdoc />
        public virtual void HandleComponentState(ComponentState? curState, ComponentState? nextState) { }

        // these two methods clear the LastModifiedTick/CreationTick to mark it as "not different from prototype load".
        // This is used as optimization in the game state system to avoid sending redundant component data.
        internal virtual void ClearTicks()
        {
            LastModifiedTick = GameTick.Zero;
            ClearCreationTick();
        }

        internal void ClearCreationTick()
        {
            CreationTick = GameTick.Zero;
        }
    }
}
