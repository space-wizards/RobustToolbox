using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;

namespace ServerServices.Tiles.Floor
{
    public class Space : Tile
    {
        public Space(int x, int y, ServerServices.Map.Map _map)
            : base(x, y, _map)
        {
            tileType = TileType.Space;
            gasPermeable = true;
            gasSink = true;
        }
    }
}
