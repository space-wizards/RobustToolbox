using System.Collections.Generic;
using SS13_Shared.GO;
using SS13_Shared;

namespace ClientInterfaces.GOC
{
    public interface IEntityTemplate
    {
        string Name { get; set; }
        string Description { get; }
        List<int> MountingPoints { get; }
        KeyValuePair<int, int> PlacementOffset { get; }
        string PlacementMode { get; }

        IEnumerable<ComponentParameter> GetBaseSpriteParamaters();
    }
}
