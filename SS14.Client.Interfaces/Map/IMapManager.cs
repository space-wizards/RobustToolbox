using Lidgren.Network;
using System.Collections.Generic;
using System.Drawing;
using SS14.Shared;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Map
{
    public delegate void TileChangedEventHandler(TileRef tileRef, Tile oldTile);

    public interface IMapManager
    {
        event TileChangedEventHandler TileChanged;

        void HandleNetworkMessage(NetIncomingMessage message);

        int TileSize { get; }

        IEnumerable<TileRef> GetTilesIntersecting(RectangleF area, bool ignoreSpace);
        IEnumerable<TileRef> GetGasTilesIntersecting(RectangleF area);
        IEnumerable<TileRef> GetWallsIntersecting(RectangleF area);
        IEnumerable<TileRef> GetAllTiles();

        TileRef GetTileRef(Vector2 pos);
        TileRef GetTileRef(int x, int y);
        ITileCollection Tiles { get; }
    }
}