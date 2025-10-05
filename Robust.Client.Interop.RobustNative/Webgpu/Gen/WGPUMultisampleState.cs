namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUMultisampleState
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    [NativeTypeName("uint32_t")]
    public uint count;

    [NativeTypeName("uint32_t")]
    public uint mask;

    [NativeTypeName("WGPUBool")]
    public uint alphaToCoverageEnabled;
}
