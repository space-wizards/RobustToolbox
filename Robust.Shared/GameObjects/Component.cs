using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
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
        [ViewVariables(VVAccess.ReadOnly)]
        [Obsolete("Resolve IComponentFactory and call GetComponentName instead")]
        public virtual string Name => IoCManager.Resolve<IComponentFactory>().GetComponentName(GetType());

        /// <inheritdoc />
        [DataField("netsync")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool _netSync { get; set; } = true;

        internal bool Networked = true;

        public bool NetSyncEnabled
        {
            get => Networked && _netSync;
            set => _netSync = value;
        }

        /// <inheritdoc />
        [ViewVariables]
        public EntityUid Owner { get; set; } = EntityUid.Invalid;

        /// <inheritdoc />
        [ViewVariables]
        public ComponentLifeStage LifeStage { get; private set; } = ComponentLifeStage.PreAdd;

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

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.PreAdd" /> to <see cref="ComponentLifeStage.Added" />,
        /// after raising a <see cref="ComponentAdd"/> event.
        /// </summary>
        internal void LifeAddToEntity(IEntityManager entManager, CompIdx type)
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.PreAdd);

            LifeStage = ComponentLifeStage.Adding;
            CreationTick = entManager.CurrentTick;
            // networked components are assumed to be dirty when added to entities. See also: ClearTicks()
            LastModifiedTick = entManager.CurrentTick;
            entManager.EventBus.RaiseComponentEvent(this, type, CompAddInstance);
            LifeStage = ComponentLifeStage.Added;
        }

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.Added" /> to <see cref="ComponentLifeStage.Initialized" />,
        /// calling <see cref="Initialize" />.
        /// </summary>
        internal void LifeInitialize(IEntityManager entManager, CompIdx type)
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Added);

            LifeStage = ComponentLifeStage.Initializing;
            entManager.EventBus.RaiseComponentEvent(this, type, CompInitInstance);
            Initialize();

#if DEBUG
            if (LifeStage != ComponentLifeStage.Initialized)
            {
                DebugTools.Assert($"Component {this.GetType().Name} did not call base {nameof(Initialize)} in derived method.");
            }
#endif
        }

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.Initialized" /> to
        /// <see cref="ComponentLifeStage.Running" />, calling <see cref="Startup" />.
        /// </summary>
        internal void LifeStartup(IEntityManager entManager)
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Initialized);

            LifeStage = ComponentLifeStage.Starting;
            entManager.EventBus.RaiseComponentEvent(this, CompStartupInstance);
            Startup();

#if DEBUG
            if (LifeStage != ComponentLifeStage.Running)
            {
                DebugTools.Assert($"Component {this.GetType().Name} did not call base {nameof(Startup)} in derived method.");
            }
#endif
        }

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.Running" /> to <see cref="ComponentLifeStage.Stopped" />,
        /// calling <see cref="Shutdown" />.
        /// </summary>
        /// <remarks>
        /// Components are allowed to remove themselves in their own Startup function.
        /// </remarks>
        internal void LifeShutdown(IEntityManager entManager)
        {
            // Starting allows a component to remove itself in it's own Startup function.
            DebugTools.Assert(LifeStage == ComponentLifeStage.Starting || LifeStage == ComponentLifeStage.Running);

            LifeStage = ComponentLifeStage.Stopping;
            entManager.EventBus.RaiseComponentEvent(this, CompShutdownInstance);
            Shutdown();

#if DEBUG
            if (LifeStage != ComponentLifeStage.Stopped)
            {
                DebugTools.Assert($"Component {this.GetType().Name} did not call base {nameof(Shutdown)} in derived method.");
            }
#endif
        }

        /// <summary>
        /// Increases the life stage from <see cref="ComponentLifeStage.Stopped" /> to <see cref="ComponentLifeStage.Deleted" />,
        /// calling <see cref="OnRemove" />.
        /// </summary>
        internal void LifeRemoveFromEntity(IEntityManager entManager)
        {
            // can be called at any time after PreAdd, including inside other life stage events.
            DebugTools.Assert(LifeStage != ComponentLifeStage.PreAdd);

            LifeStage = ComponentLifeStage.Removing;
            entManager.EventBus.RaiseComponentEvent(this, CompRemoveInstance);

            OnRemove();

#if DEBUG
            if (LifeStage != ComponentLifeStage.Deleted)
            {
                DebugTools.Assert($"Component {this.GetType().Name} did not call base {nameof(OnRemove)} in derived method.");
            }
#endif
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
        public GameTick LastModifiedTick { get; internal set; }

        private static readonly ComponentAdd CompAddInstance = new();
        private static readonly ComponentInit CompInitInstance = new();
        private static readonly ComponentStartup CompStartupInstance = new();
        private static readonly ComponentShutdown CompShutdownInstance = new();
        private static readonly ComponentRemove CompRemoveInstance = new();

        /// <summary>
        /// Called when all of the entity's other components have been added and are available,
        /// But are not necessarily initialized yet. DO NOT depend on the values of other components to be correct.
        /// </summary>
        protected virtual void Initialize()
        {
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
            LifeStage = ComponentLifeStage.Running;
        }

        /// <summary>
        ///     Shuts down the component. The is called Automatically by OnRemove.
        /// </summary>
        protected virtual void Shutdown()
        {
            LifeStage = ComponentLifeStage.Stopped;
        }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component.
        /// The component has already been marked as deleted in the component manager.
        /// </summary>
        protected virtual void OnRemove()
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

        private static readonly ComponentState DefaultComponentState = new();

        /// <inheritdoc />
        public virtual ComponentState GetComponentState()
        {
            DebugTools.Assert(
                Attribute.GetCustomAttribute(GetType(), typeof(NetworkedComponentAttribute)) != null,
                $"Calling base {nameof(GetComponentState)} without being networked.");

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
        /// Currently being removed from its entity.
        /// </summary>
        Removing,

        /// <summary>
        /// Removed from its entity, and is deleted.
        /// </summary>
        Deleted,
    }

    /// <summary>
    /// The component has been added to the entity. This is the first function
    /// to be called after the component has been allocated and (optionally) deserialized.
    /// </summary>
    [ComponentEvent]
    public sealed class ComponentAdd : EntityEventArgs { }

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
