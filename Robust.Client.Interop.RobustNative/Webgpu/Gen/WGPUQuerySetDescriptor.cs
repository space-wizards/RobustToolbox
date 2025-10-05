namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUQuerySetDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    public WGPUQueryType type;

    [NativeTypeName("uint32_t")]
    public uint count;
}
