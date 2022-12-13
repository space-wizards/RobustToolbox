using System;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameStates
{
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
