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
    /// All discoverable implementations of IComponent must override the Name property.
    /// </remarks>
    public interface IComponent : IEntityEventSubscriber
    {
        ComponentFamily Family { get; }
        IEntity Owner { get; }
        Type StateType { get; }

        /// <summary>
        /// Name that this component is represented with in prototypes and over the network.
        /// </summary>
        string Name { get; }

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
        void LoadParameters(Dictionary<string, YamlNode> mapping);

        /// <summary>
        /// Main method for updating the component. This is called from a big loop in Componentmanager.
        /// </summary>
        /// <param name="frameTime"></param>
        void Update(float frameTime);

        /// <summary>
        /// Recieve a message from another component within the owner entity
        /// </summary>
        /// <param name="sender">the component that sent the message</param>
        /// <param name="type">the message type in CGO.MessageType</param>
        /// <param name="list">parameters list</param>
        ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                             params object[] list);

        /// <summary>
        /// Get the component's state for synchronizing
        /// </summary>
        /// <returns>ComponentState object</returns>
        ComponentState GetComponentState();

        void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender);
        void HandleComponentState(dynamic state);

        /// <summary>
        /// Handles a message that a client has just instantiated a component
        /// </summary>
        /// <param name="netConnection"></param>
        void HandleInstantiationMessage(NetConnection netConnection);

        /// <summary>
        /// This gets a list of runtime-settable component parameters, with CURRENT VALUES
        /// If it isn't going to return a current value, it shouldn't return it at all.
        /// </summary>
        /// <returns></returns>
        IList<ComponentParameter> GetParameters();

        /// <summary>
        /// Gets all available SVars for the entity.
        /// This gets current values, or at least it should...
        /// </summary>
        /// <returns>Returns a list of component parameters for marshaling</returns>
        IList<MarshalComponentParameter> GetSVars();

        /// <summary>
        /// Sets a component parameter via the sVar interface. Only
        /// parameters that are registered as sVars will be set through this
        /// function.
        /// </summary>
        /// <param name="sVar">ComponentParameter</param>
        void SetSVar(MarshalComponentParameter sVar);
    }
}
