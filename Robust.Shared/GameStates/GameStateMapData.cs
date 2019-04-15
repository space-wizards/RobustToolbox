using System;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class GameStateMapData
    {
        // Dict of the new maps along with which grids are their defaults.
        public readonly Dictionary<MapId, GridId> CreatedMaps;
        public readonly Dictionary<GridId, GridCreationDatum> CreatedGrids;
        public readonly Dictionary<GridId, GridDatum> GridData;
        public readonly List<GridId> DeletedGrids;
        public readonly List<MapId> DeletedMaps;

        public GameStateMapData(Dictionary<GridId, GridDatum> gridData, List<GridId> deletedGrids, List<MapId> deletedMaps, Dictionary<MapId, GridId> createdMaps, Dictionary<GridId, GridCreationDatum> createdGrids)
        {
            GridData = gridData;
            DeletedGrids = deletedGrids;
            DeletedMaps = deletedMaps;
            CreatedMaps = createdMaps;
            CreatedGrids = createdGrids;
        }

        [Serializable, NetSerializable]
        public struct GridCreationDatum
        {
            public readonly ushort ChunkSize;
            public readonly float SnapSize;
            public readonly bool IsTheDefault;

            public GridCreationDatum(ushort chunkSize, float snapSize, bool isTheDefault)
            {
                ChunkSize = chunkSize;
                SnapSize = snapSize;
                IsTheDefault = isTheDefault;
            }
        }

        [Serializable, NetSerializable]
        public struct GridDatum
        {
            public readonly MapCoordinates Coordinates;
            public readonly List<ChunkDatum> ChunkData;

            public GridDatum(List<ChunkDatum> chunkData, MapCoordinates coordinates)
            {
                ChunkData = chunkData;
                Coordinates = coordinates;
            }
        }

        [Serializable, NetSerializable]
        public struct ChunkDatum
        {
            public readonly MapIndices Index;

            // Definitely wasteful to send EVERY tile.
            // Optimize away future coder.
            // Also it's stored row-major.
            public readonly Tile[] TileData;

            public ChunkDatum(MapIndices index, Tile[] tileData)
            {
                Index = index;
                TileData = tileData;
            }
        }
    }
}
