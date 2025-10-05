namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUPipelineLayoutDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("size_t")]
    public nuint bindGroupLayoutCount;

    [NativeTypeName("const WGPUBindGroupLayout *")]
    public WGPUBindGroupLayoutImpl** bindGroupLayouts;
}
