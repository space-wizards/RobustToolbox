namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPURenderPassMaxDrawCount
{
    public WGPUChainedStruct chain;

    [NativeTypeName("uint64_t")]
    public ulong maxDrawCount;
}
