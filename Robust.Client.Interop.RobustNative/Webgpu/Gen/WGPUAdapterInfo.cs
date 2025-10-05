namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUAdapterInfo
{
    public WGPUChainedStructOut* nextInChain;

    public WGPUStringView vendor;

    public WGPUStringView architecture;

    public WGPUStringView device;

    public WGPUStringView description;

    public WGPUBackendType backendType;

    public WGPUAdapterType adapterType;

    [NativeTypeName("uint32_t")]
    public uint vendorID;

    [NativeTypeName("uint32_t")]
    public uint deviceID;
}
