using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SS14.Shared;
using System.Collections.Generic;

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

        IEnumerable<TileRef> GetTilesIntersecting(FloatRect area, bool ignoreSpace);
        IEnumerable<TileRef> GetGasTilesIntersecting(FloatRect area);
        IEnumerable<TileRef> GetWallsIntersecting(FloatRect area);
        IEnumerable<TileRef> GetAllTiles();

        TileRef GetTileRef(Vector2f pos);
        TileRef GetTileRef(int x, int y);
        ITileCollection Tiles { get; }
    }
}