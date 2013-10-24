using ServerServices.Map;
using SS13_Shared;
using System.Drawing;

namespace ServerServices.Tiles
{
    public class Space : Tile
    {
        public Space(RectangleF rectangle, MapManager _map)
            : base(rectangle, _map)
        {
            GasPermeable = true;
            GasSink = true;
        }
    }
}