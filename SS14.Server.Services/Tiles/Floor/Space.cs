using SS14.Server.Services.Map;
using System.Drawing;

namespace SS14.Server.Services.Tiles
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