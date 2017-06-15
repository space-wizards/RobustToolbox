using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.IoC;

namespace SS14.Shared.Interfaces.Map
{
    public interface ITileDefinitionManager : IEnumerable<ITileDefinition>
    {
        void InitializeResources();
        ushort Register(ITileDefinition tileDef);
        void RegisterServerTileMapping(NetIncomingMessage message);

        ITileDefinition this[string name] { get; }
        ITileDefinition this[int id] { get; }
        int Count { get; }
    }
}
