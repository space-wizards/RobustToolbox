using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    public delegate void TileChangedEventHandler(TileRef tileRef, Tile oldTile);

    public interface IMapManager
    {
        Dictionary<Vector2i, Chunk> Chunks { get; }

        bool LoadMap(string mapName);
        void SaveMap(string mapName);

        event TileChangedEventHandler TileChanged;

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
