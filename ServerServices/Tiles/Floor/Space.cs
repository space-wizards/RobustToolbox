using ServerServices.Map;
using SS13_Shared;

namespace ServerServices.Tiles
{
    public class Space : Tile
    {
        public Space(Vector2 pos, MapManager _map)
            : base(pos, _map)
        {
            GasPermeable = true;
            GasSink = true;
        }
    }
}