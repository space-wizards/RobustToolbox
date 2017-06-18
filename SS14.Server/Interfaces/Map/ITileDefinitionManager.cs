using System.Collections.Generic;
using SS14.Shared.IoC;

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
