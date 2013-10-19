using ServerServices.Map;
using SS13_Shared;

namespace ServerServices.Tiles
{
    public class Floor : Tile
    {
        public Floor(Vector2 pos, MapManager _map)
            : base(pos, _map)
        {
            StartWithAtmos = true;
            GasPermeable = true;
        }
    }
}