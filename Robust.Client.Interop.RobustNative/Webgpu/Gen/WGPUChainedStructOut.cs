namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUChainedStructOut
{
    [NativeTypeName("struct WGPUChainedStructOut *")]
    public WGPUChainedStructOut* next;

    public WGPUSType sType;
}
