using System;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    /// <remarks>
    ///     Base component for the ECS system.
    ///     Instances are dynamically instantiated by a <c>ComponentFactory</c>, and will have their IoC Dependencies resolved.
    /// </remarks>
    [ImplicitDataDefinitionForInheritors]
    public partial interface IComponent
    {
        /// <summary>
        ///     The current lifetime stage of this component. You can use this to check
        ///     if the component is initialized or being deleted.
        /// </summary>
        ComponentLifeStage LifeStage { get; internal set; }

        internal bool Networked { get; set; }

        /// <summary>
        ///     Whether this component should be synchronized with clients when modified.
        ///     If this is true, the server will synchronize all client instances with the data in this instance.
        ///     If this is false, clients can modify the data in their instances without being overwritten by the server.
        ///     This flag has no effect if <see cref="NetworkedComponentAttribute" /> is not defined on the component.
        ///     This is enabled by default.
        /// </summary>
        bool NetSyncEnabled { get; set; }

        /// <summary>
        ///     If true, and if this is a networked component, then component data will only be sent to players if their
        ///     controlled entity is the owner of this component. This is less performance intensive than <see cref="SessionSpecific"/>.
        /// </summary>
        bool SendOnlyToOwner { get; }

        /// <summary>
        ///     If true, and if this is a networked component, then this component will cause <see
        ///     cref="ComponentGetStateAttemptEvent"/> events to be raised to check whether a given player should
        ///     receive this component's state.
        /// </summary>
        bool SessionSpecific { get; }

        /// <summary>
        ///     Entity that this component is attached to.
        /// </summary>
        /// <seealso cref="EntityQueryEnumerator{TComp1}"/>
        [Obsolete("Update your API to allow accessing Owner through other means")]
        EntityUid Owner { get; set; }

        /// <summary>
        /// Component has been (or is currently being) initialized.
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        ///     This is true when the component is active.
        /// </summary>
        bool Running { get; }

        /// <summary>
        ///     True if the component has been removed from its owner, AKA deleted.
        /// </summary>
        bool Deleted { get; }

        /// <summary>
        ///     Marks the component as dirty so that the network will re-sync it with clients.
        /// </summary>
        [Obsolete]
        void Dirty(IEntityManager? entManager = null);

        /// <summary>
        ///     This is the tick the component was created.
        /// </summary>
        GameTick CreationTick { get; internal set; }

        /// <summary>
        ///     This is the last game tick Dirty() was called.
        /// </summary>
        GameTick LastModifiedTick { get; internal set; }

        internal void ClearTicks();

        internal void ClearCreationTick();
    }
}
