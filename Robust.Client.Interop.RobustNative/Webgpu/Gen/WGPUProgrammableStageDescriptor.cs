namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUProgrammableStageDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("WGPUShaderModule")]
    public WGPUShaderModuleImpl* module;

    public WGPUStringView entryPoint;

    [NativeTypeName("size_t")]
    public nuint constantCount;

    [NativeTypeName("const WGPUConstantEntry *")]
    public WGPUConstantEntry* constants;
}
