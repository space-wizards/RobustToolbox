namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslBoolMap
{
    [NativeTypeName("const char *const *")]
    public sbyte** keys;

    [NativeTypeName("const _Bool *")]
    public bool* values;

    [NativeTypeName("size_t")]
    public nuint len;
}
