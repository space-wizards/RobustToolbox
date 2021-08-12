using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal interface IMapChunkInternal : IMapChunk
    {
        List<Fixture> Fixtures { get; set; }

        bool SuppressCollisionRegeneration { get; set; }

        void RegenerateCollision();

        /// <summary>
        /// The last game simulation tick that this chunk was modified.
        /// </summary>
        GameTick LastModifiedTick { get; }
    }
}
