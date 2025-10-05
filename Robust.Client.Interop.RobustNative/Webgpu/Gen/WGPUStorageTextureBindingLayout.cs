namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUStorageTextureBindingLayout
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStorageTextureAccess access;

    public WGPUTextureFormat format;

    public WGPUTextureViewDimension viewDimension;
}
