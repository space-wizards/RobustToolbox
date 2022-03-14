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
        public readonly KeyValuePair<GridId, GridDatum>[]? GridData;

        public GameStateMapData(KeyValuePair<GridId, GridDatum>[]? gridData)
        {
            GridData = gridData;
        }

        [Serializable, NetSerializable]
        public struct GridDatum
        {
            // TransformComponent State
            public readonly MapCoordinates Coordinates;
            public readonly Angle Angle;

            // MapGridComponent State
            public readonly ChunkDatum[] ChunkData;

            public GridDatum(ChunkDatum[] chunkData, MapCoordinates coordinates, Angle angle)
            {
                ChunkData = chunkData;
                Coordinates = coordinates;
                Angle = angle;
            }
        }

        [Serializable, NetSerializable]
        public readonly struct ChunkDatum
        {
            public readonly Vector2i Index;

            // Definitely wasteful to send EVERY tile.
            // Optimize away future coder.
            // Also it's stored row-major.
            public readonly Tile[] TileData;

            public bool IsDeleted()
            {
                return TileData == default;
            }

            private ChunkDatum(Vector2i index, Tile[] tileData)
            {
                Index = index;
                TileData = tileData;
            }

            public static ChunkDatum CreateModified(Vector2i index, Tile[] tileData)
            {
                return new ChunkDatum(index, tileData);
            }

            public static ChunkDatum CreateDeleted(Vector2i index)
            {
                return new ChunkDatum(index, default!);
            }
        }
    }
}
