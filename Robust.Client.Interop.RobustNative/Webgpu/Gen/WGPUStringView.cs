namespace Robust.Client.Interop.RobustNative.Webgpu;

internal unsafe partial struct WGPUStringView
{
    [NativeTypeName("const char *")]
    public sbyte* data;

    [NativeTypeName("size_t")]
    public nuint length;
}
