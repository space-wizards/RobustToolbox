namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUCommandBufferDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;
}
