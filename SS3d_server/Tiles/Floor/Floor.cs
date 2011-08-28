using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Tiles.Floor
{
    public class Floor : Tile
    {
        public Floor(int x, int y, SS3D_Server.Modules.Map.Map _map)
            : base(x, y, _map)
        {
            tileType = TileType.Floor;
            gasPermeable = true;
        }

    }
}
