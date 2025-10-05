namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUColorTargetState
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUTextureFormat format;

    [NativeTypeName("const WGPUBlendState *")]
    public WGPUBlendState* blend;

    [NativeTypeName("WGPUColorWriteMask")]
    public ulong writeMask;
}
