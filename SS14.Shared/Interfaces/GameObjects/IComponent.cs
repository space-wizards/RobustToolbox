using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <remarks>
    /// All discoverable implementations of IComponent must override the <see cref="Name" />.
    /// </remarks>
    public interface IComponent : IEntityEventSubscriber
    {
        /// <summary>
        /// Represents the network ID for the component.
        /// The network ID is used to determine which component will receive the component state
        /// on the other side of the network.
        /// If this is <c>null</c>, the component is not replicated across the network.
        /// </summary>
        /// <seealso cref="NetworkSynchronizeExistence" />
        /// <seealso cref="IComponentRegistration.NetID" />
        uint? NetID { get; }

        /// <summary>
        /// Name that this component is represented with in prototypes.
        /// </summary>
        /// <seealso cref="IComponentRegistration.Name" />
        string Name { get; }

        /// <summary>
        /// Whether the client should synchronize component additions and removals.
        /// If this is false and the component gets added or removed server side, the client will not do the same.
        /// If this is true and the server adds or removes the component, the client will do as such too.
        /// This flag has no effect if <see cref="NetID" /> is <c>null</c>.
        /// </summary>
        /// <seealso cref="IComponentRegistration.NetworkSynchronizeExistence" />
        bool NetworkSynchronizeExistence { get; }

        IEntity Owner { get; }

        /// <summary>
        /// Class Type that deserializes this component. This is the type that GetComponentState returns,
        /// and is the type that is passed to HandleComponentState.
        /// </summary>
        Type StateType { get; }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component
        /// </summary>
        void OnRemove();

        /// <summary>
        /// Called when the component gets added to an entity.
        /// </summary>
        /// <param name="owner"></param>
        void OnAdd(IEntity owner);

        /// <summary>
        /// Base method to shut down the component.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// This allows setting of the component's parameters from YAML once it is instantiated.
        /// This should basically be overridden by every inheriting component, as parameters will be different
        /// across the board.
        /// </summary>
        void LoadParameters(YamlMappingNode mapping);

        /// <summary>
        /// Main method for updating the component. This is called from a big loop in Componentmanager.
        /// </summary>
        /// <param name="frameTime"></param>
        void Update(float frameTime);

        /// <summary>
        /// Receive a message from another component within the owner entity
        /// </summary>
        /// <param name="sender">the component that sent the message</param>
        /// <param name="type">the message type in CGO.MessageType</param>
        /// <param name="list">parameters list</param>
        ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                             params object[] list);

        /// <summary>
        /// Get the component's state for synchronizing
        /// </summary>
        /// <returns>ComponentState object</returns>
        ComponentState GetComponentState();

        void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender);

        /// <summary>
        /// Handles an incoming component state from the server.
        /// </summary>
        /// <param name="state"></param>
        void HandleComponentState(ComponentState state);
    }
}
