namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPURenderPassColorAttachment
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("WGPUTextureView")]
    public WGPUTextureViewImpl* view;

    [NativeTypeName("uint32_t")]
    public uint depthSlice;

    [NativeTypeName("WGPUTextureView")]
    public WGPUTextureViewImpl* resolveTarget;

    public WGPULoadOp loadOp;

    public WGPUStoreOp storeOp;

    public WGPUColor clearValue;
}
