using System;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameStates
{
    [Serializable, NetSerializable]
    public sealed class GameStateMapData
    {
        // Dict of the new maps
        public readonly MapId[]? CreatedMaps;
        public readonly GridId[]? CreatedGrids;
        public readonly KeyValuePair<GridId, GridDatum>[]? GridData;

        public GameStateMapData(KeyValuePair<GridId, GridDatum>[]? gridData, MapId[]? createdMaps, GridId[]? createdGrids)
        {
            GridData = gridData;
            CreatedMaps = createdMaps;
            CreatedGrids = createdGrids;
        }

        [Serializable, NetSerializable]
        public struct GridDatum
        {
            public readonly MapCoordinates Coordinates;
            public readonly Angle Angle;
            public readonly ChunkDatum[] ChunkData;
            public readonly DeletedChunkDatum[] DeletedChunkData;

            public GridDatum(ChunkDatum[] chunkData, DeletedChunkDatum[] deletedChunkData, MapCoordinates coordinates, Angle angle)
            {
                ChunkData = chunkData;
                DeletedChunkData = deletedChunkData;
                Coordinates = coordinates;
                Angle = angle;
            }
        }

        [Serializable, NetSerializable]
        public struct ChunkDatum
        {
            public readonly Vector2i Index;

            // Definitely wasteful to send EVERY tile.
            // Optimize away future coder.
            // Also it's stored row-major.
            public readonly Tile[] TileData;

            public ChunkDatum(Vector2i index, Tile[] tileData)
            {
                Index = index;
                TileData = tileData;
            }
        }

        [Serializable, NetSerializable]
        public struct DeletedChunkDatum
        {
            public readonly Vector2i Index;

            public DeletedChunkDatum(Vector2i index)
            {
                Index = index;
            }
        }
    }
}
