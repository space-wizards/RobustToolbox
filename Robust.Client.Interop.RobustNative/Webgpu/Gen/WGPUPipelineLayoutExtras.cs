namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUPipelineLayoutExtras
{
    public WGPUChainedStruct chain;

    [NativeTypeName("size_t")]
    public nuint pushConstantRangeCount;

    [NativeTypeName("const WGPUPushConstantRange *")]
    public WGPUPushConstantRange* pushConstantRanges;
}
