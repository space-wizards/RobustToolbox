namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUComputePassTimestampWrites
{
    [NativeTypeName("WGPUQuerySet")]
    public WGPUQuerySetImpl* querySet;

    [NativeTypeName("uint32_t")]
    public uint beginningOfPassWriteIndex;

    [NativeTypeName("uint32_t")]
    public uint endOfPassWriteIndex;
}
