namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUTextureBindingLayout
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUTextureSampleType sampleType;

    public WGPUTextureViewDimension viewDimension;

    [NativeTypeName("WGPUBool")]
    public uint multisampled;
}
