using Lidgren.Network;
using System.Drawing;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Map
{
    public delegate void TileChangeEvent(PointF tileWorldPosition);

    public interface IMapManager
    {
        event TileChangeEvent OnTileChanged;
        int GetTileSpacing();
        int GetWallThickness();
        void Shutdown();
        bool IsSolidTile(Vector2 pos);
        void HandleNetworkMessage(NetIncomingMessage message);
        void HandleAtmosDisplayUpdate(NetIncomingMessage message);


        ITile[] GetAllTilesIn(RectangleF Area);
        ITile[] GetAllFloorIn(RectangleF Area);
        ITile[] GetAllWallIn(RectangleF Area);

        ITile GetWallAt(Vector2 pos);
        ITile GetFloorAt(Vector2 pos);
        ITile[] GetAllTilesAt(Vector2 pos);

        int GetMapWidth();
        int GetMapHeight();

        void Init();
    }
}