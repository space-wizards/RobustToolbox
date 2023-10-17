using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    [Reflect(false)]
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class Component : IComponent
    {
        [DataField("netsync")]
        [ViewVariables(VVAccess.ReadWrite)]
        private bool _netSync { get; set; } = true;

        internal bool Networked = true;

        /// <inheritdoc />
        public bool NetSyncEnabled
        {
            get => Networked && _netSync;
            set => _netSync = value;
        }

        /// <inheritdoc />
        [ViewVariables]
        [Obsolete("Update your API to allow accessing Owner through other means")]
        public EntityUid Owner { get; set; } = EntityUid.Invalid;

        /// <inheritdoc />
        [ViewVariables]
        public ComponentLifeStage LifeStage { get; internal set; } = ComponentLifeStage.PreAdd;

        /// <summary>
        ///     If true, and if this is a networked component, then component data will only be sent to players if their
        ///     controlled entity is the owner of this component. This is less performance intensive than <see cref="SessionSpecific"/>.
        /// </summary>
        public virtual bool SendOnlyToOwner => false;

        /// <summary>
        ///     If true, and if this is a networked component, then this component will cause <see
        ///     cref="ComponentGetStateAttemptEvent"/> events to be raised to check whether a given player should
        ///     receive this component's state.
        /// </summary>
        public virtual bool SessionSpecific => false;

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
        public GameTick CreationTick { get; internal set; }

        /// <inheritdoc />
        [ViewVariables]
        public GameTick LastModifiedTick { get; internal set; }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component.
        /// The component has already been marked as deleted in the component manager.
        /// </summary>
        protected internal virtual void OnRemove()
        {
            LifeStage = ComponentLifeStage.Deleted;
        }

        /// <inheritdoc />
        [Obsolete]
        public void Dirty(IEntityManager? entManager = null)
        {
            IoCManager.Resolve(ref entManager);
            entManager.Dirty(this);
        }

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
    public enum ComponentLifeStage : byte
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
        /// Currently being removed from its entity.
        /// </summary>
        Removing,

        /// <summary>
        /// Removed from its entity, and is deleted.
        /// </summary>
        Deleted,
    }

    /// <summary>
    /// WARNING: Do not subscribe to this unless you know what you are doing!
    /// The component has been added to the entity. This is the first function
    /// to be called after the component has been allocated and (optionally) deserialized.
    /// </summary>
    [ComponentEvent]
    public readonly record struct ComponentAdd;

    /// <summary>
    /// Raised when all of the entity's other components have been added and are available,
    /// But are not necessarily initialized yet. DO NOT depend on the values of other components to be correct.
    /// </summary>
    [ComponentEvent]
    public sealed class ComponentInit : EntityEventArgs { }

    /// <summary>
    /// Starts up a component. This is called automatically after all components are Initialized and the entity is Initialized.
    /// This can be called multiple times during the component's life, and at any time.
    /// </summary>
    [ComponentEvent]
    public sealed class ComponentStartup : EntityEventArgs { }

    /// <summary>
    /// Shuts down the component. The is called Automatically by OnRemove. This can be called multiple times during
    /// the component's life, and at any time.
    /// </summary>
    [ComponentEvent]
    public sealed class ComponentShutdown : EntityEventArgs { }

    /// <summary>
    /// The component has been removed from the entity. This is the last function
    /// that is called before the component is freed.
    /// </summary>
    [ComponentEvent]
    public sealed class ComponentRemove : EntityEventArgs { }
}
