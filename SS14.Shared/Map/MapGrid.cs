using System;
using System.Collections.Generic;
using OpenTK;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Maths;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Shared.Map
{
    public class MapGrid : IMapGrid
    {
        public int MapID { get; private set; } = 0;
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

            public override string ToString()
            {
                return $"{{{X},{Y}}}";
            }
        }

        internal MapGrid(MapManager mapManager, int gridIndex, ushort chunkSize, float snapsize, int mapID)
        {
            _mapManager = mapManager;
            Index = gridIndex;
            ChunkSize = chunkSize;
            SnapSize = snapsize;
            MapID = mapID;
        }

        /// <summary>
        /// Disposes the grid.
        /// </summary>
        public void Dispose()
        {
            // Nothing for now.
        }

        /// <inheritdoc />
        public Box2 AABBWorld { get; private set; }

        /// <inheritdoc />
        public ushort ChunkSize { get; }

        /// <inheritdoc />
        public float SnapSize { get; }

        public int Index { get; }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        public ushort TileSize { get; set; } = 1;

        /// <inheritdoc />
        public Vector2 WorldPosition { get; set; }

        /// <summary>
        /// Expands the AABB for this grid when a new tile is added. If the tile is already inside the existing AABB,
        /// nothing happens. If it is outside, the AABB is expanded to fit the new tile.
        /// </summary>
        /// <param name="gridTile">The new tile to check.</param>
        public void UpdateAABB(Indices gridTile)
        {
            var worldPos = GridTileToLocal(gridTile).ToWorld();

            if (AABBWorld.Contains(worldPos.Position))
                return;

            // rect union
            var a = AABBWorld;
            var b = worldPos;

            var x = Math.Min(a.Left, b.X);
            var width = Math.Max(a.Left + a.Width, b.X);

            var y = Math.Min(a.Top, b.Y);
            var height = Math.Max(a.Top + a.Height, b.Y);

            AABBWorld = new Box2(x, y, width - x, height - y);
        }

        public bool OnSnapCenter(Vector2 position)
        {
            return (FloatMath.CloseTo(position.X % SnapSize, 0) && FloatMath.CloseTo(position.Y % SnapSize, 0));
        }

        public bool OnSnapBorder(Vector2 position)
        {
            return (FloatMath.CloseTo(position.X % SnapSize, SnapSize / 2) && FloatMath.CloseTo(position.Y % SnapSize, SnapSize / 2));
        }

        #region TileAccess

        /// <inheritdoc />
        public TileRef GetTile(LocalCoordinates worldPos)
        {
            var chunkIndices = WorldToChunk(worldPos);
            var gridTileIndices = WorldToTile(worldPos);

            Chunk output;
            if (_chunks.TryGetValue(chunkIndices, out output))
            {
                var chunkTileIndices = output.GridTileToChunkTile(gridTileIndices);
                return output.GetTile((ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
            }
            return new TileRef(MapID, Index, gridTileIndices.X, gridTileIndices.Y, default(Tile));
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true)
        {
            foreach (var kvChunk in _chunks)
            {
                foreach (var tileRef in kvChunk.Value)
                {
                    if (!tileRef.Tile.IsEmpty)
                        yield return tileRef;
                }
            }
        }

        /// <inheritdoc />
        public void SetTile(LocalCoordinates worldPos, Tile tile)
        {
            var localTile = WorldToTile(worldPos);
            SetTile(localTile.X, localTile.Y, tile);
        }

        /// <inheritdoc />
        public void SetTile(int xIndex, int yIndex, Tile tile)
        {
            var gridTileIndices = new Indices(xIndex, yIndex);
            var gridChunkIndices = GridTileToGridChunk(gridTileIndices);
            var chunk = GetChunk(gridChunkIndices);
            var chunkTileIndices = chunk.GridTileToChunkTile(gridTileIndices);
            chunk.SetTile((ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y, tile);
        }

        /// <inheritdoc />
        public void SetTile(LocalCoordinates worldPos, ushort tileId, ushort tileData = 0)
        {
            SetTile(worldPos, new Tile(tileId, tileData));
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef> predicate = null)
        {
            //TODO: needs world -> local -> tile translations.
            var gridTileLt = new Indices((int)Math.Floor(worldArea.Left), (int)Math.Floor(worldArea.Top));
            var gridTileRb = new Indices((int)Math.Floor(worldArea.Right), (int)Math.Floor(worldArea.Bottom));

            var tiles = new List<TileRef>();

            for (var x = gridTileLt.X; x <= gridTileRb.X; x++)
            {
                for (var y = gridTileLt.Y; y <= gridTileRb.Y; y++)
                {
                    var gridChunk = GridTileToGridChunk(new Indices(x, y));
                    Chunk chunk;
                    if (_chunks.TryGetValue(gridChunk, out chunk))
                    {
                        var chunkTile = chunk.GridTileToChunkTile(new Indices(x, y));
                        var tile = chunk.GetTile((ushort)chunkTile.X, (ushort)chunkTile.Y);

                        if (ignoreEmpty && tile.Tile.IsEmpty)
                            continue;

                        if (predicate == null || predicate(tile))
                        {
                            tiles.Add(tile);
                        }
                    }
                    else if (!ignoreEmpty)
                    {
                        var tile = new TileRef(MapID, Index, x, y, new Tile());

                        if (predicate == null || predicate(tile))
                        {
                            tiles.Add(tile);
                        }
                    }
                }
            }
            return tiles;
        }

        #endregion TileAccess

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

        #endregion ChunkAccess

        #region Transforms

        /// <inheritdoc />
        public Vector2 WorldToLocal(Vector2 posWorld)
        {
            return posWorld - WorldPosition;
        }

        /// <inheritdoc />
        public LocalCoordinates LocalToWorld(LocalCoordinates local)
        {
            return new LocalCoordinates(local.Position + WorldPosition, 0, local.MapID);
        }

        public Vector2 ConvertToWorld(Vector2 localpos)
        {
            return localpos + WorldPosition;
        }

        /// <summary>
        /// Transforms global world coordinates to tile indices relative to grid origin.
        /// </summary>
        /// <returns></returns>
        public Indices WorldToTile(LocalCoordinates posWorld)
        {
            var local = posWorld.ConvertToGrid(this);
            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Indices(x, y);
        }

        /// <summary>
        /// Transforms global world coordinates to chunk indices relative to grid origin.
        /// </summary>
        /// <param name="localPos">The position in the world.</param>
        /// <returns></returns>
        public Indices WorldToChunk(LocalCoordinates posWorld)
        {
            var local = posWorld.ConvertToGrid(this);
            var x = (int)Math.Floor(local.X / (TileSize * ChunkSize));
            var y = (int)Math.Floor(local.Y / (TileSize * ChunkSize));
            return new Indices(x, y);
        }

        /// <summary>
        /// Transforms grid tile indices to grid chunk indices.
        /// </summary>
        /// <param name="gridTile"></param>
        /// <returns></returns>
        public Indices GridTileToGridChunk(Indices gridTile)
        {
            var x = (int)Math.Floor(gridTile.X / (float)ChunkSize);
            var y = (int)Math.Floor(gridTile.Y / (float)ChunkSize);

            return new Indices(x, y);
        }

        /// <inheritdoc />
        public LocalCoordinates GridTileToLocal(Indices gridTile)
        {
            return new LocalCoordinates(gridTile.X * TileSize + (TileSize / 2), gridTile.Y * TileSize + (TileSize / 2), this);
        }

        /// <inheritdoc />
        public bool IndicesToTile(MapGrid.Indices indices, out TileRef tile)
        {
            MapGrid.Indices chunkindices = new MapGrid.Indices(indices.X / ChunkSize, indices.Y / ChunkSize);
            if (!_chunks.ContainsKey(chunkindices))
            {
                tile = new TileRef(); //Nothing should ever use or access this, bool check should occur first
                return false;
            }
            Chunk chunk = _chunks[chunkindices];
            tile = chunk.GetTile(new MapGrid.Indices(indices.X % ChunkSize, indices.Y % ChunkSize));
            return true;
        }

        #endregion Transforms
    }
}
