namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUBindGroupEntryExtras
{
    public WGPUChainedStruct chain;

    [NativeTypeName("const WGPUBuffer *")]
    public WGPUBufferImpl** buffers;

    [NativeTypeName("size_t")]
    public nuint bufferCount;

    [NativeTypeName("const WGPUSampler *")]
    public WGPUSamplerImpl** samplers;

    [NativeTypeName("size_t")]
    public nuint samplerCount;

    [NativeTypeName("const WGPUTextureView *")]
    public WGPUTextureViewImpl** textureViews;

    [NativeTypeName("size_t")]
    public nuint textureViewCount;
}
