namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUBufferBindingLayout
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUBufferBindingType type;

    [NativeTypeName("WGPUBool")]
    public uint hasDynamicOffset;

    [NativeTypeName("uint64_t")]
    public ulong minBindingSize;
}
