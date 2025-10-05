namespace Robust.Client.Interop.RobustNative.Webgpu;

internal partial struct WGPUVertexAttribute
{
    public WGPUVertexFormat format;

    [NativeTypeName("uint64_t")]
    public ulong offset;

    [NativeTypeName("uint32_t")]
    public uint shaderLocation;
}
