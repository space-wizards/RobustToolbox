using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerServices.Tiles.Floor
{
    public class Floor : Tile
    {
        public Floor(int x, int y, ServerServices.Map.Map _map)
            : base(x, y, _map)
        {
            tileType = TileType.Floor;
            gasPermeable = true;
        }

    }
}
