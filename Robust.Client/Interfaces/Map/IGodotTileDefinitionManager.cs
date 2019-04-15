using Robust.Shared.Interfaces.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Client.Interfaces.Map
{
    internal interface IGodotTileDefinitionManager : ITileDefinitionManager
    {
        Godot.TileSet TileSet { get; }
    }
}
