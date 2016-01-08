using BKSystem.IO;
using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.State;
using SS14.Client.Services.State.States;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SS14.Client.Services.Map
{
    public class MapManager : IMapManager
    {
        private Dictionary<Vector2i, Chunk> chunks = new Dictionary<Vector2i, Chunk>();
        private static readonly int ChunkSize = Chunk.ChunkSize;

        public event TileChangedEventHandler TileChanged;

        public MapManager()
        {
            tileIndexer = new TileCollection(this);
        }

        public int TileSize
        {
            get { return 32; }
        }

        #region Tile Enumerators

        // If `ignoreSpace` is false, this will return tiles in chunks that don't even exist.
        // This is to make the tile count predictable.  Is this appropriate behavior?
        public IEnumerable<TileRef> GetTilesIntersecting(FloatRect area, bool ignoreSpace)
        {
            int chunkLeft = (int)Math.Floor(area.Left / ChunkSize);
            int chunkTop = (int)Math.Floor(area.Top / ChunkSize);
            int chunkRight = (int)Math.Floor(area.Right() / ChunkSize);
            int chunkBottom = (int)Math.Floor(area.Bottom() / ChunkSize);
            for (int chunkY = chunkTop; chunkY <= chunkBottom; ++chunkY)
            {
                for (int chunkX = chunkLeft; chunkX <= chunkRight; ++chunkX)
                {
                    int xMin = 0;
                    int yMin = 0;
                    int xMax = 15;
                    int yMax = 15;

                    if (chunkX == chunkLeft)
                        xMin = Mod(Math.Floor(area.Left), ChunkSize);
                    if (chunkY == chunkTop)
                        yMin = Mod(Math.Floor(area.Top), ChunkSize);

                    if (chunkX == chunkRight)
                        xMax = Mod(Math.Floor(area.Right()), ChunkSize);
                    if (chunkY == chunkBottom)
                        yMax = Mod(Math.Floor(area.Bottom()), ChunkSize);

                    Chunk chunk;
                    if (!chunks.TryGetValue(new Vector2i(chunkX, chunkY), out chunk))
                    {
                        if (ignoreSpace)
                            continue;
                        else
                            for (int y = yMin; y <= yMax; ++y)
                                for (int x = xMin; x <= xMax; ++x)
                                    yield return new TileRef(this,
                                        chunkX * ChunkSize + x,
                                        chunkY * ChunkSize + y);
                    }
                    else
                    {
                        for (int y = yMin; y <= yMax; ++y)
                        {
                            int i = y * ChunkSize + xMin;
                            for (int x = xMin; x <= xMax; ++x, ++i)
                            {
                                if (!ignoreSpace || chunk.Tiles[i].TileId != 0)
                                    yield return new TileRef(this,
                                        chunkX * ChunkSize + x,
                                        chunkY * ChunkSize + y,
                                        chunk, i);
                            }
                        }
                    }
                }
            }
        }
        public IEnumerable<TileRef> GetGasTilesIntersecting(FloatRect area)
        {
            return GetTilesIntersecting(area, true).Where(t => t.Tile.TileDef.IsGasVolume);
        }
        public IEnumerable<TileRef> GetWallsIntersecting(FloatRect area)
        {
            return GetTilesIntersecting(area, true).Where(t => t.Tile.TileDef.IsWall);
        }
        // Unlike GetAllTilesIn(...), this skips non-existant chunks.
        // It also does not return chunks in order.
        public IEnumerable<TileRef> GetAllTiles()
        {
            foreach (var pair in chunks)
            {
                int i = 0;
                for (int y = 0; y < ChunkSize; ++y)
                {
                    for (int x = 0; x < ChunkSize; ++x, ++i)
                    {
                        if (pair.Value.Tiles[i].TileId != 0)
                            yield return new TileRef(this,
                                pair.Key.X * ChunkSize + x,
                                pair.Key.Y * ChunkSize + y,
                                pair.Value, i);
                    }
                }
            }
        }

        #endregion

        #region Indexers

        public TileRef GetTileRef(Vector2f pos)
        {
            return GetTileRef((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y));
        }
        public TileRef GetTileRef(int x, int y)
        {
            Vector2i chunkPos = new Vector2i(
                (int)Math.Floor((float)x / ChunkSize),
                (int)Math.Floor((float)y / ChunkSize)
            );
            Chunk chunk;
            if (chunks.TryGetValue(chunkPos, out chunk))
                return new TileRef(this, x, y, chunk,
                    (y - chunkPos.Y * ChunkSize) * ChunkSize + (x - chunkPos.X * ChunkSize));
            else
                return new TileRef(this, x, y);
        }

        private TileCollection tileIndexer;
        public ITileCollection Tiles { get { return tileIndexer; } }

        public sealed class TileCollection : ITileCollection
        {
            private readonly MapManager mm;

            internal TileCollection(MapManager mm)
            {
                this.mm = mm;
            }

            public Tile this[Vector2f pos]
            {
                get
                {
                    return this[(int)Math.Floor(pos.X), (int)Math.Floor(pos.Y)];
                }
                set
                {
                    this[(int)Math.Floor(pos.X), (int)Math.Floor(pos.Y)] = value;
                }
            }
            public Tile this[int x, int y]
            {
                get
                {
                    Vector2i chunkPos = new Vector2i(
                        (int)Math.Floor((float)x / ChunkSize),
                        (int)Math.Floor((float)y / ChunkSize)
                    );
                    Chunk chunk;
                    if (mm.chunks.TryGetValue(chunkPos, out chunk))
                        return chunk.Tiles[(y - chunkPos.Y * ChunkSize) * ChunkSize + (x - chunkPos.X * ChunkSize)];
                    else
                        return default(Tile); // SPAAAAAAAAAAAAAACE!!!
                }
                set
                {
                    Vector2i chunkPos = new Vector2i(
                        (int)Math.Floor((float)x / ChunkSize),
                        (int)Math.Floor((float)y / ChunkSize)
                    );
                    Chunk chunk;
                    if (!mm.chunks.TryGetValue(chunkPos, out chunk))
                    {
                        if (value.IsSpace)
                            return;
                        else
                            mm.chunks[chunkPos] = chunk = new Chunk();
                    }

                    int index = (y - chunkPos.Y * ChunkSize) * ChunkSize + (x - chunkPos.X * ChunkSize);
                    Tile oldTile = chunk.Tiles[index];
                    if (oldTile == value)
                        return;

                    chunk.Tiles[index] = value;

                    if (mm.TileChanged != null)
                        mm.TileChanged(new TileRef(mm, x, y, chunk, index), oldTile);
                }
            }
        }

        #endregion

        #region Networking

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (MapMessage) message.ReadByte();
            switch (messageType)
            {
                case MapMessage.TurfUpdate:
                    HandleTurfUpdate(message);
                    break;
                case MapMessage.SendTileMap:
                    LoadTileMap(message);
                    break;
            }
        }

        public bool LoadTileMap(NetIncomingMessage message)
        {
            int version = message.ReadInt32();
            if (version != 1)
                return false; // Unsupported version.

            var tileDefMgr = IoCManager.Resolve<ITileDefinitionManager>();
            tileDefMgr.RegisterServerTileMapping(message);

            int chunkCount = message.ReadInt32();
            for (int i = 0; i < chunkCount; ++i)
            {
                int x = message.ReadInt32();
                int y = message.ReadInt32();
                var chunkPos = new Vector2i(x, y);

                Chunk chunk;
                if (!chunks.TryGetValue(chunkPos, out chunk))
                    chunks[chunkPos] = chunk = new Chunk();

                chunk.ReceiveChunkData(message);
            }

            return true;
        }

        private void HandleTurfUpdate(NetIncomingMessage message)
        {
            int x = message.ReadInt32();
            int y = message.ReadInt32();
            Tile tile = (Tile)message.ReadUInt32();

            Tiles[x, y] = tile;
        }

        #endregion


        //public Tile GenerateNewTile(string typeName, TileState state, Vector2D pos, Direction dir = Direction.North)
        //{
        //    Type tileType = Type.GetType("SS14.Client.Services.Tiles." + typeName, false);

        //    if (tileType == null) throw new ArgumentException("Invalid Tile Type specified : '" + typeName + "' .");
        //    RectangleF rect = new FloatRect();
        //    Tile created;
        //    if (typeName != "Wall")
        //    {
        //        rect = new FloatRect(pos.X, pos.Y, TileSpacing, TileSpacing);
        //    }
        //    else
        //    {
        //        if (dir == Direction.North)
        //        {
        //            rect = new FloatRect(pos.X, pos.Y, wallThickness, TileSpacing);
        //        }
        //        else
        //        {
        //            rect = new FloatRect(pos.X, pos.Y, TileSpacing, wallThickness);
        //        }
        //    }

        //    if (typeName == "Wall")
        //    {
        //        created = (Tile)Activator.CreateInstance(tileType, state, rect, dir);
        //    }
        //    else
        //    {
        //        created = (Tile)Activator.CreateInstance(tileType, state, rect);
        //    }

        //    created.Initialize();

        //    if (tileType.GetInterface("ICollidable") != null)
        //        _collisionManager.AddCollidable((ICollidable) created);

        //    return created;
        //}
        
        // An actual modulus implementation, because apparently % is not modulus.  Srsly
        // Should probably stick this in some static class.
        [System.Diagnostics.DebuggerStepThrough]
        private static int Mod(double n, int d)
        {
            return (int)(n - (int)Math.Floor(n / d) * d);
        }

    }
}