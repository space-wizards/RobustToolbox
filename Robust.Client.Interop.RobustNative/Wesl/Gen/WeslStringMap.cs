namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslStringMap
{
    [NativeTypeName("const char *const *")]
    public sbyte** keys;

    [NativeTypeName("const char *const *")]
    public sbyte** values;

    [NativeTypeName("size_t")]
    public nuint len;
}
