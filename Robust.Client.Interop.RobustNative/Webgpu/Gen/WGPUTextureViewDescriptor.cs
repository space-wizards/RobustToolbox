namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUTextureViewDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    public WGPUTextureFormat format;

    public WGPUTextureViewDimension dimension;

    [NativeTypeName("uint32_t")]
    public uint baseMipLevel;

    [NativeTypeName("uint32_t")]
    public uint mipLevelCount;

    [NativeTypeName("uint32_t")]
    public uint baseArrayLayer;

    [NativeTypeName("uint32_t")]
    public uint arrayLayerCount;

    public WGPUTextureAspect aspect;

    [NativeTypeName("WGPUTextureUsage")]
    public ulong usage;
}
