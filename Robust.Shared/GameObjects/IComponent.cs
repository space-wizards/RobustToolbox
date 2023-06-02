using System;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    /// <remarks>
    ///     Base component for the ECS system.
    ///     All discoverable implementations of IComponent must override the <see cref="Name" />.
    ///     Instances are dynamically instantiated by a <c>ComponentFactory</c>, and will have their IoC Dependencies resolved.
    /// </remarks>
    public interface IComponent
    {
        /// <summary>
        ///     The current lifetime stage of this component. You can use this to check
        ///     if the component is initialized or being deleted.
        /// </summary>
        ComponentLifeStage LifeStage { get; }

        /// <summary>
        ///     Whether this component should be synchronized with clients when modified.
        ///     If this is true, the server will synchronize all client instances with the data in this instance.
        ///     If this is false, clients can modify the data in their instances without being overwritten by the server.
        ///     This flag has no effect if <see cref="NetworkedComponentAttribute" /> is not defined on the component.
        ///     This is enabled by default.
        /// </summary>
        bool NetSyncEnabled { get; set; }

        /// <summary>
        ///     Entity that this component is attached to.
        /// </summary>
        /// <seealso cref="EntityQueryEnumerator{TComp1}"/>
        EntityUid Owner { get; }

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
        void Dirty(IEntityManager? entManager = null);

        /// <summary>
        ///     This is the tick the component was created.
        /// </summary>
        GameTick CreationTick { get; }

        /// <summary>
        ///     This is the last game tick Dirty() was called.
        /// </summary>
        GameTick LastModifiedTick { get; }

        /// <summary>
        ///     Get the component's state for replicating on the client.
        /// </summary>
        /// <returns>ComponentState object</returns>
        ComponentState GetComponentState();

        /// <summary>
        ///     Handles an incoming component state from the server.
        /// </summary>
        /// <remarks>
        /// This function should only be called on the client.
        /// Both, one, or neither of the two states can be null.
        /// On the next tick, curState will be nextState.
        /// Passing null for both arguments should do nothing.
        /// </remarks>
        /// <param name="curState">Current component state for this tick.</param>
        /// <param name="nextState">Next component state for the next tick.</param>
        void HandleComponentState(ComponentState? curState, ComponentState? nextState);
    }
}
