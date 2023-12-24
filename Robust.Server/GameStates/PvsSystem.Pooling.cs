using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// This partial class contains code for pooling objects to avoid allocations.
// This file is now blessedly small, so maybe we can just get rid of it.
internal sealed partial class PvsSystem
{
    /// <summary>
    /// Maximum number of pooled objects.
    /// </summary>
    private const int MaxVisPoolSize = 1024;

    private readonly ObjectPool<List<EntityData>> _entDataListPool
        = new DefaultObjectPool<List<EntityData>>(new ListPolicy<EntityData>(), MaxVisPoolSize);

    private readonly ObjectPool<HashSet<EntityUid>> _uidSetPool
        = new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>(), MaxVisPoolSize);

    private readonly ObjectPool<PvsChunk> _chunkPool =
        new DefaultObjectPool<PvsChunk>(new PvsChunkPolicy(), 256);

    public sealed class PvsChunkPolicy : PooledObjectPolicy<PvsChunk>
    {
        public override PvsChunk Create()
        {
            return new PvsChunk();
        }

        public override bool Return(PvsChunk obj)
        {
            obj.Wipe();
            return true;
        }
    }
}
