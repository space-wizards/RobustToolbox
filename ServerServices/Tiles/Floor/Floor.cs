using ServerServices.Map;

namespace ServerServices.Tiles
{
    public class Floor : Tile
    {
        public Floor(int x, int y, MapManager _map)
            : base(x, y, _map)
        {
            StartWithAtmos = true;
            GasPermeable = true;
        }
    }
}