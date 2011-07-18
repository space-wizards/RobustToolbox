using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3d_server.Tiles.Atmos;

namespace SS3d_server.Tiles
{
    public class Tile
    {
        public TileType tileType;
        public GasCell gasCell;
        public bool gasPermeable = false;
        public bool gasSink = false;

        public Tile()
        {

        }
    }
}
