using System;
using System.Drawing;
using GorgonLibrary;
using Lidgren.Network;

namespace ClientInterfaces.Map
{
    public delegate void TileChangeEvent(PointF tileWorldPosition);

    public interface IMapManager
    {
        event TileChangeEvent OnTileChanged;
        int GetTileSpacing();
        int GetWallThickness();
        void Shutdown();
        bool IsSolidTile(Vector2D pos);
        void HandleNetworkMessage(NetIncomingMessage message);
        void HandleAtmosDisplayUpdate(NetIncomingMessage message);


        ITile[] GetAllTilesIn(RectangleF Area);
        ITile[] GetAllFloorIn(RectangleF Area);
        ITile[] GetAllWallIn(RectangleF Area);

        ITile GetWallAt(Vector2D pos);
        ITile GetFloorAt(Vector2D pos);
        ITile[] GetAllTilesAt(Vector2D pos);

        int GetMapWidth();
        int GetMapHeight();

        void Init();
    }
}