namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUComputePipelineDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("WGPUPipelineLayout")]
    public WGPUPipelineLayoutImpl* layout;

    public WGPUProgrammableStageDescriptor compute;
}
