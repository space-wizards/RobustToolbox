namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUInstanceCapabilities
{
    public WGPUChainedStructOut* nextInChain;

    [NativeTypeName("WGPUBool")]
    public uint timedWaitAnyEnable;

    [NativeTypeName("size_t")]
    public nuint timedWaitAnyMaxCount;
}
