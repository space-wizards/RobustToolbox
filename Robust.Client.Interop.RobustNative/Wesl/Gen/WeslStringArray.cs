namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslStringArray
{
    [NativeTypeName("const char *const *")]
    public sbyte** items;

    [NativeTypeName("size_t")]
    public nuint len;
}
