namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUInstanceExtras
{
    public WGPUChainedStruct chain;

    [NativeTypeName("WGPUInstanceBackend")]
    public ulong backends;

    [NativeTypeName("WGPUInstanceFlag")]
    public ulong flags;

    public WGPUDx12Compiler dx12ShaderCompiler;

    public WGPUGles3MinorVersion gles3MinorVersion;

    public WGPUGLFenceBehaviour glFenceBehaviour;

    public WGPUStringView dxilPath;

    public WGPUStringView dxcPath;

    public WGPUDxcMaxShaderModel dxcMaxShaderModel;
}
