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
        /// The last game simulation tick that this chunk was modified.
        /// </summary>
        GameTick LastModifiedTick { get; }

        /// <summary>
        /// The physical collision boxes of this chunk.
        /// </summary>
        IEnumerable<Box2> CollisionBoxes { get; }
    }
}
