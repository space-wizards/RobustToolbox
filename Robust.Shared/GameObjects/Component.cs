using System;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    [Reflect(false)]
    [ImplicitDataDefinitionForInheritors]
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

        /// <inheritdoc />
        [ViewVariables]
        [DataField("netsync")]
        public bool NetSyncEnabled { get; set; } = true;

        /// <inheritdoc />
        [ViewVariables]
        public IEntity Owner { get; set; } = default!;

        /// <inheritdoc />
        [ViewVariables]
        public bool Paused => Owner.Paused;

        /// <inheritdoc />
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

        /// <inheritdoc />
        [ViewVariables]
        public GameTick CreationTick { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public GameTick LastModifiedTick { get; private set; }

        private static readonly ComponentAdd CompAddInstance = new();
        private static readonly ComponentInit CompInitInstance = new();
        private static readonly ComponentStartup CompStartupInstance = new();
        private static readonly ComponentShutdown CompShutdownInstance = new();
        private static readonly ComponentRemove CompRemoveInstance = new();

        private EntityEventBus GetBus()
        {
            // Apparently components are being created outside of the ComponentManager,
            // and the Owner is not being set correctly.
            // ReSharper disable once RedundantAssertionStatement
            DebugTools.AssertNotNull(Owner);

            return (EntityEventBus) Owner.EntityManager.EventBus;
        }

        /// <inheritdoc />
        public virtual void OnRemove()
        {
            if (Running)
                throw new InvalidOperationException("Cannot Remove a running entity!");

            // We have been marked for deletion by the Component Manager.
            Deleted = true;
            GetBus().RaiseComponentEvent(this, CompRemoveInstance);
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
            GetBus().RaiseComponentEvent(this, CompAddInstance);
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
            GetBus().RaiseComponentEvent(this, CompInitInstance);
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
            GetBus().RaiseComponentEvent(this, CompStartupInstance);
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
            GetBus().RaiseComponentEvent(this, CompShutdownInstance);
        }

        /// <inheritdoc />
        public void Dirty()
        {
            // Deserialization will cause this to be true.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if(Owner is null)
                return;

            Owner.Dirty();
            LastModifiedTick = Owner.EntityManager.CurrentTick;
        }

        /// <summary>
        ///     Sends a message to all other components in this entity.
        ///     This is an alias of 'Owner.SendMessage(this, message);'
        /// </summary>
        /// <param name="message">Message to send.</param>
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
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
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
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

    /// <summary>
    /// The component has been added to the entity. This is the first function
    /// to be called after the component has been allocated and (optionally) deserialized.
    /// </summary>
    public class ComponentAdd : EntityEventArgs { }

    /// <summary>
    /// Raised when all of the entity's other components have been added and are available,
    /// But are not necessarily initialized yet. DO NOT depend on the values of other components to be correct.
    /// </summary>
    public class ComponentInit : EntityEventArgs { }

    /// <summary>
    /// Starts up a component. This is called automatically after all components are Initialized and the entity is Initialized.
    /// This can be called multiple times during the component's life, and at any time.
    /// </summary>
    public class ComponentStartup : EntityEventArgs { }

    /// <summary>
    /// Shuts down the component. The is called Automatically by OnRemove. This can be called multiple times during
    /// the component's life, and at any time.
    /// </summary>
    public class ComponentShutdown : EntityEventArgs { }

    /// <summary>
    /// The component has been removed from the entity. This is the last function
    /// that is called before the component is freed.
    /// </summary>
    public class ComponentRemove : EntityEventArgs { }
}
