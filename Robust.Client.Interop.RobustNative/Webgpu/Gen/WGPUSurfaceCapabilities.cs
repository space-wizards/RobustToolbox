namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSurfaceCapabilities
{
    public WGPUChainedStructOut* nextInChain;

    [NativeTypeName("WGPUTextureUsage")]
    public ulong usages;

    [NativeTypeName("size_t")]
    public nuint formatCount;

    [NativeTypeName("const WGPUTextureFormat *")]
    public WGPUTextureFormat* formats;

    [NativeTypeName("size_t")]
    public nuint presentModeCount;

    [NativeTypeName("const WGPUPresentMode *")]
    public WGPUPresentMode* presentModes;

    [NativeTypeName("size_t")]
    public nuint alphaModeCount;

    [NativeTypeName("const WGPUCompositeAlphaMode *")]
    public WGPUCompositeAlphaMode* alphaModes;
}
