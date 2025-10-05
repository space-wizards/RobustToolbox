namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUBindGroupEntry
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("uint32_t")]
    public uint binding;

    [NativeTypeName("WGPUBuffer")]
    public WGPUBufferImpl* buffer;

    [NativeTypeName("uint64_t")]
    public ulong offset;

    [NativeTypeName("uint64_t")]
    public ulong size;

    [NativeTypeName("WGPUSampler")]
    public WGPUSamplerImpl* sampler;

    [NativeTypeName("WGPUTextureView")]
    public WGPUTextureViewImpl* textureView;
}
