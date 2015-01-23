using SS14.Server.Services.Map;
using System.Drawing;

namespace SS14.Server.Services.Tiles
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