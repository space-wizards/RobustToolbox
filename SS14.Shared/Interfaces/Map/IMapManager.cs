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
        uint TileSize { get; }

        event TileChangedEventHandler TileChanged;

        IEnumerable<TileRef> GetTilesIntersecting(FloatRect area, bool ignoreSpace);
        IEnumerable<TileRef> GetGasTilesIntersecting(FloatRect area);
        IEnumerable<TileRef> GetWallsIntersecting(FloatRect area);
        IEnumerable<TileRef> GetAllTiles();

        bool LoadMap(string mapName);
        void SaveMap(string mapName);

        TileRef GetTileRef(Vector2f posWorld);
        TileRef GetTileRef(int x, int y);

        /// <summary>
        /// </summary>
        /// <param name="mapIndex"></param>
        /// <returns></returns>
        uint GetChunkCount(uint mapIndex);

        /// <summary>
        ///     Returns the chunk at the given position on the given map. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="mapIndex"></param>
        /// <param name="posChunk"></param>
        /// <returns></returns>
        IMapChunk GetChunk(uint mapIndex, Vector2i posChunk);

        IMapChunk GetChunk(uint mapIndex, int xChunk, int yChunk);

        Tile GetTile(uint mapIndex, float xWorld, float yWorld);
        void SetTile(uint mapIndex, float xWorld, float yWorld, Tile tile);

        IEnumerable<IMapChunk> GetMapChunks(uint mapIndex);
    }
}
