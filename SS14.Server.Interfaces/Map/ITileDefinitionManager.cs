using System.Collections.Generic;

namespace SS14.Server.Interfaces.Map
{
    public interface ITileDefinitionManager : IEnumerable<ITileDefinition>
    {
        ushort Register(ITileDefinition tileDef);
        
        ITileDefinition this[string name] { get; }
        ITileDefinition this[int id] { get; }
        int Count { get; }
    }
}
