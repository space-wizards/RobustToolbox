namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUTexelCopyTextureInfo
{
    [NativeTypeName("WGPUTexture")]
    public WGPUTextureImpl* texture;

    [NativeTypeName("uint32_t")]
    public uint mipLevel;

    public WGPUOrigin3D origin;

    public WGPUTextureAspect aspect;
}
