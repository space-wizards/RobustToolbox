using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.GameObjects;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Interfaces.GameObjects
{
    public interface IEntity
    {
        IEntityNetworkManager EntityNetworkManager { get; }
        IEntityManager EntityManager { get; }

        string Name { get; set; }
        int Uid { get; set; }

        bool Initialized { get; set; }

        EntityPrototype Prototype { get; set; }
        event EntityShutdownEvent OnShutdown;

        /// <summary>
        /// Called after the entity is construted by its prototype to load parameters
        /// from the prototype's <c>data</c> field.
        /// </summary>
        /// <remarks>
        /// This method does not get called in case no data field is provided.
        /// </remarks>
        /// <param name="parameters">A dictionary representing the YAML mapping in the <c>data</c> field.</param>
        void LoadData(YamlNode parameters);

        /// <summary>
        /// Match
        ///
        /// Allows us to fetch entities with a defined SET of components
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        bool Match(IEntityQuery query);

        /// <summary>
        /// Public method to add a component to an entity.
        /// Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="family">the family of component -- there can only be one at a time per family.</param>
        /// <param name="component">The component.</param>
        void AddComponent(ComponentFamily family, IComponent component);

        /// <summary>
        /// Public method to remove a component from an entity.
        /// Calls the onRemove method of the component, which handles removing it
        /// from the component manager and shutting down the component.
        /// </summary>
        /// <param name="family"></param>
        void RemoveComponent(ComponentFamily family);

        /// <summary>
        /// Checks to see if a component of a certain family exists
        /// </summary>
        /// <param name="family">componentfamily to check</param>
        /// <returns>true if component exists, false otherwise</returns>
        bool HasComponent(ComponentFamily family);

        T GetComponent<T>(ComponentFamily family) where T : class;

        /// <summary>
        /// Gets the component of the specified family, if it exists
        /// </summary>
        /// <param name="family">componentfamily to get</param>
        /// <returns></returns>
        IComponent GetComponent(ComponentFamily family);

        void Shutdown();
        List<IComponent> GetComponents();
        List<ComponentFamily> GetComponentFamilies();
        void SendMessage(object sender, ComponentMessageType type, params object[] args);

        /// <summary>
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        void SendMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies,
                         params object[] args);

        ComponentReplyMessage SendMessage(object sender, ComponentFamily family, ComponentMessageType type,
                                          params object[] args);

        /// <summary>
        /// Requests Description string from components and returns it. If no component answers, returns default description from template.
        /// </summary>
        string GetDescriptionString(); //This needs to go here since it can not be bound to any single component.

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="messageParams">Parameters</param>
        void SendComponentNetworkMessage(Component component, NetDeliveryMethod method, params object[] messageParams);

        /// <summary>
        /// Sends a message to the counterpart component on the server side
        /// </summary>
        /// <param name="component">Sending component</param>
        /// <param name="method">Net Delivery Method</param>
        /// <param name="recipient">The intended recipient netconnection (if null send to all)</param>
        /// <param name="messageParams">Parameters</param>
        void SendDirectedComponentNetworkMessage(Component component, NetDeliveryMethod method,
                                                 NetConnection recipient, params object[] messageParams);

        /// <summary>
        /// Sets up variables and shite
        /// </summary>
        void Initialize();

        void HandleNetworkMessage(IncomingEntityMessage message);

        /// <summary>
        /// Client method to handle an entity state object
        /// </summary>
        /// <param name="state"></param>
        void HandleEntityState(EntityState state);

        /// <summary>
        /// Serverside method to prepare an entity state object
        /// </summary>
        /// <returns></returns>
        EntityState GetEntityState();

        void SubscribeEvent<T>(EntityEventHandler<EntityEventArgs> evh, IEntityEventSubscriber s) where T : EntityEventArgs;
        void UnsubscribeEvent<T>(IEntityEventSubscriber s) where T : EntityEventArgs;
        void RaiseEvent(EntityEventArgs toRaise);
    }
}
