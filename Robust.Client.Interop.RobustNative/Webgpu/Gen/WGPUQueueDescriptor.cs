namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUQueueDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;
}
