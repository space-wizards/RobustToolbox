namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUVertexState
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
    public nuint bufferCount;

    [NativeTypeName("const WGPUVertexBufferLayout *")]
    public WGPUVertexBufferLayout* buffers;
}
