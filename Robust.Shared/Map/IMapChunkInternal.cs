using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal interface IMapChunkInternal : IMapChunk
    {
        bool SuppressCollisionRegeneration { get; set; }

        void RegenerateCollision();

        /// <summary>
        /// The last game simulation tick that a tile on this chunk was modified.
        /// </summary>
        GameTick LastTileModifiedTick { get; }

        /// <summary>
        /// The last game simulation tick that an anchored entity on this chunk was modified.
        /// </summary>
        GameTick LastAnchoredModifiedTick { get; set; }

        /// <summary>
        /// The physical collision boxes of this chunk.
        /// </summary>
        IEnumerable<Box2> CollisionBoxes { get; }
    }
}
