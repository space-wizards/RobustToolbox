using System.Threading;
using Robust.Shared.Threading;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem
{
    public WaitHandle ProcessDynamicChunksets()
    {
        // TODO: Get viewers
        // TODO: Get AABBs
        // TODO: Get chunks in range
        // TODO: Use vismask and get chunksets
        var job = new DynamicChunksetJob();
        var handle = _parallelManager.Process(job, 1);
        return handle;
    }

    private record struct DynamicChunksetJob : IParallelRobustJob
    {
        public int BatchSize => 1;

        public void Execute(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}
