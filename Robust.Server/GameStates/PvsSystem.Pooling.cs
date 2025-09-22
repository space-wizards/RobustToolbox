using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using SharpZstd.Interop;

namespace Robust.Server.GameStates;

// This partial class contains code for pooling objects to avoid allocations.
// This file is now blessedly small, so maybe we can just get rid of it.
internal sealed partial class PvsSystem
{
    /// <summary>
    /// Maximum number of pooled objects.
    /// </summary>
    private const int MaxVisPoolSize = 1024;

    private readonly ObjectPool<List<PvsIndex>> _entDataListPool
        = new DefaultObjectPool<List<PvsIndex>>(new ListPolicy<PvsIndex>(), MaxVisPoolSize);

    private readonly ObjectPool<HashSet<EntityUid>> _uidSetPool
        = new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>(), MaxVisPoolSize);

    private readonly ObjectPool<PvsChunk> _chunkPool =
        new DefaultObjectPool<PvsChunk>(new PvsChunkPolicy(), 256);

    private sealed class PvsChunkPolicy : PooledObjectPolicy<PvsChunk>
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

    private sealed class PvsThreadResourcesObjectPolicy(int ce) : IPooledObjectPolicy<PvsThreadResources>
    {
        PvsThreadResources IPooledObjectPolicy<PvsThreadResources>.Create()
        {
            var res = new PvsThreadResources();
            res.CompressionContext.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, ce);
            return res;
        }

        bool IPooledObjectPolicy<PvsThreadResources>.Return(PvsThreadResources _)
        {
            return true;
        }
    }

    private sealed class PvsThreadResources
    {
        public ZStdCompressionContext CompressionContext = new();

        ~PvsThreadResources()
        {
            CompressionContext.Dispose();
        }
    }
}
