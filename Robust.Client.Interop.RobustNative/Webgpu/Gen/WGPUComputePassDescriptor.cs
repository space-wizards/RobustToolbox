namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUComputePassDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("const WGPUComputePassTimestampWrites *")]
    public WGPUComputePassTimestampWrites* timestampWrites;
}
