namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed partial class RhiWebGpu
{
    private long _rhiHandleCounter;

    private RhiHandle AllocRhiHandle() => new(Interlocked.Increment(ref _rhiHandleCounter));
}
