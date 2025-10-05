namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUVertexBufferLayout
{
    public WGPUVertexStepMode stepMode;

    [NativeTypeName("uint64_t")]
    public ulong arrayStride;

    [NativeTypeName("size_t")]
    public nuint attributeCount;

    [NativeTypeName("const WGPUVertexAttribute *")]
    public WGPUVertexAttribute* attributes;
}
