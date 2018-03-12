using System;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <remarks>
    ///     Base component for the ECS system.
    ///     All discoverable implementations of IComponent must override the <see cref="Name" />.
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
        ///     Whether the client should synchronize component additions and removals.
        ///     If this is false and the component gets added or removed server side, the client will not do the same.
        ///     If this is true and the server adds or removes the component, the client will do as such too.
        ///     This flag has no effect if <see cref="NetID" /> is <c>null</c>.
        /// </summary>
        /// <seealso cref="IComponentRegistration.NetworkSynchronizeExistence" />
        bool NetworkSynchronizeExistence { get; }

        /// <summary>
        ///     Entity that this component is attached to.
        /// </summary>
        IEntity Owner { get; }

        /// <summary>
        ///     Class Type that deserializes this component. This is the type that GetComponentState returns,
        ///     and is the type that is passed to HandleComponentState.
        /// </summary>
        Type StateType { get; }

        /// <summary>
        ///     This is true when the component is active. This value is changed when Startup and Shutdown are called.
        /// </summary>
        bool Running { get; }

        /// <summary>
        ///     True if the component has been removed from its owner, AKA deleted.
        /// </summary>
        bool Deleted { get; }

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
        ///     Starts up a component. This is called automatically after all components are Initialized and the entity is Initialized.
        ///     This can be called multiple times during the component's life, and at any time.
        /// </summary>
        void Startup();

        /// <summary>
        ///     Shuts down the component. The is called Automatically by OnRemove. This can be called multiple times during
        ///     the component's life, and at any time.
        /// </summary>
        void Shutdown();

        /// <summary>
        ///     This allows setting of the component's parameters from YAML once it is instantiated.
        ///     This should basically be overridden by every inheriting component, as parameters will be different
        ///     across the board.
        /// </summary>
        void ExposeData(EntitySerializer serializer);

        /// <summary>
        ///     Main method for updating the component. This is called from a big loop in ComponentManager.
        /// </summary>
        /// <param name="frameTime"></param>
        [Obsolete("Components should be updated through a system.")]
        void Update(float frameTime);

        /// <summary>
        ///     Handles an incoming component message.
        /// </summary>
        /// <param name="owner">
        ///     Object that raised the event.
        ///     If the message was raised locally, this contains the object that raised it.
        ///     If the message was raised remotely, this contains the PlayerSession that sent it.
        /// </param>
        /// <param name="message">Message that was sent.</param>
        void HandleMessage(object owner, ComponentMessage message);

        /// <summary>
        ///     Get the component's state for replicating on the client.
        /// </summary>
        /// <returns>ComponentState object</returns>
        ComponentState GetComponentState();

        /// <summary>
        ///     Handles an incoming component state from the server.
        /// </summary>
        /// <param name="state"></param>
        void HandleComponentState(ComponentState state);
    }
}
