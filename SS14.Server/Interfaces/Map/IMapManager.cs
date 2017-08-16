using SFML.Graphics;
using SFML.System;
using SS14.Shared;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Server.Interfaces.Map
{
    public delegate void TileChangedEventHandler(TileRef tileRef, Tile oldTile);

    public interface IMapManager
    {
        void Initialize();
        bool LoadMap(string mapName);
        void SaveMap(string mapName);

        event TileChangedEventHandler TileChanged;

        void HandleNetworkMessage(MsgMap message);
        void SendMap(INetChannel connection);

        int TileSize { get; }

        IEnumerable<TileRef> GetTilesIntersecting(FloatRect area, bool ignoreSpace);
        IEnumerable<TileRef> GetGasTilesIntersecting(FloatRect area);
        IEnumerable<TileRef> GetWallsIntersecting(FloatRect area);
        IEnumerable<TileRef> GetAllTiles();

        TileRef GetTileRef(Vector2 pos);
        TileRef GetTileRef(int x, int y);
        ITileCollection Tiles { get; }
    }
}
