using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.Maths;
using System.Collections.Generic;
using System.Drawing;

namespace SS14.Server.Interfaces.Map
{
    public delegate void TileChangedEventHandler(TileRef tileRef, Tile oldTile);

    public interface IMapManager
    {
        bool LoadMap(string mapName);
        void SaveMap(string mapName);

        event TileChangedEventHandler TileChanged;

        void HandleNetworkMessage(NetIncomingMessage message);
        NetOutgoingMessage CreateMapMessage(MapMessage messageType);
        void SendMap(NetConnection connection);

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