namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSamplerDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    public WGPUAddressMode addressModeU;

    public WGPUAddressMode addressModeV;

    public WGPUAddressMode addressModeW;

    public WGPUFilterMode magFilter;

    public WGPUFilterMode minFilter;

    public WGPUMipmapFilterMode mipmapFilter;

    public float lodMinClamp;

    public float lodMaxClamp;

    public WGPUCompareFunction compare;

    [NativeTypeName("uint16_t")]
    public ushort maxAnisotropy;
}
