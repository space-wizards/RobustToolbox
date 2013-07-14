using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;

namespace ServerServices.Tiles
{
    public class Space : Tile
    {
        public Space(int x, int y, ServerServices.Map.MapManager _map)
            : base(x, y, _map)
        {
            
            GasPermeable = true;
            GasSink = true;
        }
    }
}
