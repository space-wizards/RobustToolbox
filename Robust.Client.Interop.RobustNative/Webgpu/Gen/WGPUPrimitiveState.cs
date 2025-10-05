namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUPrimitiveState
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUPrimitiveTopology topology;

    public WGPUIndexFormat stripIndexFormat;

    public WGPUFrontFace frontFace;

    public WGPUCullMode cullMode;

    [NativeTypeName("WGPUBool")]
    public uint unclippedDepth;
}
