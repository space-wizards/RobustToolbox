namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPURenderPassTimestampWrites
{
    [NativeTypeName("WGPUQuerySet")]
    public WGPUQuerySetImpl* querySet;

    [NativeTypeName("uint32_t")]
    public uint beginningOfPassWriteIndex;

    [NativeTypeName("uint32_t")]
    public uint endOfPassWriteIndex;
}
