using System.Collections.Generic;
using SS13_Shared;

namespace ClientInterfaces.GOC
{
    public interface IEntityTemplate
    {
        string Name { get; set; }
        string Description { get; }
        List<int> MountingPoints { get; }
        KeyValuePair<int, int> PlacementOffset { get; }
        PlacementOption PlacementMode { get; }

        IEnumerable<ComponentParameter> GetBaseSpriteParamaters();
    }
}
