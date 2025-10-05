namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUCommandEncoderDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;
}
