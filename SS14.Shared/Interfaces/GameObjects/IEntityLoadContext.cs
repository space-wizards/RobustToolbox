using System.Collections.Generic;
using SS14.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Interfaces.GameObjects
{
    interface IEntityLoadContext
    {
        ObjectSerializer GetComponentSerializer(string componentName, YamlMappingNode protoData);
        IEnumerable<string> GetExtraComponentTypes();
    }
}
