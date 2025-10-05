using System.Threading;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed partial class RhiWebGpu
{
    private long _rhiHandleCounter;

    private RhiHandle AllocRhiHandle() => new(Interlocked.Increment(ref _rhiHandleCounter));
}
