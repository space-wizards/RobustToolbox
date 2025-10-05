namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPURenderBundleEncoderDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("size_t")]
    public nuint colorFormatCount;

    [NativeTypeName("const WGPUTextureFormat *")]
    public WGPUTextureFormat* colorFormats;

    public WGPUTextureFormat depthStencilFormat;

    [NativeTypeName("uint32_t")]
    public uint sampleCount;

    [NativeTypeName("WGPUBool")]
    public uint depthReadOnly;

    [NativeTypeName("WGPUBool")]
    public uint stencilReadOnly;
}
