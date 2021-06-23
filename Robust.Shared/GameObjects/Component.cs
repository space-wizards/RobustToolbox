using System;
using Robust.Shared.GameStates;
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
        public ComponentLifeStage LifeStage { get; private set; } = ComponentLifeStage.PreAdd;

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.PreAdd" /> to <see cref="ComponentLifeStage.Added" />,
        /// calling <see cref="OnAdd" />.
        /// </summary>
        internal void LifeAddToEntity()
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.PreAdd);

            LifeStage = ComponentLifeStage.Adding;
            OnAdd();

            DebugTools.Assert(LifeStage == ComponentLifeStage.Added, $"Component {this.GetType().Name} did not call base {nameof(OnAdd)} in derived method.");
        }

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.Added" /> to <see cref="ComponentLifeStage.Initialized" />,
        /// calling <see cref="Initialize" />.
        /// </summary>
        internal void LifeInitialize()
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Added);

            LifeStage = ComponentLifeStage.Initializing;
            Initialize();

            DebugTools.Assert(LifeStage == ComponentLifeStage.Initialized, $"Component {this.GetType().Name} did not call base {nameof(Initialize)} in derived method.");
        }

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.Initialized" /> to
        /// <see cref="ComponentLifeStage.Running" />, calling <see cref="Startup" />.
        /// </summary>
        internal void LifeStartup()
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Initialized);

            LifeStage = ComponentLifeStage.Starting;
            Startup();

            DebugTools.Assert(LifeStage == ComponentLifeStage.Running, $"Component {this.GetType().Name} did not call base {nameof(Startup)} in derived method.");
        }

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.Running" /> to <see cref="ComponentLifeStage.Stopped" />,
        /// calling <see cref="Shutdown" />.
        /// </summary>
        /// <remarks>
        /// Components are allowed to remove themselves in their own Startup function.
        /// </remarks>
        internal void LifeShutdown()
        {
            // Starting allows a component to remove itself in it's own Startup function.
            DebugTools.Assert(LifeStage == ComponentLifeStage.Starting || LifeStage == ComponentLifeStage.Running);

            LifeStage = ComponentLifeStage.Stopping;
            Shutdown();

            DebugTools.Assert(LifeStage == ComponentLifeStage.Stopped, $"Component {this.GetType().Name} did not call base {nameof(Shutdown)} in derived method.");
        }

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.Stopped" /> to <see cref="ComponentLifeStage.Deleted" />,
        /// calling <see cref="OnRemove" />.
        /// </summary>
        internal void LifeRemoveFromEntity()
        {
            // can be called at any time after PreAdd, including inside other life stage events.
            DebugTools.Assert(LifeStage != ComponentLifeStage.PreAdd);

            LifeStage = ComponentLifeStage.Removing;
            OnRemove();

            DebugTools.Assert(LifeStage == ComponentLifeStage.Deleted, $"Component {this.GetType().Name} did not call base {nameof(OnRemove)} in derived method.");
        }

        /// <inheritdoc />
        [ViewVariables]
        public bool Initialized => LifeStage >= ComponentLifeStage.Initializing;

        /// <inheritdoc />
        [ViewVariables]
        public bool Running => ComponentLifeStage.Starting <= LifeStage && LifeStage <= ComponentLifeStage.Stopping;

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted => LifeStage >= ComponentLifeStage.Removing;

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

        /// <summary>
        /// Called when the component gets added to an entity.
        /// </summary>
        protected virtual void OnAdd()
        {
            CreationTick = Owner.EntityManager.CurrentTick;
            GetBus().RaiseComponentEvent(this, CompAddInstance);
            LifeStage = ComponentLifeStage.Added;
        }

        /// <summary>
        /// Called when all of the entity's other components have been added and are available,
        /// But are not necessarily initialized yet. DO NOT depend on the values of other components to be correct.
        /// </summary>
        protected virtual void Initialize()
        {
            GetBus().RaiseComponentEvent(this, CompInitInstance);
            LifeStage = ComponentLifeStage.Initialized;
        }

        /// <summary>
        ///     Starts up a component. This is called automatically after all components are Initialized and the entity is Initialized.
        /// </summary>
        /// <remarks>
        /// Components are allowed to remove themselves in their own Startup function.
        /// </remarks>
        protected virtual void Startup()
        {
            GetBus().RaiseComponentEvent(this, CompStartupInstance);
            LifeStage = ComponentLifeStage.Running;
        }

        /// <summary>
        ///     Shuts down the component. The is called Automatically by OnRemove.
        /// </summary>
        protected virtual void Shutdown()
        {
            GetBus().RaiseComponentEvent(this, CompShutdownInstance);
            LifeStage = ComponentLifeStage.Stopped;
        }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component.
        /// The component has already been marked as deleted in the component manager.
        /// </summary>
        protected virtual void OnRemove()
        {
            GetBus().RaiseComponentEvent(this, CompRemoveInstance);
            LifeStage = ComponentLifeStage.Deleted;
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
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public virtual void HandleMessage(ComponentMessage message, IComponent? component) { }

        /// <inheritdoc />
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public virtual void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null) { }

        /// <inheritdoc />
        public uint? GetNetId()
        {
            // It is assumed that this respects the inheritance order, so it will always return the child attribute before the parent.
            // This would allow you to safely change the NetID of child classes.
            if (Attribute.GetCustomAttribute(GetType(), typeof(NetIDAttribute)) is NetIDAttribute attribute)
                return attribute.NetId;

            return null;
        }

        private static readonly ComponentState DefaultComponentState = new();

        /// <param name="player"></param>
        /// <inheritdoc />
        public virtual ComponentState GetComponentState(ICommonSession player)
        {
            var netId = GetNetId();

            if (!netId.HasValue)
                throw new InvalidOperationException($"Calling base {nameof(GetComponentState)} without having a NetId.");

            return DefaultComponentState;
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
    /// The life stages of an ECS component.
    /// </summary>
    public enum ComponentLifeStage
    {
        /// <summary>
        /// The component has just been allocated.
        /// </summary>
        PreAdd = 0,

        /// <summary>
        /// Currently being added to an entity.
        /// </summary>
        Adding,

        /// <summary>
        /// Has been added to an entity.
        /// </summary>
        Added,

        /// <summary>
        /// Currently being initialized.
        /// </summary>
        Initializing,

        /// <summary>
        /// Has been initialized.
        /// </summary>
        Initialized,

        /// <summary>
        /// Currently being started up.
        /// </summary>
        Starting,

        /// <summary>
        /// Has started up.
        /// </summary>
        Running,

        /// <summary>
        /// Currently shutting down.
        /// </summary>
        Stopping,

        /// <summary>
        /// Has been shut down.
        /// </summary>
        Stopped,

        /// <summary>
        /// Currently being removed from it's entity.
        /// </summary>
        Removing,

        /// <summary>
        /// Removed from it's entity, and is deleted.
        /// </summary>
        Deleted,
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
