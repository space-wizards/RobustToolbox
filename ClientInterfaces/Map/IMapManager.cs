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
        void Shutdown();
        bool IsSolidTile(Vector2D pos);
        void HandleNetworkMessage(NetIncomingMessage message);
        void HandleAtmosDisplayUpdate(NetIncomingMessage message);

        /// <summary>
        /// Get Tile from World Position.
        /// </summary>
        ITile GetITileAt(Vector2D WorldPos);
        ITile[] GetITilesIn(RectangleF Area);

        int GetMapWidth();
        int GetMapHeight();
        byte SetSprite(Vector2D position);

        void Init();
    }
}