namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUTextureDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("WGPUTextureUsage")]
    public ulong usage;

    public WGPUTextureDimension dimension;

    public WGPUExtent3D size;

    public WGPUTextureFormat format;

    [NativeTypeName("uint32_t")]
    public uint mipLevelCount;

    [NativeTypeName("uint32_t")]
    public uint sampleCount;

    [NativeTypeName("size_t")]
    public nuint viewFormatCount;

    [NativeTypeName("const WGPUTextureFormat *")]
    public WGPUTextureFormat* viewFormats;
}
