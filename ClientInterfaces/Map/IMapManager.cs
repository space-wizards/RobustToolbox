using System.Drawing;
using ClientInterfaces.Lighting;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using System;

namespace ClientInterfaces.Map
{
    public delegate void TileChangeEvent(Point tilePosition, PointF tileWorldPosition);

    public interface IMapManager
    {
        event TileChangeEvent OnTileChanged;
        int GetTileSpacing();
        void Shutdown();
        bool IsSolidTile(Vector2D pos);
        void HandleNetworkMessage(NetIncomingMessage message);
        void HandleAtmosDisplayUpdate(NetIncomingMessage message);

        /// <summary>
        /// Get Tile from World Position.
        /// </summary>
        ITile GetTileAt(Vector2D WorldPos);

        /// <summary>
        /// Get Tile from Array Position.
        /// </summary>
        ITile GetTileAt(int array_x, int array_y);

        Vector2D GetTileArrayPositionFromWorldPosition(float x, float z);
        Point GetTileArrayPositionFromWorldPosition(Vector2D pos);
        int GetMapWidth();
        int GetMapHeight();
        byte SetSprite(int x, int y);

        void Init();
        Size GetMapSizeWorld();

        Type GetTileTypeFromWorldPosition(Vector2D vector2D);
    }
}
