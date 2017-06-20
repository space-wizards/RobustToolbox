using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Map
{
    public class MapGrid : IMapGrid
    {
        private readonly MapManager _mapManager;
        private readonly Dictionary<Indices, Chunk> _chunks = new Dictionary<Indices, Chunk>();

        /// <summary>
        /// Internal structure to store 2 indices of a chunk or tile.
        /// </summary>
        public struct Indices
        {
            /// <summary>
            /// The X index.
            /// </summary>
            public readonly int X;

            /// <summary>
            /// The Y index.
            /// </summary>
            public readonly int Y;

            /// <summary>
            /// Public constructor.
            /// </summary>
            /// <param name="x">The X index.</param>
            /// <param name="y">The Y index.</param>
            public Indices(int x, int y)
            {
                X = x;
                Y = y;
            }

            //TODO: Fill out the rest of these.
            public static Indices operator +(Indices left, Indices right)
            {
                return new Indices(left.X + right.X, left.Y + right.Y);
            }

            public static Indices operator *(Indices indices, int multiplier)
            {
                return new Indices(indices.X * multiplier, indices.Y * multiplier);
            }
        }

        internal MapGrid(MapManager mapManager, int gridIndex, ushort chunkSize)
        {
            _mapManager = mapManager;
            Index = gridIndex;
            ChunkSize = chunkSize;
        }

        /// <summary>
        /// Disposes the grid.
        /// </summary>
        public void Dispose() { }

        /// <inheritdoc />
        public FloatRect AABBWorld { get; private set; }

        /// <inheritdoc />
        public ushort ChunkSize { get; }

        public int Index { get; }

        /// <inheritdoc />
        public Vector2f WorldPosition { get; set; }

        public void UpdateAABB(Indices gridTile)
        {
            var localPos = TileToLocal(gridTile);
            var worldPos = LocalToWorld(localPos);

            if (AABBWorld.Contains(worldPos.X, worldPos.Y))
                return;

            // rect union
            var a = AABBWorld;
            var b = worldPos;

            var x = Math.Min(a.Left, b.X);
            var width = Math.Max(a.Left + a.Width, b.X);

            var y = Math.Min(a.Top, b.Y);
            var height = Math.Max(a.Top + a.Height, b.Y);
            
            AABBWorld = new FloatRect(x, y, width - x, height - y);
        }

        #region  TileAccess

        /// <inheritdoc />
        public TileRef GetTile(Vector2f posWorld)
        {
            var chunkIndices = WorldToChunk(posWorld);
            var gridTileIndices = WorldToTile(posWorld);

            Chunk output;
            if (_chunks.TryGetValue(chunkIndices, out output))
            {
                var chunkTileIndices = output.GridTileToChunkTile(gridTileIndices);
                return output.GetTile((ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
            }
            return new TileRef(_mapManager, Index, gridTileIndices.X, gridTileIndices.Y, default(Tile));
        }

        /// <inheritdoc />
        public TileRef GetTile(float xWorld, float yWorld)
        {
            return GetTile(new Vector2f(xWorld, yWorld));
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true)
        {
            foreach (var kvChunk in _chunks)
            {
                foreach (var tileRef in kvChunk.Value)
                {
                    if(!tileRef.Tile.IsEmpty)
                        yield return tileRef;
                }
            }
        }

        /// <inheritdoc />
        public void SetTile(float xWorld, float yWorld, Tile tile)
        {
            SetTile(new Vector2f(xWorld, yWorld), tile);
        }

        /// <inheritdoc />
        public void SetTile(Vector2f posWorld, Tile tile)
        {
            var localTile = WorldToTile(posWorld);
            SetTile(localTile.X, localTile.Y, tile);
        }

        /// <inheritdoc />
        public void SetTile(int xIndex, int yIndex, Tile tile)
        {
            var gridTileIndices = new Indices(xIndex, yIndex);
            var gridChunkIndices = TileToGrid(gridTileIndices);
            var chunk = GetChunk(gridChunkIndices);
            var chunkTileIndices = chunk.GridTileToChunkTile(gridTileIndices);
            chunk.SetTile((ushort) chunkTileIndices.X, (ushort) chunkTileIndices.Y, tile);
        }

        /// <inheritdoc />
        public void SetTile(float xWorld, float yWorld, ushort tileId, ushort tileData = 0)
        {
            SetTile(new Vector2f(xWorld, yWorld), new Tile(tileId, tileData));
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetTilesIntersecting(FloatRect areaWorld, bool ignoreEmpty = true, Predicate<TileRef> predicate = null)
        {
            /* Apparently CluwneLib already does world2tile transform
            var gridTileLt = WorldToTile(new Vector2f(areaWorld.Left, areaWorld.Top));
            var gridTileRb = WorldToTile(new Vector2f(areaWorld.Right(), areaWorld.Bottom()));
            */

            var gridTileLt = new Indices((int)areaWorld.Left, (int)areaWorld.Top);
            var gridTileRb = new Indices((int)areaWorld.Right(), (int)areaWorld.Bottom());

            var tiles = new List<TileRef>();

            for (var x = gridTileLt.X; x <= gridTileRb.X; x++)
            {
                for (var y = gridTileLt.Y; y <= gridTileRb.Y; y++)
                {
                    var gridChunk = TileToGrid(new Indices(x, y));
                    Chunk chunk;
                    if (_chunks.TryGetValue(gridChunk, out chunk))
                    {
                        var chunkTile = chunk.GridTileToChunkTile(new Indices(x, y));
                        var tile = chunk.GetTile((ushort) chunkTile.X, (ushort) chunkTile.Y);

                        if(ignoreEmpty && tile.Tile.IsEmpty)
                            continue;

                        if (predicate == null || predicate(tile))
                        {
                            tiles.Add(tile);
                        }
                    }
                    else if(!ignoreEmpty)
                    {
                        var tile = new TileRef(_mapManager, Index, x, y, new Tile());

                        if (predicate == null || predicate(tile))
                        {
                            tiles.Add(tile);
                        }
                    }

                } 
            }

            return tiles;

#if _OLD 
            // translate 2 points from global world space to grid chunk indexes
            int chunkLeft = (int)Math.Truncate(areaWorld.Left / (ChunkSize * _mapManager.TileSize));
            int chunkTop = (int)Math.Truncate(areaWorld.Top / (ChunkSize * _mapManager.TileSize));
            int chunkRight = (int)Math.Truncate(areaWorld.Right() / (ChunkSize * _mapManager.TileSize));
            int chunkBottom = (int)Math.Truncate(areaWorld.Bottom() / (ChunkSize * _mapManager.TileSize));

            // iterate over the rectangle
            for (int chunkY = chunkTop; chunkY <= chunkBottom; ++chunkY)
            {
                for (int chunkX = chunkLeft; chunkX <= chunkRight; ++chunkX)
                {
                    int xMinTile = 0;
                    int yMinTile = 0;
                    int xMaxTile = 15;
                    int yMaxTile = 15;

                    if (chunkX == chunkLeft)
                        xMinTile = (int) (Math.Truncate(areaWorld.Left / _mapManager.TileSize) % ChunkSize);
                    if (chunkY == chunkTop)
                        yMinTile = (int)(Math.Truncate(areaWorld.Top / _mapManager.TileSize) % ChunkSize);

                    if (chunkX == chunkRight)
                        xMaxTile = (int)(Math.Truncate(areaWorld.Right() / _mapManager.TileSize) % ChunkSize);
                    if (chunkY == chunkBottom)
                        yMaxTile = (int)(Math.Truncate(areaWorld.Bottom() / _mapManager.TileSize) % ChunkSize);

                    Chunk chunk;
                    if (!_chunks.TryGetValue(new Indices(chunkX, chunkY), out chunk))
                    {
                        if (ignoreEmpty)
                            continue;

                        for (var y = yMinTile; y <= yMaxTile; ++y)
                        {
                            for (var x = xMinTile; x <= xMaxTile; ++x)
                            {
                                var tileRef = new TileRef(_mapManager, Index, chunkX * ChunkSize + x, chunkY * ChunkSize + y, new Tile());
                                if (predicate != null && predicate(tileRef))
                                    yield return tileRef;
                            }
                        }
                    }
                    else
                    {
                        for (int y = yMinTile; y <= yMaxTile; ++y)
                        {
                            int i = y * ChunkSize + xMinTile;
                            for (int x = xMinTile; x <= xMaxTile; ++x, ++i)
                            {
                                var tileRef = chunk.GetTile((ushort) x, (ushort) i);
                                if (!ignoreEmpty || tileRef.Tile.TileId != 0)
                                {
                                        if (predicate != null && predicate(tileRef))
                                            yield return tileRef;
                                }
                            }
                        }
                    }
                }
            }
#endif
        }

#endregion

#region ChunkAccess
        
        /// <summary>
        /// The total number of allocated chunks in the grid.
        /// </summary>
        public int ChunkCount => _chunks.Count;

        /// <inheritdoc />
        public IMapChunk GetChunk(int xIndex, int yIndex)
        {
            return GetChunk(new Indices(xIndex, yIndex));
        }

        /// <inheritdoc />
        public IMapChunk GetChunk(Indices chunkIndices)
        {
            Chunk output;
            if (_chunks.TryGetValue(chunkIndices, out output))
                return output;

            return _chunks[chunkIndices] = new Chunk(_mapManager, this, chunkIndices.X, chunkIndices.Y, ChunkSize);
        }

        /// <inheritdoc />
        public IEnumerable<IMapChunk> GetMapChunks()
        {
            foreach (var kvChunk in _chunks)
            {
                yield return kvChunk.Value;
            }
        }

#endregion

#region Transforms

        /// <inheritdoc />
        public Vector2f WorldToLocal(Vector2f posWorld)
        {
            return posWorld - WorldPosition;
        }

        /// <inheritdoc />
        public Vector2f LocalToWorld(Vector2f posLocal)
        {
            return posLocal + WorldPosition;
        }

        /// <summary>
        /// Transforms global world coordinates to tile indices relative to grid origin.
        /// </summary>
        /// <returns></returns>
        public Indices WorldToTile(Vector2f posWorld)
        {
            var local = WorldToLocal(posWorld);
            var x = (int)Math.Truncate(local.X / _mapManager.TileSize);
            var y = (int)Math.Truncate(local.Y / _mapManager.TileSize);
            return new Indices(x, y);
        }

        /// <summary>
        /// Transforms global world coordinates to chunk indices relative to grid origin.
        /// </summary>
        /// <param name="posWorld">The position in the world.</param>
        /// <returns></returns>
        public Indices WorldToChunk(Vector2f posWorld)
        {
            var local = posWorld - WorldPosition;
            var x = (int)Math.Truncate(local.X / (_mapManager.TileSize * ChunkSize));
            var y = (int)Math.Truncate(local.X / (_mapManager.TileSize * ChunkSize));
            return new Indices(x,y);
        }

        /// <summary>
        /// Transforms grid tile indices to grid chunk indices.
        /// </summary>
        /// <param name="tileGrid"></param>
        /// <returns></returns>
        public Indices TileToGrid(Indices tileGrid)
        {
            var x = tileGrid.X / ChunkSize;
            var y = tileGrid.Y / ChunkSize;

            return new Indices(x, y);
        }

        public Vector2f TileToLocal(Indices gridTile)
        {
            var tileSize = _mapManager.TileSize;
            return new Vector2f(gridTile.X * tileSize + (tileSize/2), gridTile.Y * tileSize + (tileSize / 2));
        }
#endregion
    }
}
