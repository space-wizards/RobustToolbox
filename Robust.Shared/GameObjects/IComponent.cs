using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    /// <remarks>
    ///     Base component for the ECS system.
    ///     All discoverable implementations of IComponent must override the <see cref="Name" />.
    ///     Instances are dynamically instantiated by a <c>ComponentFactory</c>, and will have their IoC Dependencies resolved.
    /// </remarks>
    public interface IComponent : IEntityEventSubscriber
    {
        /// <summary>
        ///     Represents the network ID for the component.
        ///     The network ID is used to determine which component will receive the component state
        ///     on the other side of the network.
        ///     If this is <c>null</c>, the component is not replicated across the network.
        /// </summary>
        /// <seealso cref="NetworkSynchronizeExistence" />
        /// <seealso cref="IComponentRegistration.NetID" />
        uint? NetID { get; }

        /// <summary>
        ///     Name that this component is represented with in prototypes.
        /// </summary>
        /// <seealso cref="IComponentRegistration.Name" />
        string Name { get; }

        /// <summary>
        ///     Whether the Owner has been paused.
        /// </summary>
        bool Paused { get; }

        /// <summary>
        ///     Whether the client should synchronize component additions and removals.
        ///     If this is false and the component gets added or removed server side, the client will not do the same.
        ///     If this is true and the server adds or removes the component, the client will do as such too.
        ///     This flag has no effect if <see cref="NetID" /> is <c>null</c>.
        ///     This is disabled by default, usually the client builds their instance from a prototype.
        /// </summary>
        /// <seealso cref="IComponentRegistration.NetworkSynchronizeExistence" />
        bool NetworkSynchronizeExistence { get; }

        /// <summary>
        ///     Whether this component should be synchronized with clients when modified.
        ///     If this is true, the server will synchronize all client instances with the data in this instance.
        ///     If this is false, clients can modify the data in their instances without being overwritten by the server.
        ///     This flag has no effect if <see cref="NetID" /> is <c>null</c>.
        ///     This is enabled by default.
        /// </summary>
        bool NetSyncEnabled { get; }

        /// <summary>
        ///     Entity that this component is attached to.
        /// </summary>
        IEntity Owner { get; }

        /// <summary>
        /// Component has been properly initialized.
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        ///     This is true when the component is active. Set this value to start or stop the component.
        /// </summary>
        bool Running { get; set; }

        /// <summary>
        ///     True if the component has been removed from its owner, AKA deleted.
        /// </summary>
        bool Deleted { get; }

        /// <summary>
        ///     Marks the component as dirty so that the network will re-sync it with clients.
        /// </summary>
        void Dirty();

        /// <summary>
        ///     This is the tick the component was created.
        /// </summary>
        GameTick CreationTick { get; }

        /// <summary>
        ///     This is the last game tick Dirty() was called.
        /// </summary>
        GameTick LastModifiedTick { get; }

        /// <summary>
        ///     Called when the component is removed from an entity.
        ///     Shuts down the component.
        ///     This should be called AFTER any inheriting classes OnRemove code has run. This should be last.
        /// </summary>
        void OnRemove();

        /// <summary>
        ///     Called when all of the entity's other components have been added and are available,
        ///     But are not necessarily initialized yet. DO NOT depend on the values of other components to be correct.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Handles a local incoming component message.
        /// </summary>
        /// <param name="message">Incoming event message.</param>
        /// <param name="component">The local component that sent the message.</param>
        void HandleMessage(ComponentMessage message, IComponent? component);

        /// <summary>
        ///     Handles an incoming component message from the server.
        /// </summary>
        /// <param name="message">Incoming event message.</param>
        /// <param name="netChannel">The channel of the remote client that sent the message.</param>
        /// <param name="session">The session data for the player who sent this message. Null if this is a client.</param>
        void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null);

        /// <summary>
        ///     Get the component's state for replicating on the client.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>ComponentState object</returns>
        ComponentState GetComponentState(ICommonSession player);

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
