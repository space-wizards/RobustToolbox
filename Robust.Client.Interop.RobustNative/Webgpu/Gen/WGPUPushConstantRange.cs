namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUPushConstantRange
{
    [NativeTypeName("WGPUShaderStage")]
    public ulong stages;

    [NativeTypeName("uint32_t")]
    public uint start;

    [NativeTypeName("uint32_t")]
    public uint end;
}
