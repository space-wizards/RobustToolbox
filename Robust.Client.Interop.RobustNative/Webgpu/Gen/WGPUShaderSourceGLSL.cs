namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUShaderSourceGLSL
{
    public WGPUChainedStruct chain;

    [NativeTypeName("WGPUShaderStage")]
    public ulong stage;

    public WGPUStringView code;

    [NativeTypeName("uint32_t")]
    public uint defineCount;

    public WGPUShaderDefine* defines;
}
