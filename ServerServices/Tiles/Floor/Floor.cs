using ServerServices.Map;
using SS13_Shared;
using System.Drawing;

namespace ServerServices.Tiles
{
    public class Floor : Tile
    {
        public Floor(RectangleF rectangle, MapManager _map)
            : base(rectangle, _map)
        {
            StartWithAtmos = true;
            GasPermeable = true;
        }
    }
}