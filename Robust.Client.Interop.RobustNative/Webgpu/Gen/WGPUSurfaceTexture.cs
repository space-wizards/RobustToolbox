namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSurfaceTexture
{
    public WGPUChainedStructOut* nextInChain;

    [NativeTypeName("WGPUTexture")]
    public WGPUTextureImpl* texture;

    public WGPUSurfaceGetCurrentTextureStatus status;
}
