namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUChainedStruct
{
    [NativeTypeName("const struct WGPUChainedStruct *")]
    public WGPUChainedStruct* next;

    public WGPUSType sType;
}
