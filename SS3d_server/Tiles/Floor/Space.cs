using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Tiles.Floor
{
    public class Space : Tile
    {
         public Space()
            : base()
        {
            tileType = TileType.Space;
            gasPermeable = true;
            gasSink = true;
        }
    }
}
