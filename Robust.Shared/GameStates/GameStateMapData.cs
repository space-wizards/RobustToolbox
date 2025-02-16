using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameStates
{
    [Serializable, NetSerializable]
    public readonly struct ChunkDatum
    {
        public static readonly ChunkDatum Empty = new();

        public readonly HashSet<string>? Fixtures;

        // Definitely wasteful to send EVERY tile.
        // Optimize away future coder.
        // Also it's stored row-major.
        public readonly Tile[]? TileData;

        public readonly Box2i? CachedBounds;

        [MemberNotNullWhen(false, nameof(TileData), nameof(Fixtures))]
        public bool IsDeleted()
        {
            return TileData == null;
        }

        private ChunkDatum(Tile[] tileData, HashSet<string> fixtures, Box2i cachedBounds)
        {
            TileData = tileData;
            Fixtures = fixtures;
            CachedBounds = cachedBounds;
        }

        public static ChunkDatum CreateModified(Tile[] tileData, HashSet<string> fixtures, Box2i cachedBounds)
        {
            return new ChunkDatum(tileData, fixtures, cachedBounds);
        }
    }
}
