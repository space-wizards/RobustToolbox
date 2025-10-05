namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUBufferDescriptor
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUStringView label;

    [NativeTypeName("WGPUBufferUsage")]
    public ulong usage;

    [NativeTypeName("uint64_t")]
    public ulong size;

    [NativeTypeName("WGPUBool")]
    public uint mappedAtCreation;
}
