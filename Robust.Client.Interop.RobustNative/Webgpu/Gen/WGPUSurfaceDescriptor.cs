namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUSurfaceDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;
}
