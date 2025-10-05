namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUInstanceEnumerateAdapterOptions
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("WGPUInstanceBackend")]
    public ulong backends;
}
