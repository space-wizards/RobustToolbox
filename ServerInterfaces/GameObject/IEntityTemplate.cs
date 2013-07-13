using System.Xml.Linq;
using SS13_Shared;
using SS13_Shared.GO;

namespace ServerInterfaces.GameObject
{
    public interface IEntityTemplate
    {
        /// <summary>
        /// The Placement mode used for server-initiated placement. This is used for placement during normal gameplay. The clientside version controls the placement type for editor and admin spawning.
        /// </summary>
        string PlacementMode { get; }

        /// <summary>
        /// The Range this entity can be placed from.
        /// </summary>
        int PlacementRange { get; }

        string Name { get; set; }

        /// <summary>
        /// Creates an entity from this template
        /// </summary>
        /// <returns></returns>
        IEntity CreateEntity(IEntityNetworkManager entityNetworkManager);

        /// <summary>
        /// Adds a component type to the entity template
        /// </summary>
        void AddComponent(string componentType);

        /// <summary>
        /// Sets a parameter for a component type for this template
        /// </summary>
        /// <param name="t">The type of the component to set a parameter on</param>
        /// <param name="parameter">The parameter object</param>
        void SetParameter(string componenttype, ComponentParameter parameter);

        void LoadFromXML(XElement templateElement);
    }
}