namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUInstanceDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUInstanceCapabilities features;
}
