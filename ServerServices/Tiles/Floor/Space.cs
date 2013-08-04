using ServerServices.Map;

namespace ServerServices.Tiles
{
    public class Space : Tile
    {
        public Space(int x, int y, MapManager _map)
            : base(x, y, _map)
        {
            GasPermeable = true;
            GasSink = true;
        }
    }
}