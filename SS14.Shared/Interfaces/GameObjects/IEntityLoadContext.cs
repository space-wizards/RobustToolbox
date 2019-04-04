using System.Collections.Generic;
using SS14.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <summary>
    ///     Interface used to allow the map loader to override prototype data with map data.
    /// </summary>
    internal interface IEntityLoadContext
    {
        /// <summary>
        ///     Gets the serializer used to ExposeData a specific component.
        /// </summary>
        ObjectSerializer GetComponentSerializer(string componentName, YamlMappingNode protoData);

        /// <summary>
        ///     Gets extra component names that must also be instantiated on top of the ones defined in the prototype,
        ///     (and then deserialized with <see cref="GetComponentSerializer"/>)
        /// </summary>
        IEnumerable<string> GetExtraComponentTypes();
    }
}
