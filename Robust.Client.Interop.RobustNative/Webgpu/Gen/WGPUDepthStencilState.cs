namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUDepthStencilState
{
    [NativeTypeName("const WGPUChainedStruct *")]
    public WGPUChainedStruct* nextInChain;

    public WGPUTextureFormat format;

    public WGPUOptionalBool depthWriteEnabled;

    public WGPUCompareFunction depthCompare;

    public WGPUStencilFaceState stencilFront;

    public WGPUStencilFaceState stencilBack;

    [NativeTypeName("uint32_t")]
    public uint stencilReadMask;

    [NativeTypeName("uint32_t")]
    public uint stencilWriteMask;

    [NativeTypeName("int32_t")]
    public int depthBias;

    public float depthBiasSlopeScale;

    public float depthBiasClamp;
}
