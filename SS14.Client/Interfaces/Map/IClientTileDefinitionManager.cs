using SS14.Shared.Interfaces.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Interfaces.Map
{
    public interface IClientTileDefinitionManager : ITileDefinitionManager
    {
        #if GODOT
        Godot.TileSet TileSet { get; }
        #endif
    }
}
