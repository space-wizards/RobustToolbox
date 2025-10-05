namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUFragmentState
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

    [NativeTypeName("size_t")]
    public nuint targetCount;

    [NativeTypeName("const WGPUColorTargetState *")]
    public WGPUColorTargetState* targets;
}
