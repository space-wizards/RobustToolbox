using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Tiles.Floor
{
    public class Floor : Tile
    {
        public Floor()
            : base()
        {
            tileType = TileType.Floor;
            gasPermeable = true;
        }

    }
}
