using Lidgren.Network;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.Map
{
    public interface ITileDefinitionManager : IEnumerable<ITileDefinition>
    {
        ushort Register(ITileDefinition tileDef);
        void RegisterServerTileMapping(NetIncomingMessage message);
        
        ITileDefinition this[string name] { get; }
        ITileDefinition this[int id] { get; }
        int Count { get; }
    }
}
