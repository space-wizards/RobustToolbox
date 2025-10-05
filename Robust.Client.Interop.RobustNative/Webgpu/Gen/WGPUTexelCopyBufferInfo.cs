namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUTexelCopyBufferInfo
{
    public WGPUTexelCopyBufferLayout layout;

    [NativeTypeName("WGPUBuffer")]
    public WGPUBufferImpl* buffer;
}
