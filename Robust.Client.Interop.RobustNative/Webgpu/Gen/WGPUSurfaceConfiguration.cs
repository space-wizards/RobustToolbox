namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSurfaceConfiguration
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("WGPUDevice")]
    public WGPUDeviceImpl* device;

    public WGPUTextureFormat format;

    [NativeTypeName("WGPUTextureUsage")]
    public ulong usage;

    [NativeTypeName("uint32_t")]
    public uint width;

    [NativeTypeName("uint32_t")]
    public uint height;

    [NativeTypeName("size_t")]
    public nuint viewFormatCount;

    [NativeTypeName("const WGPUTextureFormat *")]
    public WGPUTextureFormat* viewFormats;

    public WGPUCompositeAlphaMode alphaMode;

    public WGPUPresentMode presentMode;
}
